using RTLSDR.Common;
using System;
using System.ComponentModel;
namespace RTLSDR.DAB
{
    /// <summary>
    /// The DAB processor state.
    /// </summary>
    public class DABProcessorState
    {
        /// <summary>
        /// Gets or sets a value indicating whether synced.
        /// </summary>
        public bool Synced { get; set; } = false;

        /// <summary>
        /// Gets or sets the audio description.
        /// </summary>
        public AudioDataDescription AudioDescription { get; set; } = new AudioDataDescription();

        /// <summary>
        /// Gets or sets the start time.
        /// </summary>
        public DateTime StartTime { get; set; } = DateTime.MinValue;
        /// <summary>
        /// Gets or sets the find first symbol total time.
        /// </summary>
        public double FindFirstSymbolTotalTime { get; set; } = 0;
        /// <summary>
        /// Gets or sets the find first symbol fft time.
        /// </summary>
        public double FindFirstSymbolFFTTime { get; set; } = 0;
        /// <summary>
        /// Gets or sets the find first symbol dft time.
        /// </summary>
        public double FindFirstSymbolDFTTime { get; set; } = 0;
        /// <summary>
        /// Gets or sets the find first symbol multiply time.
        /// </summary>
        public double FindFirstSymbolMultiplyTime { get; set; } = 0;
        /// <summary>
        /// Gets or sets the find first symbol bin time.
        /// </summary>
        public double FindFirstSymbolBinTime { get; set; } = 0;
        /// <summary>
        /// Gets or sets the get first symbol data total time.
        /// </summary>
        public double GetFirstSymbolDataTotalTime { get; set; } = 0;
        /// <summary>
        /// Gets or sets the sync total time.
        /// </summary>
        public double SyncTotalTime { get; set; } = 0;
        /// <summary>
        /// Gets or sets the get all symbols time.
        /// </summary>
        public double GetAllSymbolsTime { get; set; } = 0;
        /// <summary>
        /// Gets or sets the coarse corrector time.
        /// </summary>
        public double CoarseCorrectorTime { get; set; } = 0;
        /// <summary>
        /// Gets or sets the get null symbols time.
        /// </summary>
        public double GetNULLSymbolsTime { get; set; } = 0;

        /// <summary>
        /// Gets or sets the total cycles count.
        /// </summary>
        public int TotalCyclesCount { get; set; } = 0;
        /// <summary>
        /// Gets or sets a value indicating whether first sync processed.
        /// </summary>
        public bool FirstSyncProcessed { get; set; } = true;

        /// <summary>
        /// Gets or sets the s level.
        /// </summary>
        public float SLevel { get; set; } = 0;
        /// <summary>
        /// Gets or sets the local phase.
        /// </summary>
        public int LocalPhase { get; set; } = 0;

        /// <summary>
        /// Gets or sets the fine corrector.
        /// </summary>
        public short FineCorrector { get; set; } = 0;
        /// <summary>
        /// Gets or sets the coarse corrector.
        /// </summary>
        public int CoarseCorrector { get; set; } = 0;

