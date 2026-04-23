using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Numerics;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using LoggerService;
using RTLSDR.Common;

namespace RTLSDR.DAB
{
    /*
        Free .NET DAB+ library

        -   based upon welle.io (https://github.com/AlbrechtL/welle.io)
        -   DAB documentation: https://www.etsi.org/deliver/etsi_en/300400_300499/300401/02.01.01_60/en_300401v020101p.pdf
    */

    /// <summary>
    /// Processes DAB (Digital Audio Broadcasting) signals, demodulating and decoding audio data.
    /// </summary>
    public class DABProcessor : IDemodulator
    {
        private readonly ILoggingService _loggingService;

        /// <summary>
        /// Gets or sets the sample rate for the input signal.
        /// </summary>
        public int Samplerate { get; set; } = 2048000; // INPUT_RATE

        /// <summary>
        /// Gets or sets a value indicating whether coarse correction is enabled.
        /// </summary>
        public bool CoarseCorrector { get; set; } = true;

        /// <summary>
        /// Event raised when data has been demodulated.
        /// </summary>
        public event EventHandler? OnDemodulated = null;

        /// <summary>
        /// Event raised when processing is finished.
        /// </summary>
        public event EventHandler? OnFinished = null;

        /// <summary>
        /// Event raised when a service is found.
        /// </summary>
        public event EventHandler? OnServiceFound = null;

        /// <summary>
        /// Event raised when a service is played.
        /// </summary>
        public event EventHandler? OnServicePlayed = null;

        public event EventHandler? OnDynamicLabelChanged = null;

        /// <summary>
        /// Gets or sets the service number to process.
        /// </summary>
        public int ServiceNumber { get; set; } = -1;

        private DABSubChannel? _processingSubChannel { get; set; } = null;
        private DABService? _processingService { get; set; } = null;

        private readonly DABProcessorState _state = new DABProcessorState();

        private bool _finish = false;

        private const int MinThreadNoDataMSDelay = 25;

        private const int SEARCH_RANGE = 2 * 36;
        private const int CORRELATION_LENGTH = 24;
        private const int CUSize = 4 * 16; // 64

        private const int T_F = 196608;
        private const int T_null = 2656;
        private const int T_u = 2048;
        private const int L = 76;
        private const int T_s = 2552;
        private const int K = 1536;
        private const int CarrierDiff = 1000;
        private const int BitsperBlock = 2 * K; // 3072

        private readonly ConcurrentQueue<FComplex[]> _samplesQueue = new ConcurrentQueue<FComplex[]>();
        private readonly ConcurrentQueue<List<FComplex[]>> _OFDMDataQueue = new ConcurrentQueue<List<FComplex[]>>();
        private readonly ConcurrentQueue<FICQueueItem> _ficDataQueue = new ConcurrentQueue<FICQueueItem>();
        private readonly ConcurrentQueue<sbyte[]> _MSCDataQueue = new ConcurrentQueue<sbyte[]>();
        private readonly ConcurrentQueue<byte[]> _DABSuperFrameDataQueue = new ConcurrentQueue<byte[]>();
        private readonly ConcurrentQueue<byte[]> _AACDataQueue = new ConcurrentQueue<byte[]>();

        private readonly ThreadWorker<object>? _statusThreadWorker = null;
        private readonly ThreadWorker<FComplex[]>? _syncThreadWorker = null;
        private readonly ThreadWorker<List<FComplex[]>>? _OFDMThreadWorker = null;   // FFT
        private readonly ThreadWorker<FICQueueItem>? _FICThreadWorker = null;        // Reading FIC channel
        private readonly ThreadWorker<sbyte[]>? _MSCThreadWorker = null;             // Reading MSC channel (de-interleave, deconvolute, dedisperse)
        private readonly ThreadWorker<byte[]>? _SuperFrameThreadWorker = null;       // Decoding SuperFrames
        private readonly ThreadWorker<byte[]>? _AACThreadWorker = null;              // AAC to PCM

        private FComplex[]? _currentSamples = null;
        private int _currentSamplesPosition = -1;
        private long _totalSamplesRead = 0;

        private const int SyncBufferSize = 32768;
        private const int SyncInterruptCyclesCount = 100;
        private readonly float[] _syncEnvBuffer = new float[SyncBufferSize];
        private readonly int _syncBufferMask = SyncBufferSize - 1;

        private readonly FrequencyInterleaver _interleaver;

        private readonly BitRateCalculation _audioBitRateCalculator;
        private readonly BitRateCalculation _IQBitRateCalculator;

        // DAB mode I:
        private const int DABModeINumberOfBlocksPerCIF = 18;

        private FComplex[]? _oscillatorTable { get; set; } = null;
        private readonly double[] _refArg;
        private readonly FourierSinCosTable _sinCosTable;

        private readonly PhaseTable _phaseTable;
        private readonly FICData _fic;

        private readonly Viterbi _FICViterbi;

        private DABDecoder? _DABDecoder = null;

        private byte _addSamplesOoddByte;
        private bool _oddByteSet = false;

        private string? _dynamicLabel = null;

        private AACSuperFrameHeader? _AACSuperFrameHeader = null;