        /// <summary>
        /// Gets or sets the last sync notify time.
        /// </summary>
        public DateTime LastSyncNotifyTime { get; set; } = DateTime.MinValue;
        /// <summary>
        /// Gets or sets the last stat notify time.
        /// </summary>
        public DateTime LastStatNotifyTime { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Gets or sets the total continued count.
        /// </summary>
        public int TotalContinuedCount { get; set; } = 0;

        /// <summary>
        /// Gets or sets the audio bitrate.
        /// </summary>
        public double AudioBitrate { get; set; } = 0;
        /// <summary>
        /// Gets or sets the iq bitrate.
        /// </summary>
        public double IQBitrate { get; set; } = 0;
        /// <summary>
        /// Gets or sets the signal power.
        /// </summary>
        public double SignalPower { get; set; } = 0;

        /// <summary>
        /// Gets or sets the fic count.
        /// </summary>
        public int FICCount { get; set; } = 0;
        /// <summary>
        /// Gets or sets the fic count in valid.
        /// </summary>
        public int FICCountInValid { get; set; } = 0;
        /// <summary>
        /// Gets or sets the fic count valid.
        /// </summary>
        public int FICCountValid { get; set; } = 0;

        /// <summary>
        /// Gets or sets the processed super frames count.
        /// </summary>
        public int ProcessedSuperFramesCount { get; set; } = 0;
        /// <summary>
        /// Gets or sets the processed super frames count in valid.
        /// </summary>
        public int ProcessedSuperFramesCountInValid { get; set; } = 0;
        /// <summary>
        /// Gets or sets the processed super frames count valid.
        /// </summary>
        public int ProcessedSuperFramesCountValid { get; set; } = 0;

        /// <summary>
        /// Gets or sets the processed super frames a us count.
        /// </summary>
        public int ProcessedSuperFramesAUsCount { get; set; } = 0;
        /// <summary>
        /// Gets or sets the processed super frames a us count in valid.
        /// </summary>
        public int ProcessedSuperFramesAUsCountInValid { get; set; } = 0;
        /// <summary>
        /// Gets or sets the processed super frames a us count valid.
        /// </summary>
        public int ProcessedSuperFramesAUsCountValid { get; set; } = 0;

        /// <summary>
        /// Gets or sets the sync thread stat.
        /// </summary>
        public IThreadWorkerInfo? SyncThreadStat { get; set; } = null;
        /// <summary>
        /// Gets or sets the sprectrum thread stat.
        /// </summary>
        public IThreadWorkerInfo? SprectrumThreadStat { get; set; } = null;
        /// <summary>
        /// Gets or sets the ofdm thread stat.
        /// </summary>
        public IThreadWorkerInfo? OFDMThreadStat { get; set; } = null;
        /// <summary>
        /// Gets or sets the msc thread stat.
        /// </summary>
        public IThreadWorkerInfo? MSCThreadStat { get; set; } = null;
        /// <summary>
        /// Gets or sets the fic thread stat.
        /// </summary>
        public IThreadWorkerInfo? FICThreadStat { get; set; } = null;
        /// <summary>
        /// Gets or sets the sfm thread stat.
        /// </summary>
        public IThreadWorkerInfo? SFMThreadStat { get; set; } = null;
        /// <summary>
        /// Gets or sets the aac thread stat.
        /// </summary>
        public IThreadWorkerInfo? AACThreadStat { get; set; } = null;

        public String SyncedAsString
        {
            get
            {
                return Synced ? "Yes" : "-";
            }
        }

        private static string GetValueHR(double value)
        {
            if (value > 1000000)
            {
                return (value / 1000000.00).ToString("N2");
            }
            if (value > 1000)
            {
                return (value / 1000.00).ToString("N2");
            }

            return (value).ToString("N2");
        }

        private static string GetValueHRUnit(double value, string suffix)
        {
            if (value > 1000000)
            {
                return $"M{suffix}";
            }
            if (value > 1000)
            {
                return $"K{suffix}";
            }

            return $"{suffix}";
        }

        public string AudioDescriptionHR
        {
            get
            {
                if (AudioDescription == null)
                    return "";

                var sr = $"{GetValueHR(AudioDescription.SampleRate)} {GetValueHRUnit(AudioDescription.SampleRate, "Hz")}, {AudioDescription.BitsPerSample} bit";

                if (AudioDescription.Channels == 1)
                {
                    sr += ", mono";
                }
                else if (AudioDescription.Channels == 2)
                {
                    sr += ", stereo";
                }
                else
                {
                    sr += $", channels: {(AudioDescription.Channels)}";
                }

                return sr;
            }
        }

        public string AudioBitRateHR
        {
            get
            {
                return GetValueHR(AudioBitrate);
            }
        }

        public string AudioBitRateHRUnit
        {
            get
            {
                return GetValueHRUnit(AudioBitrate, "b/s");
            }
        }


        public string SignalPowerHR
        {
            get
            {
                return SignalPower.ToString("N0");
            }
        }

        public string SignalPowerHRUnit
        {
            get
            {
                return "%";
            }
        }


        public string IQBitRateHR
        {
            get
            {
                return GetValueHR(IQBitrate);
            }
        }

        public string IQBitRateHRUnit
        {
            get
            {
                return GetValueHRUnit(IQBitrate, "b/s");
            }
        }

        public void Clear()
        {
            Synced = false;
            AudioDescription = new AudioDataDescription();
            StartTime = DateTime.MinValue;
            FindFirstSymbolTotalTime = 0;
            FindFirstSymbolFFTTime = 0;
            FindFirstSymbolDFTTime = 0;
            FindFirstSymbolMultiplyTime  = 0;
            FindFirstSymbolBinTime  = 0;
            GetFirstSymbolDataTotalTime  = 0;
            SyncTotalTime  = 0;
            GetAllSymbolsTime  = 0;
            CoarseCorrectorTime  = 0;
            GetNULLSymbolsTime  = 0;

            TotalCyclesCount  = 0;
            FirstSyncProcessed  = true;

            SLevel = 0;
            LocalPhase = 0;

            FineCorrector = 0;
            CoarseCorrector = 0;

            LastSyncNotifyTime = DateTime.MinValue;
            LastStatNotifyTime = DateTime.MinValue;

            TotalContinuedCount  = 0;

            AudioBitrate  = 0;
            IQBitrate  = 0;
            SignalPower  = 0;

            FICCount  = 0;
            FICCountInValid  = 0;
            FICCountValid  = 0;

            ProcessedSuperFramesCount  = 0;
            ProcessedSuperFramesCountInValid  = 0;
            ProcessedSuperFramesCountValid  = 0;

            ProcessedSuperFramesAUsCount  = 0;
            ProcessedSuperFramesAUsCountInValid  = 0;
            ProcessedSuperFramesAUsCountValid  = 0;
        }
    }
}