        public DABProcessor(ILoggingService loggingService)
        {
            _loggingService = loggingService;

            BuildOscillatorTable();

            _sinCosTable = new FourierSinCosTable()
            {
                Count = T_u
            };

            _interleaver = new FrequencyInterleaver(T_u, K);
            _phaseTable = new PhaseTable(_loggingService, Samplerate, T_u);

            if (_phaseTable ==  null || _phaseTable.RefTable == null)
            {
                throw new DABException("Phase table not initialized");
            }

            _refArg = new double[CORRELATION_LENGTH];

            for (int i = 0; i < CORRELATION_LENGTH; i++)
            {
                _refArg[i] = FComplex.Multiply(_phaseTable.RefTable[(T_u + i) % T_u], _phaseTable.RefTable[(T_u + i + 1) % T_u].Conjugated()).PhaseAngle();
            }

            _FICViterbi = new Viterbi(768);

            _fic = new FICData(_loggingService, _FICViterbi);
            _fic.OnServiceFound += _fic_OnServiceFound;
            _fic.OnProcessedFICCountChanged += delegate
            {
                _state.FICCount = _fic.FICCount;
                _state.FICCountValid = _fic.FICProcessedCountWithValidCRC;
                _state.FICCountInValid = _fic.FICProcessedCountWithInValidCRC;
                _state.FICCountInValid = _fic.FICProcessedCountWithInValidCRC;
            };

            _state.StartTime = DateTime.UtcNow;

            _statusThreadWorker = new ThreadWorker<object>(_loggingService, "STAT");
            _statusThreadWorker.SetThreadMethod(StatusThreadWorkerGo, MinThreadNoDataMSDelay);

            _syncThreadWorker = new ThreadWorker<FComplex[]>(_loggingService, "SYNC");
            _syncThreadWorker.SetThreadMethod(SyncThreadWorkerGo, MinThreadNoDataMSDelay);
            _syncThreadWorker.SetQueue(_samplesQueue);

            _OFDMThreadWorker = new ThreadWorker<List<FComplex[]>>(_loggingService, "OFDM");
            _OFDMThreadWorker.SetThreadMethod(_OFDMThreadWorkerGo, MinThreadNoDataMSDelay);
            _OFDMThreadWorker.SetQueue(_OFDMDataQueue);
            _OFDMThreadWorker.ReadingQueue = true;

            _FICThreadWorker = new ThreadWorker<FICQueueItem>(_loggingService, "FIC");
            _FICThreadWorker.SetThreadMethod(FICThreadWorkerGo, MinThreadNoDataMSDelay);
            _FICThreadWorker.SetQueue(_ficDataQueue);
            _FICThreadWorker.ReadingQueue = true;

            _MSCThreadWorker = new ThreadWorker<sbyte[]>(_loggingService, "MSC");
            _MSCThreadWorker.SetThreadMethod(MSCThreadWorkerGo, MinThreadNoDataMSDelay);
            _MSCThreadWorker.SetQueue(_MSCDataQueue);
            _MSCThreadWorker.ReadingQueue = true;

            _SuperFrameThreadWorker = new ThreadWorker<byte[]>(_loggingService, "SpFM");
            _SuperFrameThreadWorker.SetThreadMethod(SuperFrameThreadWorkerGo, MinThreadNoDataMSDelay);
            _SuperFrameThreadWorker.SetQueue(_DABSuperFrameDataQueue);
            _SuperFrameThreadWorker.ReadingQueue = true;

            _AACThreadWorker = new ThreadWorker<byte[]>(_loggingService, "AAC");
            _AACThreadWorker.SetThreadMethod(AACThreadWorkerGo, MinThreadNoDataMSDelay);
            _AACThreadWorker.SetQueue(_AACDataQueue);
            _AACThreadWorker.ReadingQueue = true;

            _state.SyncThreadStat = _syncThreadWorker;
            _state.OFDMThreadStat = _OFDMThreadWorker;
            _state.MSCThreadStat = _MSCThreadWorker;
            _state.FICThreadStat = _FICThreadWorker;
            _state.SFMThreadStat = _SuperFrameThreadWorker;
            _state.AACThreadStat = _AACThreadWorker;

            _audioBitRateCalculator = new BitRateCalculation(_loggingService, "DAB audio");
            _IQBitRateCalculator =  new BitRateCalculation(_loggingService, "IQ data");
        }

        public List<DABService> DABServices
        {
            get
            {
                if (_fic == null)
                {
                    return new List<DABService>();
                }

                return _fic.DABServices;
            }
        }

        public void Start()
        {
            _loggingService.Debug("Starting all thread workers");

            _statusThreadWorker!.Start();
            _syncThreadWorker!.Start();
            _OFDMThreadWorker!.Start();
            _FICThreadWorker!.Start();
            _MSCThreadWorker!.Start();
            _SuperFrameThreadWorker!.Start();
            _AACThreadWorker!.Start();
        }

        public void Stop()
        {
            _loggingService.Debug("Stopping all thread workers");

            _syncThreadWorker!.Stop();
            _statusThreadWorker!.Stop();
            _OFDMThreadWorker!.Stop();
            _FICThreadWorker!.Stop();
            _MSCThreadWorker!.Stop();
            _SuperFrameThreadWorker!.Stop();
            _AACThreadWorker!.Stop();
        }

        public int QueueSize
        {
            get
            {
                var res = 0;
                if ((_syncThreadWorker != null) &&
                    (_syncThreadWorker is IThreadWorkerInfo i)
                   )
                {
                    res += i.QueueItemsCount;
                }

                return res;
            }
        }

        public double AudioBitrate
        {
            get
            {
                return _state.AudioBitrate;
            }
        }

        public double PercentSignalPower
        {
            get
            {
                return 0;
            }
        }

        /// <summary>
        /// Inform that all data from input has been processed
        /// </summary>
        public void Finish()
        {
            _finish = true;
        }

        public FICData FIC
        {
            get
            {
                return _fic;
            }
        }

        public DABService? ProcessingDABService
        {
            get
            {
                return _processingService;
            }
        }

        public DABProcessorState State
        {
            get
            {
                return _state;
            }
        }

        public DABSubChannel? ProcessingSubCannel
        {
            get
            {
                return _processingSubChannel;
            }
        }

        #region STAT

        private string StatTitle(string title)
        {
            return $"--------{title.PadRight(46, '-')}";
        }

        private string StatValue(string title, string value, string unit = "")
        {
            if (!string.IsNullOrWhiteSpace(title))
            {
                title += ":";
            }
            return $" {title.PadRight(25, ' ')} {value.PadLeft(15, ' ')} {unit}";
        }

        private string FormatStatValue(string title, int value, string unit)
        {
            return StatValue(title, value.ToString(), unit);
        }

        private string FormatStatValue(string title, long value, string unit)
        {
            return StatValue(title, value.ToString("N0"), unit);
        }

        private string FormatStatValue(string title, BitRateCalculation bc)
        {
            double val = bc.BitRate;
            var unit = "b/s";

            if (val > 1000000.0)
            {
                val = bc.BitRate / 1000000.0;
                unit = "Mb/s";
            } else if (val > 1000.0)
            {
                val = bc.BitRate / 1000.0;
                unit = "Kb/s";
            }

            return FormatStatValue(title, val, unit);
        }

        private string FormatStatValue(string title, double value, string unit)
        {
            return StatValue(title, value.ToString("N2"), unit);
        }

        private string FormatStatValue(string title, TimeSpan elapsed, string unit)
        {
            var time = $"{elapsed.Hours.ToString().PadLeft(2, '0')}:{elapsed.Minutes.ToString().PadLeft(2, '0')}:{elapsed.Seconds.ToString().PadLeft(2, '0')}";
            return StatValue(title, time, unit);
        }

        public List<StatValue> GetStat()
        {
            var res = new List<StatValue>();

            res.Add(new StatValue("Total samples count", _totalSamplesRead));
            res.Add(new StatValue("Service number", ServiceNumber));

            res.Add(RTLSDR.Common.StatValue.CreateFromBitrate("BitRate - IQ data", _IQBitRateCalculator.BitRate));
            res.Add(RTLSDR.Common.StatValue.CreateFromBitrate("BitRate - AAC audio",_audioBitRateCalculator.BitRate));
            res.Add(RTLSDR.Common.StatValue.CreateFromFrequency("Sample rate", Samplerate));
            res.Add(new StatValue("Synced", Synced));
            res.Add(new StatValue("Continued count", _state.TotalContinuedCount));
            res.Add(new StatValue("Sync queue", _syncThreadWorker == null ? 0 : _syncThreadWorker.QueueItemsCount));

            var tbl = new DataTable();
            tbl.Columns.Add(new DataColumn("Name", typeof(string)));
            tbl.Columns.Add(new DataColumn("Total", typeof(int)));
            tbl.Columns.Add(new DataColumn("Invalid", typeof(int)));
            tbl.Columns.Add(new DataColumn("Decoded", typeof(int)));

            var row = tbl.NewRow();
            row["Name"] = "FIC";
            row["Total"] = _fic.FICCount;
            row["Invalid"] = _fic.FICProcessedCountWithInValidCRC;
            row["Decoded"] = _fic.FICProcessedCountWithValidCRC;
            tbl.Rows.Add(row);

            if (_DABDecoder != null)
            {
                var row2 = tbl.NewRow();
                row2["Name"] = "SpF";
                row2["Total"] = _DABDecoder.ProcessedSuperFramesCount;
                row2["Invalid"] = _DABDecoder.ProcessedSuperFramesCount - _DABDecoder.ProcessedSuperFramesSyncedCount;
                row2["Decoded"] = _DABDecoder.ProcessedSuperFramesSyncedCount;
                tbl.Rows.Add(row2);

                var row3 = tbl.NewRow();
                row3["Name"] = "AU";
                row3["Total"] = _DABDecoder.ProcessedSuperFramesAUsCount;
                row3["Invalid"] = _DABDecoder.ProcessedSuperFramesAUsCount - _DABDecoder.ProcessedSuperFramesAUsSyncedCount;
                row3["Decoded"] = _DABDecoder.ProcessedSuperFramesAUsSyncedCount;
                tbl.Rows.Add(row3);
            }

            res.Add(new StatValue("FIC/SpF/AU", tbl));

            var tbl3 = new DataTable();
            tbl3.Columns.Add(new DataColumn("Name", typeof(string)));
            tbl3.Columns.Add(new DataColumn(" ", typeof(int)));
            tbl3.Columns.Add(new DataColumn(" ", typeof(long)));
            tbl3.Columns.Add(new DataColumn("#", typeof(double)));

            foreach (var service in _fic.DABServices)
            {
                var r = tbl3.NewRow();
                r["ServiceName"] = service.ServiceName;
                r["c1"] = string.Empty;
                r["c2"] = string.Empty;
                r["ServiceNumber"] = service.ServiceNumber;
                tbl3.Rows.Add(r);
            }

            res.Add(new StatValue("Services", tbl3));

            var tbl2 = new DataTable();
            tbl2.Columns.Add(new DataColumn("Thread", typeof(string)));
            tbl2.Columns.Add(new DataColumn("Queue", typeof(int)));
            tbl2.Columns.Add(new DataColumn("Cycles", typeof(long)));
            tbl2.Columns.Add(new DataColumn("Time", typeof(double)));

            var tws = new List<IThreadWorkerInfo>();
            tws.AddRange(new IThreadWorkerInfo[]
            {
                _syncThreadWorker!,
                _OFDMThreadWorker!,
                _MSCThreadWorker!,
                _FICThreadWorker!,
                _SuperFrameThreadWorker!,
                _AACThreadWorker!,
            });

            foreach (var twi in tws)
            {
                if (twi == null)
                    continue;

                var r = tbl2.NewRow();
                r["Thread"] = twi.Name;
                r["Queue"] = twi.QueueItemsCount;
                r["Cycles"] = twi.CyclesCount;
                r["Time"] = twi.QueueItemsCount;
                tbl2.Rows.Add(r);
            }

            res.Add(new StatValue("Threads", tbl2));

            return res;
        }

        public string Stat(bool detailed)
        {
            var res = new StringBuilder();

            var line = "";

            res.AppendLine(FormatStatValue("Total samples count", _totalSamplesRead, ""));
            res.AppendLine(FormatStatValue("Service number", ServiceNumber, ""));

            if (_IQBitRateCalculator!=null)
            {
                res.AppendLine(FormatStatValue("BitRate - IQ", _IQBitRateCalculator));
            }

            if (_audioBitRateCalculator!=null)
            {
               res.AppendLine(FormatStatValue("BitRate - AAC", _audioBitRateCalculator));
            }

             res.AppendLine(StatValue("Synced", _state.Synced ? "[x]" : "[ ]"));
             res.AppendLine(FormatStatValue("Continued count", _state.TotalContinuedCount, ""));
             res.AppendLine(FormatStatValue("Sync queue", _syncThreadWorker == null ? 0 : _syncThreadWorker.QueueItemsCount, ""));

            line = $"{"-".PadLeft(9, '-')}";
            line += $"{"-Total-".PadLeft(17, '-')}";
            line += $"{"-Invalid-".PadLeft(12, '-')}";
            line += $"{"-Decoded-".PadLeft(17, '-')}";
            res.AppendLine(line);

            line = $"{"FIC".PadLeft(8, ' ')} |";
            line += $"{_fic.FICCount.ToString().PadLeft(15, ' ')} |";
            line += $"{_fic.FICProcessedCountWithInValidCRC.ToString().PadLeft(10, ' ')} |";
            line += $"{_fic.FICProcessedCountWithValidCRC.ToString().PadLeft(15, ' ')} |";
            res.AppendLine(line);

            if (_DABDecoder != null)
            {
                line = $"{"SpFS".PadLeft(8, ' ')} |";
                line += $"{_DABDecoder.ProcessedSuperFramesCount.ToString().PadLeft(15, ' ')} |";
                line += $"{(_DABDecoder.ProcessedSuperFramesCount - _DABDecoder.ProcessedSuperFramesSyncedCount).ToString().PadLeft(10, ' ')} |";
                line += $"{_DABDecoder.ProcessedSuperFramesSyncedCount.ToString().PadLeft(15, ' ')} |";
                res.AppendLine(line);

                line = $"{"AU".PadLeft(8, ' ')} |";
                line += $"{_DABDecoder.ProcessedSuperFramesAUsCount.ToString().PadLeft(15, ' ')} |";
                line += $"{(_DABDecoder.ProcessedSuperFramesAUsCount - _DABDecoder.ProcessedSuperFramesAUsSyncedCount).ToString().PadLeft(10, ' ')} |";
                line += $"{_DABDecoder.ProcessedSuperFramesAUsSyncedCount.ToString().PadLeft(15, ' ')} |";
                res.AppendLine(line);
            }

            res.AppendLine(StatTitle("-"));

            if (detailed)
            {
                line = $"{"-Thread-".PadLeft(9, '-')}";
                line += $"{"-Queue-".PadLeft(17, '-')}";
                line += $"{"-Cycles-".PadLeft(12, '-')}";
                line += $"{"-Time(s)-".PadLeft(17, '-')}";

                res.AppendLine(line);

                var tws = new List<IThreadWorkerInfo>();
                tws.AddRange(new IThreadWorkerInfo[]
                {
                _syncThreadWorker!,
                _OFDMThreadWorker!,
                _MSCThreadWorker!,
                _FICThreadWorker!,
                _SuperFrameThreadWorker!,
                _AACThreadWorker!,
                });

                var sumCount = 0;
                foreach (var twi in tws)
                {
                    if (twi == null)
                        continue;
                    line = $"{(twi.Name).ToString().PadLeft(8, ' ')} |";
                    line += $"{(twi.QueueItemsCount.ToString().PadLeft(15, ' '))} |";
                    line += $"{twi.CyclesCount.ToString().PadLeft(10, ' ')} |";
                    line += $"{(twi.WorkingTimeMS / 1000).ToString("#00.00").PadLeft(15, ' ')} |";
                    sumCount += twi.QueueItemsCount;
                    res.AppendLine(line);
                }
                line = $"{"-Total-".PadLeft(9, '-')}";
                line += $"{"-".PadLeft(17, '-')}";
                line += $"{"-".PadLeft(12, '-')}";
                line += $"{"-" + (((DateTime.UtcNow - _state.StartTime).TotalMilliseconds / 1000).ToString("#00.00") + "-").PadLeft(16, '-')}";
                res.AppendLine(line);

            }

            line = $"{"-".PadLeft(9, '-')}";
            line += $"{"-DAB servicies-".PadLeft(17, '-')}";
            line += $"{"-".PadLeft(12, '-')}";
            line += $"{"-".PadLeft(17, '-')}";
            res.AppendLine(line);

            foreach (var service in _fic.DABServices)
            {
                res.AppendLine(StatValue(
                    service.ServiceName,
                    service.ServiceNumber.ToString(),
                    " A"+service.ServiceNumber.ToString("x").ToUpper()));
            }

            return res.ToString();
        }

        #endregion

        public void ResetSync()
        {
            _state.TotalContinuedCount = 0;
            _state.Synced = false;
            _state.FirstSyncProcessed = true;
            _state.CoarseCorrector = 0;
            _state.FineCorrector = 0;
            _state.SLevel = 0;
            _state.LocalPhase = 0;
        }

        /// <summary>
        /// Sync samples position
        /// </summary>
        /// <returns>sync position</returns>
        private bool Sync(bool firstSync)
        {
            float currentStrength = 0;
            var syncBufferIndex = 0;

            // process first T_F/2 samples  (see void OFDMProcessor::run())
            if (firstSync)
            {
                GetSamples(T_F / 2, 0);
            }

            var synced = false;
            while (!synced)
            {
                syncBufferIndex = 0;
                currentStrength = 0;

                // break when total samples read exceed some value
                if ( _state.TotalContinuedCount>SyncInterruptCyclesCount)
                {
                    _loggingService.Info($"Syncing failed ({SyncInterruptCyclesCount} cycles)");
                    ResetSync();
                    return false;
                }

                var next50Samples = GetSamples(50, 0);
                for (var i = 0; i < 50; i++)
                {
                    var sample = next50Samples[i];

                     _syncEnvBuffer[syncBufferIndex] = sample.L1Norm();
                    currentStrength += _syncEnvBuffer[syncBufferIndex];
                    syncBufferIndex++;
                }

                // looking for the null level

                var counter = 0;
                var ok = true;

                while (currentStrength / 50 > 0.5F * _state.SLevel)
                {
                    var sample = GetSamples(1, _state.CoarseCorrector + _state.FineCorrector)[0];
                     _syncEnvBuffer[syncBufferIndex] = Math.Abs(sample.Real) + Math.Abs(sample.Imaginary);

                    // Update the levels
                    currentStrength += _syncEnvBuffer[syncBufferIndex] - _syncEnvBuffer[(syncBufferIndex - 50) & _syncBufferMask];
                    syncBufferIndex = (syncBufferIndex + 1) & _syncBufferMask;

                    counter++;
                    if (counter > T_F)
                    {
                        // Not synced!
                        ok = false;
                        break;
                    }
                }

                if (!ok)
                {
                    _state.TotalContinuedCount++;
                    continue;
                }

                // looking for the end of the null period.

                counter = 0;
                ok = true;
                while (currentStrength / 50 < 0.75F * _state.SLevel)
                {
                    var sample = GetSamples(1, _state.CoarseCorrector + _state.FineCorrector)[0];

                    _syncEnvBuffer[syncBufferIndex] = sample.L1Norm();
                    //  update the levels

                    currentStrength += _syncEnvBuffer[syncBufferIndex] - _syncEnvBuffer[syncBufferIndex - 50 & _syncBufferMask];
                    syncBufferIndex = syncBufferIndex + 1 & _syncBufferMask;
                    counter++;
                    if (counter > T_null + 50)
                    {
                        // not synced!
                        ok = false;
                        break;
                    }
                }

                if (!ok)
                {
                     _state.TotalContinuedCount++;
                    continue;
                }
                else
                {
                    synced = true;
                }
            }

            return synced;
        }

        private int FindIndex(FComplex[] rawSamples)
        {
            try
            {
                if (_phaseTable == null || _phaseTable.RefTable == null)
                {
                    throw new DABException("Phase table not initialized");
                }

                // rawSamples must remain intact to CoarseCorrector

                var samples = new FComplex[rawSamples.Length];
                Array.Copy(rawSamples, samples, rawSamples.Length);

                var startFindFirstSymbolFFTTime = DateTime.UtcNow;

                Fourier.FFTBackward(samples);

                _state.FindFirstSymbolFFTTime += (DateTime.UtcNow - startFindFirstSymbolFFTTime).TotalMilliseconds;

                var startFindFirstSymbolMultiplyTime = DateTime.UtcNow;

                for (var i = 0; i < samples.Length; i++)
                {
                    samples[i] = FComplex.MultiplyConjugated(samples[i], _phaseTable.RefTable[i]);
                }

                _state.FindFirstSymbolMultiplyTime += (DateTime.UtcNow - startFindFirstSymbolMultiplyTime).TotalMilliseconds;

                var startFindFirstSymbolDFTTime = DateTime.UtcNow;

                if (_sinCosTable.CosTable !=null && _sinCosTable.SinTable != null)
                {
                    samples = Fourier.DFTBackward(samples, _sinCosTable.CosTable, _sinCosTable.SinTable);
                } else
                {
                    throw new DABException("SinCos tables not initialized");
                }

                _state.FindFirstSymbolDFTTime += (DateTime.UtcNow - startFindFirstSymbolDFTTime).TotalMilliseconds;

                float factor = 1.0F / samples.Length;

                startFindFirstSymbolMultiplyTime = DateTime.UtcNow;

                //// scale all entries
                for (int i = 0; i < samples.Length; i++)
                {
                    samples[i].Scale(factor);
                }

                _state.FindFirstSymbolMultiplyTime += (DateTime.UtcNow - startFindFirstSymbolMultiplyTime).TotalMilliseconds;

                var startFindFirstSymbolBinTime = DateTime.UtcNow;

                var bin_size = 20;
                var num_bins_to_keep = 4;
                var bins = new List<Peak>();
                double mean = 0;

                for (var i = 0; i + bin_size < T_u; i += bin_size)
                {
                    var peak = new Peak();
                    for (var j = 0; j < bin_size; j++)
                    {
                        var value = samples[i + j].Abs();
                        mean += value;

                        if (value > peak.Value)
                        {
                            peak.Value = value;
                            peak.Index = i + j;
                        }
                    }
                    bins.Add(peak);
                }

                mean /= samples.Length;

                if (bins.Count < num_bins_to_keep)
                {
                    throw new DABException("Sync err, not enough bins");
                }

                // Sort bins by highest peak
                bins.Sort();

                // Keep only bins that are not too far from highest peak
                var peak_index = bins[0].Index;
                var max_subpeak_distance = 500;

                var peaksCloseToMax = new List<Peak>();
                foreach (var peak in bins)
                {
                    if (Math.Abs(peak.Index - peak_index) < max_subpeak_distance)
                    {
                        peaksCloseToMax.Add(peak);

                        if (peaksCloseToMax.Count >= num_bins_to_keep)
                        {
                            break;
                        }
                    }
                }

                var thresh = 3.0 * mean;
                var peaksAboveTresh = new List<Peak>();
                foreach (var peak in peaksCloseToMax)
                {
                    if (peak.Value > thresh)
                    {
                        peaksAboveTresh.Add(peak);
                    }
                }

                if (peaksAboveTresh.Count == 0)
                    return -1;

                // earliest_bin

                Peak earliestPeak = peaksAboveTresh[0];
                foreach (var peak in peaksAboveTresh)
                {
                    if (peak == peaksAboveTresh[0])
                        continue;

                    if (peak.Index < earliestPeak.Index)
                    {
                        earliestPeak = peak;
                    }
                }

                _state.FindFirstSymbolBinTime += (DateTime.UtcNow - startFindFirstSymbolBinTime).TotalMilliseconds;

                return earliestPeak.Index;

            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error finding index");
                return -1;
            }
        }

        public bool Synced
        {
            get
            {
                if (_state == null)
                {
                    return false;
                }

                return _state.Synced;
            }
        }

        private void SyncThreadWorkerGo(object? data = null)
        {
            try
            {
                _state.TotalCyclesCount++;

                if (!_state.Synced)
                {
                    var startSyncTime = DateTime.UtcNow;
                    _state.Synced = Sync(_state.FirstSyncProcessed);
                    _state.FirstSyncProcessed = false;

                    _state.SyncTotalTime += (DateTime.UtcNow - startSyncTime).TotalMilliseconds;

                    if (!_state.Synced)
                    {
                        _loggingService.Debug($"-[]-Sync failed!");
                        return;
                    }
                }

                // find first sample

                var samples = GetSamples(T_u, _state.CoarseCorrector + _state.FineCorrector);

                var startFirstSymbolSearchTime = DateTime.UtcNow;

                var startIndex = FindIndex(samples);

                _state.FindFirstSymbolTotalTime += (DateTime.UtcNow - startFirstSymbolSearchTime).TotalMilliseconds;

                if (startIndex == -1)
                {
                    // not synced
                    _state.Synced = false;
                    return;
                }

                var startGetFirstSymbolDataTime = DateTime.UtcNow;

                var firstOFDMBuffer = new FComplex[T_u];

                Array.Copy(samples, startIndex, firstOFDMBuffer, 0, T_u - startIndex);

                var missingSamples = GetSamples(startIndex, _state.CoarseCorrector + _state.FineCorrector);

                Array.Copy(missingSamples, 0, firstOFDMBuffer, T_u - startIndex, startIndex);

                _state.GetFirstSymbolDataTotalTime += (DateTime.UtcNow - startGetFirstSymbolDataTime).TotalMilliseconds;

                var startCoarseCorrectorTime = DateTime.UtcNow;

                // coarse corrector
                if (CoarseCorrector && (_fic.FicDecodeRatioPercent < 50))
                {
                    int correction = ProcessPRS(firstOFDMBuffer);
                    if (correction != 100)
                    {
                        _state.CoarseCorrector += correction * CarrierDiff;
                        if (Math.Abs(_state.CoarseCorrector) > 35 * 1000)
                            _state.CoarseCorrector = 0;
                    }
                }

                _state.CoarseCorrectorTime += (DateTime.UtcNow - startCoarseCorrectorTime).TotalMilliseconds;

                var startGetAllSymbolsTime = DateTime.UtcNow;

                var allSymbols = new List<FComplex[]>
                {
                    firstOFDMBuffer
                };

                var FreqCorr = new FComplex(0, 0);

                for (int sym = 1; sym < L; sym++)
                {
                    var buf = GetSamples(T_s, _state.CoarseCorrector + _state.FineCorrector);
                    allSymbols.Add(buf);

                    for (int i = T_u; i < T_s; i++)
                    {
                        FreqCorr.Add(FComplex.Multiply(buf[i], buf[i - T_u].Conjugated()));
                    }
                }

                _OFDMDataQueue.Enqueue(allSymbols);

                _state.GetAllSymbolsTime += (DateTime.UtcNow - startGetAllSymbolsTime).TotalMilliseconds;

                var startGetNULLSymbolsTime = DateTime.UtcNow;

                // cpp always round down
                _state.FineCorrector = Convert.ToInt16(Math.Truncate(_state.FineCorrector + 0.1 * FreqCorr.PhaseAngle() / Math.PI * (CarrierDiff / 2.0)));

                // save NULL data:

                var nullSymbol = GetSamples(T_null, _state.CoarseCorrector + _state.FineCorrector);

                if (_state.FineCorrector > CarrierDiff / 2)
                {
                    _state.CoarseCorrector += CarrierDiff;
                    _state.FineCorrector -= CarrierDiff;
                }
                else
                {
                    if (_state.FineCorrector < -CarrierDiff / 2)
                    {
                        _state.CoarseCorrector -= CarrierDiff;
                        _state.FineCorrector += CarrierDiff;
                    }
                }

                _state.GetNULLSymbolsTime += (DateTime.UtcNow - startGetNULLSymbolsTime).TotalMilliseconds;

            }
            catch (NoSamplesException)
            {
                // no samples available, just wait for next cycle
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error while sync");
            }
        }

        private void _OFDMThreadWorkerGo(List<FComplex[]> allSymbols)
        {
            ProcessOFDMData(allSymbols);
        }

        private void MSCThreadWorkerGo(sbyte[] MSCData)
        {
            ProcessMSCData(MSCData);
        }

        private void AACThreadWorkerGo(byte[] AUData)
        {
            if ((_AACSuperFrameHeader == null) || (OnDemodulated == null))
            {
                return;
            }

            var audioDescription = new AudioDataDescription()
            {
                BitsPerSample = 16,
                Channels = 2,
                SampleRate = 48000
            };

            var adtsHeader= ADTSHeader.CreateAdtsHeader((int)AACProfileEnum.AACLC, _AACSuperFrameHeader.GetCoreSampleRate(), audioDescription.Channels, AUData.Length);

            _state.AudioBitrate = _audioBitRateCalculator.UpdateBitRate(AUData.Length);
            _state.AudioDescription = audioDescription;

            if ((_DABDecoder?.DynamicLabel != _dynamicLabel) && (OnDynamicLabelChanged != null))
            {
                _dynamicLabel = _DABDecoder?.DynamicLabel;
                OnDynamicLabelChanged(this, new DynamicLabelChangedEventArgs()
                {
                    Label = _DABDecoder?.DynamicLabel
                });
            }

            OnDemodulated(this, new AACDataDemodulatedEventArgs()
            {
                Data = AUData,
                AudioDescription = audioDescription,
                AACHeader = _AACSuperFrameHeader,
                ADTSHeader = adtsHeader
            });
        }

        private void SuperFrameThreadWorkerGo(byte[] DABData)
        {
            if (_DABDecoder != null)
            {
                _DABDecoder.Feed(DABData);
            }
        }

        private void FICThreadWorkerGo(FICQueueItem ficData)
        {
            if (ficData.Data == null)
                return;

            _fic.ParseData(ficData);
        }

        private void StatusThreadWorkerGo(object? input = null)
        {
            try
            {
                if (_finish &&
                    (_samplesQueue.Count == 0) &&
                    (_OFDMDataQueue.Count == 0) &&
                    (_ficDataQueue.Count == 0) &&
                    (_MSCDataQueue.Count == 0) &&
                    (_DABSuperFrameDataQueue.Count == 0) &&
                    (_AACDataQueue.Count == 0))
                {
                    OnFinished?.Invoke(this, new EventArgs());
                    _finish = false;
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        private int ProcessPRS(FComplex[] data)
        {
            var index = 100;
            var correlationVector = new double[SEARCH_RANGE + CORRELATION_LENGTH];

            var fft_buffer = FComplex.CloneComplexArray(data);
            Fourier.FFTBackward(fft_buffer);

            // FreqsyncMethod::CorrelatePRS:

            for (int i = 0; i < SEARCH_RANGE + CORRELATION_LENGTH; i++)
            {
                var baseIndex = T_u - SEARCH_RANGE / 2 + i;

                correlationVector[i] = FComplex.Multiply(fft_buffer[baseIndex % T_u], fft_buffer[(baseIndex + 1) % T_u].Conjugated()).PhaseAngle();
            }

            double MMax = 0;
            for (int i = 0; i < SEARCH_RANGE; i++)
            {
                double sum = 0;
                for (int j = 0; j < CORRELATION_LENGTH; j++)
                {
                    sum += Math.Abs(_refArg[j] * correlationVector[i + j]);
                    if (sum > MMax)
                    {
                        MMax = sum;
                        index = i;
                    }
                }
            }

            return T_u - SEARCH_RANGE / 2 + index - T_u;
        }

        private void ProcessOFDMData(List<FComplex[]> allSymbols)
        {
            try
            {
                // processPRS:
                var phaseReference = allSymbols[0];

                Fourier.FFTBackward(phaseReference);

                // decodeDataSymbol:

                var iBits = new sbyte[K * 2];
                var mscData = new List<sbyte>();

                for (var sym = 1; sym < allSymbols.Count; sym++)
                {
                    var T_g = T_s - T_u;
                    var croppedSymbols = new FComplex[T_u];

                    Array.Copy(allSymbols[sym], T_g, croppedSymbols, 0, T_u);

                    Fourier.FFTBackward(croppedSymbols);

                    for (var i = 0; i < K; i++)
                    {
                        var index = _interleaver.MapIn(i);

                        if (index < 0)
                        {
                            index += T_u;
                        }

                        var r1 = FComplex.Multiply(croppedSymbols[index], phaseReference[index].Conjugated());
                        phaseReference[index] = croppedSymbols[index];

                        var ab1 = 127.0f / r1.L1Norm();
                        /// split the real and the imaginary part and scale it

                        var real = -r1.Real * ab1;
                        var imag = -r1.Imaginary * ab1;

                        iBits[i] = (sbyte)(Math.Truncate(real));
                        iBits[K + i] = (sbyte)(Math.Truncate(imag));
                    }

                    // values in iBits are changing during data processing!
                    if (sym < 4)
                    {
                        _ficDataQueue.Enqueue(new FICQueueItem()
                        {
                            Data = iBits.CloneArray(),
                            FicNo = sym - 1
                        });
                    }
                    else
                    {
                        mscData.AddRange(iBits.CloneArray());
                    }
                }

                if (ServiceNumber > 0)
                {
                    _MSCDataQueue.Enqueue(mscData.ToArray());
                }
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex);
            }
        }

        private void ProcessMSCData(sbyte[] MSCData)
        {
            if (_processingSubChannel == null)
                return;

            // MSCData consist of 72 symbols
            // 72 symbols ~ 211 184 bits  (27 648 bytes)
            // 72 symbols devided to 4 CIF (18 symbols)

            var startPos = Convert.ToInt32(_processingSubChannel.StartAddr * CUSize);
            var length = Convert.ToInt32(_processingSubChannel.Length * CUSize);

            if (_DABDecoder == null)
            {
                _DABDecoder = new DABDecoder(
                    _loggingService,
                    _processingSubChannel,
                    CUSize,
                    _DABSuperFrameDataQueue,
                    DABDecoder_OnDemodulated,
                    DABDecoder_OnSuperFrameHeaderDemodulated);

                _DABDecoder.OnProcessedSuperFramesChanged += _DABDecoder_OnProcessedSuperFramesChanged;
            }

            // DAB-audio.run

            for (var cif = 0; cif < 4; cif++)
            {
                var DABBuffer = new sbyte[length];

                Buffer.BlockCopy(MSCData, cif * BitsperBlock * DABModeINumberOfBlocksPerCIF + startPos, DABBuffer, 0, length);

                if (_DABDecoder != null)
                {
                    _DABDecoder.ProcessCIFFragmentData(DABBuffer);
                }
            }
        }

        private void _DABDecoder_OnProcessedSuperFramesChanged(object? sender, EventArgs e)
        {
            if (_DABDecoder != null)
            {
                _state.ProcessedSuperFramesCount = _DABDecoder.ProcessedSuperFramesCount;
                _state.ProcessedSuperFramesCountInValid = _DABDecoder.ProcessedSuperFramesCount - _DABDecoder.ProcessedSuperFramesSyncedCount;
                _state.ProcessedSuperFramesCountValid = _DABDecoder.ProcessedSuperFramesSyncedCount;

                _state.ProcessedSuperFramesAUsCount = _DABDecoder.ProcessedSuperFramesAUsCount;
                _state.ProcessedSuperFramesAUsCountInValid = _DABDecoder.ProcessedSuperFramesAUsCount - _DABDecoder.ProcessedSuperFramesAUsSyncedCount;
                _state.ProcessedSuperFramesAUsCountValid = _DABDecoder.ProcessedSuperFramesAUsSyncedCount;
            }
        }

        private void DABDecoder_OnDemodulated(object? sender, EventArgs e)
        {
            if (
                (e is DataDemodulatedEventArgs eAACdata) &&
                (eAACdata.Data != null) &&
                (eAACdata.Data.Length > 0))
                {
                    _AACDataQueue.Enqueue(eAACdata.Data);
                }
        }

        private void DABDecoder_OnSuperFrameHeaderDemodulated(object? sender, EventArgs e)
        {
            if (e is AACSuperFrameHaderDemodulatedEventArgs eAAC)
            {
                _AACSuperFrameHeader = eAAC.Header;
            }
        }

        private void _fic_OnServiceFound(object? sender, EventArgs e)
        {
            if (e is DABServiceFoundEventArgs d)
            {
                if (_processingSubChannel == null &&
                    d.Service?.ServiceNumber == ServiceNumber)
                {
                    SetProcessingSubChannel(d.Service, d.Service.FirstSubChannel);
                }

                OnServiceFound?.Invoke(this, e);
            }
        }

        public bool SetProcessingService(int serviceNumber)
        {
            ServiceNumber = serviceNumber; // in case of services not read yet!
            foreach (var service in DABServices)
            {
                if (service.ServiceNumber == serviceNumber)
                {
                    SetProcessingService(service);
                    return true;
                }
            }
            return false;
        }

        public void SetProcessingService(DABService service)
        {
            SetProcessingSubChannel(service, service.FirstSubChannel);
        }

        private void SetProcessingSubChannel(DABService service, DABSubChannel? dABSubChannel)
        {
            _processingSubChannel = dABSubChannel;
            _processingService = service;
            ServiceNumber = Convert.ToInt32(service.ServiceNumber);
            _DABDecoder = null;

            OnServicePlayed?.Invoke(this, new DABServicePlayedEventArgs()
            {
                Service = service,
                SubChannel = dABSubChannel
            });
        }

        public static FComplex[] ToDSPComplex(byte[] iqData, int length, int offset)
        {
            var res = new FComplex[length / 2];

            float factor = 1.0f / 128.0f;

            for (int i = 0; i < length / 2; i++)
            {
                res[i] = new FComplex(
                                (iqData[i * 2 + offset] - 128) * factor,
                                (iqData[i * 2 + offset + 1] - 128) * factor
                            );
            }

            return res;
        }

        private void BuildOscillatorTable()
        {
            _oscillatorTable = new FComplex[Samplerate];

            for (int i = 0; i < Samplerate; i++)
            {
                _oscillatorTable[i] = new FComplex(
                    Math.Cos(2.0 * Math.PI * i / (float)Samplerate),
                    Math.Sin(2.0 * Math.PI * i / (float)Samplerate));
            }
        }

        private FComplex[] GetSamples(int count, int phase, int msTimeOut = 1000)
        {
            var getStart = DateTime.UtcNow;
            var res = new FComplex[count];

            int i = 0;
            while (i < count)
            {
                if (_currentSamples == null || _currentSamplesPosition >= _currentSamples.Length)
                {
                    var ok = _samplesQueue.TryDequeue(out _currentSamples);

                    if (!ok)
                    {
                        var span = DateTime.UtcNow - getStart;
                        if (span.TotalMilliseconds > msTimeOut)
                        {
                            throw new NoSamplesException();
                        }
                        else
                        {
                            Thread.Sleep(MinThreadNoDataMSDelay);
                        }

                        continue;
                    }
                    else
                    {
                        _currentSamplesPosition = 0;
                    }
                }
                if (_currentSamplesPosition>_currentSamples!.Length-1)
                {
                    throw new NoSamplesException();
                }
                res[i] = _currentSamples![_currentSamplesPosition];

                _state.LocalPhase -= phase;
                _state.LocalPhase = (_state.LocalPhase + Samplerate) % Samplerate;

                res[i] = FComplex.Multiply(res[i], _oscillatorTable![_state.LocalPhase]);
                _state.SLevel = Convert.ToSingle(0.00001 *(res[i].L1Norm()) + (1.0 - 0.00001) * _state.SLevel);

                i++;
                _currentSamplesPosition++;

                _totalSamplesRead++;
            }

            return res;
        }

        public void AddSamples(byte[] IQData, int length)
        {
            int offset = 0;

            if (_oddByteSet)
            {
                var missingSample = ToDSPComplex( new byte[] {_addSamplesOoddByte , IQData[0]}, 2 , 0);
                offset = 1;
                 _samplesQueue.Enqueue(missingSample);
            }

            var dspComplexArray = ToDSPComplex(IQData, length-offset, offset);
            _samplesQueue.Enqueue(dspComplexArray);

            if (((length-offset) % 2) == 1)
            {
                _addSamplesOoddByte = IQData[length-1];
                _oddByteSet = true;
            } else
            {
                _oddByteSet = false;
            }

            _state.IQBitrate = _IQBitRateCalculator.UpdateBitRate(length);
        }

        /// <summary>
        /// Clears the internal state of the demodulator.
        /// </summary>
        public void Clear()
        {
            _processingSubChannel = null;
            _processingService = null;
            ServiceNumber = -1;

            ResetSync();

            _samplesQueue.Clear();
            _OFDMDataQueue.Clear();
            _ficDataQueue.Clear();
            _MSCDataQueue.Clear();
            _DABSuperFrameDataQueue.Clear();
            _AACDataQueue.Clear();

            _currentSamples = null;
            _currentSamplesPosition = 0;
            _dynamicLabel = null;

            _fic?.Clear();
            _fic?.ClearServices();
        }
    }
}
