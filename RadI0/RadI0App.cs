using LibVLCSharp.Shared;
using LoggerService;
using NAudio.CoreAudioApi;
using NAudio.MediaFoundation;
using NAudio.Wave.SampleProviders;
using Newtonsoft.Json;
using NLog;
using RTLSDR;
using RTLSDR.Audio;
using RTLSDR.Common;
using RTLSDR.DAB;
using RTLSDR.FM;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Terminal.Gui;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RadI0;

/// <summary>
/// The rad i 0 app.
/// </summary>
public class RadI0App
{
    private readonly ILoggingService _logger;
    private readonly IRawAudioPlayer? _audioPlayer;
    private readonly object _lock = new object();
    private readonly ISDR? _sdrDriver;
    private readonly AppParams _appParams;
    private int _processingFilePercents = 0;
    private string _processingFileBitRate = "";

    private bool _rawAudioPlayerInitialized = false;
    /// <summary>
    /// Occurs when event handler.
    /// </summary>
    public event EventHandler? OnDemodulated = null;

    /// <summary>
    /// Occurs when event handler.
    /// </summary>
    public event EventHandler? OnFinished = null;

    private IDemodulator? _demodulator = null;
    private readonly IAACDecoder? _aacDecoder = null;

    private DABProcessor? _dabDemodulator = null;
    private FMDemodulator? _fmDemodulator = null;

    private List<Station> _stations = new List<Station>();
    private Wave? _wave = null;

    private readonly RadI0GUI _gui;

    private CancellationTokenSource? _tuneCts = null;
    private Task? _tuneTask = null;

    private readonly SpectrumWorker _spectrumWorker;

    private UDPStreamer? _udpStreamer = null;

    private DateTime _lastDataReceivedTime = DateTime.MinValue;

    private string? _lastDynamicLabel = null;

    private int _heartbeatFrame = 0;
    private bool _running = true;

    public RadI0App(
        ISDR sdrDriver,
        ILoggingService loggingService,
        RadI0GUI gui,
        AppParams appParams,
        IAACDecoder? aacDecoder)
    {
        _gui = gui;
        _audioPlayer = new VLCSoundAudioPlayer();
        _aacDecoder = aacDecoder;

        _logger = loggingService;
        _sdrDriver = sdrDriver;
        _appParams = appParams;

        _gui.OnStationChanged += StationChanged;
        _gui.OnGainChanged += GainChanged;
        _gui.OnFrequentionChanged += FrequentionChanged;
        _gui.OnRecordStart += OnRecordStart;
        _gui.OnRecordStop += OnRecordStop;
        _gui.OnTuningStart += delegate {  StartTune(_appParams.Config.FM ? FMTune : DABTune);  } ;
        _gui.OnTuningStop += delegate { StopTune(); } ;
        _gui.OnQuit += OnQuit;
        _gui.OnBandchanged += BandChanged;

        _spectrumWorker = new SpectrumWorker(_logger, 16384, AudioTools.DABSampleRate);
    }

    private async Task DABTune()
    {
        var TuneDelaMS = 25000;

        try
        {
            foreach (var dabFreq in AudioTools.DabFrequenciesHz)
            {
                if (_tuneCts == null || _tuneCts.IsCancellationRequested)
                {
                    return;
                }

                if (_demodulator is DABProcessor dp)
                {
                    dp.ServiceNumber = -1;
                    dp.ResetSync();
                    _sdrDriver?.SetFrequency(dabFreq.Value);
                    // TODO: Clear DAB services?
                }

                for (var i=1;i<TuneDelaMS/1000;i++)
                {
                    var onePerc = Convert.ToDecimal((TuneDelaMS/1000.0)/100.0);
                    var perc = i == 1 ? 0m : Convert.ToDecimal(i-1)/onePerc;
                    await Task.Delay(1000); // wait

                    if (_tuneCts == null || _tuneCts.IsCancellationRequested)
                    {
                        return;
                    }

                }
            }

        }
        catch (OperationCanceledException)
        {
            // Expected exit path — not an error
        }
    }

    public static string ConfigPath
    {
        get
        {
            var configPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RadI0", "RadI0.json");
            if (!Directory.Exists(Path.GetDirectoryName(configPath)!))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            }
            return configPath;
        }
    }

    public static string StationsConfigPath
    {
        get
        {
            var configPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RadI0", "stations.json");
            if (!Directory.Exists(Path.GetDirectoryName(configPath)!))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
            }
            return configPath;
        }
    }

    public void SaveConfig()
    {
        try
        {

        var configJson = Newtonsoft.Json.JsonConvert.SerializeObject(_appParams.Config, Newtonsoft.Json.Formatting.Indented);
        System.IO.File.WriteAllText(ConfigPath, configJson);
        } catch (Exception ex)
        {
            _logger.Error(ex);
        }
    }


    public void SaveStations()
    {
        try
        {
            lock(_lock)
            {
                var json = Newtonsoft.Json.JsonConvert.SerializeObject(_stations, Newtonsoft.Json.Formatting.Indented);
                System.IO.File.WriteAllText(StationsConfigPath, json);
            }
        } catch (Exception ex)
        {
            _logger.Error(ex);
        }
    }

    public void LoadConfig()
    {
        try
        {
            var configJson = System.IO.File.ReadAllText(ConfigPath);
            var config = Newtonsoft.Json.JsonConvert.DeserializeObject<RaidI0Config>(configJson);
            if (config != null)
            {
                if (!_appParams.FMCommandLineParamSet && !_appParams.DABCommandLineParamSet)
                {
                    _appParams.Config.FM = config.FM;
                    _appParams.Config.DAB = config.DAB;
                }


                if (!_appParams.HwgainCommandLineParamSet &&
                    !_appParams.SwgainCommandLineParamSet &&
                    !_appParams.GainCommandLineParamSet)
                {
                    _appParams.Config.HWGain = config.HWGain;
                    _appParams.Config.SWGain = config.SWGain;
                    _appParams.Config.Gain = config.Gain;
                }

                if (!_appParams.MonoCommandLineParamSet)
                {
                    _appParams.Config.Mono = config.Mono;
                }
                if (!_appParams.FrequencyCommandLineParamSet)
                {
                    if (_appParams.Config.FM &&
                            (config.Frequency>AudioTools.FMMinFreq) &&
                            (config.Frequency<AudioTools.FMMaxFreq)
                        )
                        {
                            _appParams.Config.Frequency = config.Frequency;
                        }

                        if (_appParams.Config.DAB &&
                            (config.Frequency>AudioTools.DABMinFreq) &&
                            (config.Frequency<AudioTools.DABMaxFreq)
                        )
                        {
                            _appParams.Config.Frequency = config.Frequency;
                        }
                }

                if (!_appParams.ServiceNumberCommandLineParamSet)
                {
                    _appParams.Config.ServiceNumber = config.ServiceNumber;
                }
            }

        } catch (Exception ex)
        {
            _logger?.Error(ex);
        }
    }

    public void LoadStations()
    {
        try
        {
            if (!File.Exists(StationsConfigPath))
            {
                return;
            }

            var json = System.IO.File.ReadAllText(StationsConfigPath);

            lock (_lock)
            {
                _stations = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Station> >(json);
            }

            _gui.RefreshStations(_stations, GetStationByFreqAndServiceNumber(_appParams.Config.Frequency,_appParams.Config.ServiceNumber));
        } catch (Exception ex)
        {
            _logger?.Error(ex);
        }
    }

    private async Task FMTune()
    {
        try
        {
            var startFreqFMMhz = 88.0;
            var endFreqFMMhz = 108.0;
            var bandWidthMhz = 0.1;

            var tuneDelaMS_1 = 300;  // wait for freq change
            var tuneDelaMS_2 = 500;  // wait for buffer fill
            var tuneDelaMS_3 = 1000; // hear 85

            for (var f = startFreqFMMhz; f < endFreqFMMhz; f += bandWidthMhz)
            {
                if (_tuneCts == null || _tuneCts.IsCancellationRequested)
                {
                    break;
                }

                var freq = AudioTools.ParseFreq($"{f}Mhz");

                _sdrDriver?.SetFrequency(freq);

                await Task.Delay(tuneDelaMS_1); // wait for freq change

                _audioPlayer?.ClearBuffer();
                await Task.Delay(tuneDelaMS_2); // wait for buffer fill

                if (_demodulator?.Synced == true)
                {
                    await Task.Delay(tuneDelaMS_3); // wait for buffer fill
                }
            }

        }
        catch (OperationCanceledException)
        {
            // Expected exit path — not an error
        }
    }
    private void OnRecordStart(object? sender, EventArgs e)
    {
        if (e is RecordStartEventArgs d)
        {
            if (d.Wave)
            {
                _appParams.WaveFileName = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), $"{DateTime.UtcNow.ToString("yyyy-MM-dd--hh-mm-ss")}.wav");
            } else
            {
                _appParams.AACFileName = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), $"{DateTime.UtcNow.ToString("yyyy-MM-dd--hh-mm-ss")}.aac");
            }
        }
    }

    private void OnRecordStop(object? sender, EventArgs e)
    {
        if (!string.IsNullOrEmpty(_appParams.WaveFileName))
        {
            if (_wave != null)
            {
                _wave.CloseWaveFile();
                _wave = null;
            }
            _gui.ShowInfoDialog($"Record saved to {_appParams.WaveFileName}");
        }
        if (!string.IsNullOrEmpty(_appParams.AACFileName))
        {
            _gui.ShowInfoDialog($"Record saved to {_appParams.AACFileName}");
        }
        _appParams.WaveFileName = "";
        _appParams.AACFileName = "";
    }

    private void OnQuit(object? sender, EventArgs e)
    {
        _running = false;

        _wave?.CloseWaveFile();

        if (_demodulator != null)
        {
            _demodulator.Stop();
        }

        if ((_sdrDriver != null) && (_sdrDriver.State == DriverStateEnum.Connected))
        {
           _sdrDriver.Disconnect();
        }
    }

    private void StationChanged(object? sender, EventArgs e)
    {
        if ((e is StationFoundEventArgs d) && (d.Station != null))
        {
            Play(d.Station);
        }
    }

    private void GainChanged(object? sender, EventArgs e)
    {
        if (e is GainChangedEventArgs d)
        {
            _appParams.Config.HWGain = d.HWGain;
            _appParams.Config.SWGain = d.SWGain;
            _appParams.Config.Gain = d.ManualGainValue;

            SaveConfig();

            SetGain();
        }
    }

    private void BandChanged(object? sender, EventArgs e)
    {
        if ((_sdrDriver == null) || (_sdrDriver.State != DriverStateEnum.Connected))
        {
            return;
        }

        if (e is BandChangedEventArgs bea)
        {
            _demodulator?.Stop();

            if (_audioPlayer != null)
            {
                _audioPlayer.Stop();
                _audioPlayer.ClearBuffer();
            }
            _rawAudioPlayerInitialized = false;

            if (bea.FM)
            {
                _appParams.Config.DAB = false;
                _appParams.Config.FM = true;
                _sdrDriver.SetSampleRate(AudioTools.FMSampleRate);
                _sdrDriver.SetFrequency(AudioTools.FMMinFreq);

                _demodulator = _fmDemodulator;
            } else
            {
                _appParams.Config.DAB = true;
                _appParams.Config.FM = false;
                _sdrDriver.SetSampleRate(AudioTools.DABSampleRate);
                _sdrDriver.SetFrequency(AudioTools.DABMinFreq);

                _demodulator = _dabDemodulator;
            }

            _demodulator?.Clear();
            _demodulator!.Start();
            _lastDynamicLabel = null;

            SaveConfig();
        }
    }

    private void FrequentionChanged(object? sender, EventArgs e)
    {
        if (e is FrequentionChangedEventArgs d)
        {
            if (_demodulator is DABProcessor db)
            {
                db.SetProcessingService(-1);
            }
            _appParams.Config.Frequency = d.Frequention;
            _sdrDriver?.SetFrequency(_appParams.Config.Frequency);

            _demodulator?.Clear();
            _lastDynamicLabel = null;

            SaveConfig();
        }
    }

    public async Task StartAsync(string[] args)
    {
        LoadConfig();
        LoadStations();

        _logger.Info("DAB+/FM Radio Player");

        _fmDemodulator = new FMDemodulator(_logger);
        _fmDemodulator.Mono = _appParams.Config.Mono;
        _fmDemodulator.OnDemodulated += AppConsole_OnDemodulated;
        _fmDemodulator.OnFinished += AppConsole_OnFinished;
        _fmDemodulator.OnServiceFound += Demodulator_OnServiceFound;
        _fmDemodulator.OnDynamicLabelChanged += Demodulator_DynamicLabelChanged;

        _dabDemodulator = new DABProcessor(_logger);
        _dabDemodulator.OnServicePlayed += DABProcessor_OnServicePlayed;
        _dabDemodulator.ServiceNumber = _appParams.Config.ServiceNumber;
        _dabDemodulator.OnDemodulated += AppConsole_OnDemodulated;
        _dabDemodulator.OnFinished += AppConsole_OnFinished;
        _dabDemodulator.OnServiceFound += Demodulator_OnServiceFound;
        _dabDemodulator.OnDynamicLabelChanged += Demodulator_DynamicLabelChanged;

        if (_appParams.Config.FM)
        {
            _demodulator = _fmDemodulator;
        } else
        {
            _demodulator =_dabDemodulator;
        }

        _demodulator!.Start();

        _ = Task.Run(() => RefreshGUILoop());

        switch (_appParams.InputSource)
        {
            case InputSourceEnum.File:
                await ProcessFile();
                break;
            case (InputSourceEnum.RTLDevice):
                await ProcessDriverData();
                break;
            default:
                _logger.Info("Unknown source");
                break;
        }

        SaveConfig();

        _logger.Debug("Rad10 Run method finished");
    }

    private string GetState()
    {
        if (_sdrDriver == null)
        {
            return "Not initialized";
        }

        switch (_sdrDriver.State)
        {
            case DriverStateEnum.NotInitialized:
                return "Not initialized";
            case DriverStateEnum.Connected:
                return "Connected";
            case DriverStateEnum.DisConnected:
                return "DisConnected";
            case DriverStateEnum.Error:
                return "Error";
        }

        return "??";
    }

    public static string GetFrequencyForDisplay(int freq)
    {
        var dabFreq = "";
        foreach (var df in AudioTools.DabFrequenciesHz)
        {
            if (df.Value == freq)
            {
                dabFreq = df.Key;
                break;
            }
        }
        var  frequency = $"{(freq / 1000000.0).ToString("N3")} MHz";

        if (dabFreq != "")
        {
            frequency = $"{dabFreq} ({frequency})";
        }

        return frequency;
    }

    private async Task RefreshGUILoop()
    {
        while (_running)
        {
            string status = "";
            string bitRate = "";
            string frequency = "";
            string device = "";
            string audio = "";
            bool synced = false;

            switch (_appParams.InputSource)
            {
                case InputSourceEnum.File:
                    device = _appParams.InputFileName ?? "Unknown file";
                    status = $"Reading file: {_processingFilePercents.ToString().PadLeft(3,' ')}%";
                    bitRate = _processingFileBitRate;
                    break;
                case (InputSourceEnum.RTLDevice):
                    device = _sdrDriver?.DeviceName ?? "Unknown device";
                    bitRate =  _sdrDriver == null ? "" : $"{(_sdrDriver.RTLBitrate / 1000000.0).ToString("N1")} MB/s";
                    frequency = GetFrequencyForDisplay(_sdrDriver == null ? 0 : _sdrDriver.Frequency);
                    status = GetState();
                    break;
            }

            if (_demodulator != null)
            {
                synced = _demodulator.Synced;
            }

            if (_audioPlayer != null)
            {
                var audioDesc = _audioPlayer.GetAudioDataDescription();

                if (audioDesc != null)
                {
                    if (audioDesc.Channels == 1)
                    {
                        audio = "Mono";
                    } else
                    {
                        if (audioDesc.Channels == 2)
                        {
                            audio = "Stereo";
                        } else
                        {
                            audio = $"{audioDesc.Channels} chs";
                        }
                    }

                    audio += $", {audioDesc.BitsPerSample}b, {audioDesc.SampleRate/1000} KHz";
                }
            }

            var gain = "";
            if (_appParams.Config.HWGain)
            {
                gain = "HW";
            } else
            {
                if (_appParams.Config.SWGain)
                {
                    gain = _sdrDriver == null ? "" : $"SW ({(_sdrDriver.Gain / 10.0).ToString("N1")} dB)";
                } else
                {
                    gain = _sdrDriver == null ? "" : $"{(_sdrDriver.Gain / 10.0).ToString("N1")} dB";
                }
            }

            var audioBitRate = "";
            if (_demodulator != null)
            {
                audioBitRate =  $"{(_demodulator.AudioBitrate / 1000.0).ToString("N0")} KB/s";
            }

            _gui?.RefreshBand(_appParams.Config.FM);

            var queue = _demodulator?.QueueSize.ToString();

            var displayText = "Initializing";

            if (_sdrDriver != null)
            {
                switch (_sdrDriver.State)
                {
                    case DriverStateEnum.Connected:

                        displayText = $"Tuning {GetFrequencyForDisplay(_sdrDriver.Frequency)}";

                        if (_demodulator is DABProcessor dab)
                        {
                                if (dab.Synced &&
                                (dab.ProcessingDABService != null) &&
                                (dab.ProcessingSubCannel != null)
                                )
                            {
                                displayText = $"Playing {dab.ProcessingDABService.ServiceName}";
                            }
                        } else
                        {
                            if ((_demodulator is FMDemodulator fm) && (fm.Synced))
                            {
                                displayText = $"Playing {GetFrequencyForDisplay(_sdrDriver.Frequency)}";
                            }
                        }
                    break;
                    case DriverStateEnum.DisConnected:
                        displayText = "Disconnected";
                    break;
                    case DriverStateEnum.Error:
                        displayText = "Error";
                    break;
                    default:
                        displayText = "Initializing";
                     break;
                }
            }

            if (_appParams.InputSource == InputSourceEnum.File)
            {
                displayText = status;
            }

            if (!string.IsNullOrEmpty(_lastDynamicLabel))
            {
                displayText += $" - {_lastDynamicLabel}";
            }

            var output = (string.IsNullOrWhiteSpace(_appParams.UDP)) ? "libVLC" : "udp";
            if (!string.IsNullOrWhiteSpace(_appParams.WaveFileName))
            {
                output += $", wave";
            }
            if (!string.IsNullOrWhiteSpace(_appParams.AACFileName))
            {
                output += $", aac";
            }

            var stat = "";
            if ((_demodulator != null) && (_gui != null) && _gui.StatWindowActive)
            {
                stat = _demodulator.Stat(true);
            }

            var spectrum = "";
            if (_spectrumWorker != null)
            {
                var w = 60;
                var h = 20;
                if (_gui != null)
                {
                    w = _gui.SpectrumWidth;
                    if (w == 0)
                    {
                        w = 60;
                    }
                    h = _gui.SpectrumHeight;
                    if (h == 0)
                    {
                        h = 20;
                    }
                }
                spectrum = _spectrumWorker.GetTextSpectrum(w,h);
            }


            var heartbeat = "";
            if (DateTime.UtcNow - _lastDataReceivedTime > TimeSpan.FromSeconds(1))
            {
                heartbeat = "\u2591\u2591\u2591\u2591";
            } else
            {
                char[] symbols = { '\u2591', '\u2592', '\u2593', '\u2588' };

                for (int i = 0; i < symbols.Length; i++)
                {
                    if (i == _heartbeatFrame)
                    {
                        heartbeat += symbols[i];
                    } else
                    {
                        heartbeat += symbols[0];
                    }
                }
                _heartbeatFrame++;
                if (_heartbeatFrame >= symbols.Length)
                {
                    _heartbeatFrame = 0;
                }
            }

            var s = new AppStatus()
            {
                Status = status,
                 BitRate = bitRate,
                  AudioBitRate = audioBitRate ,
                   Audio = audio,
                    Device = device,
                     Frequency = frequency ,
                      Gain = gain,
                       Queue = queue == null ? "" : queue,
                        Synced = synced ? "[x]" : "[ ]",
                         DisplayText = displayText,
                          Output = output.Trim(),
                           Stat = stat,
                            Spectrum = spectrum,
                            Tuning = _tuneCts != null ? "tuning" : "",
                             Heartbeat = heartbeat
            };

            _gui?.RefreshStat(s);

            await Task.Delay(500);
        }
    }

    public List<Station> Stations
    {
         get => _stations; set => _stations = value;
    }

    private Station? GetStationByFrequencyAndServiceNumber(int freq, int serviceNumber)
    {
        lock(_lock)
        {
            foreach (var station in _stations)
            {
                if ((station.ServiceNumber == serviceNumber) && (station.Frequency == freq))
                {
                    return station;
                }
            }
            return null;
        }
    }

    private void Demodulator_DynamicLabelChanged(object? sender, EventArgs e)
    {
        if (e is DynamicLabelChangedEventArgs l)
        {
            _lastDynamicLabel = l.Label;
        }
    }

    private Station? GetStationByFreqAndServiceNumber(int freq, int serviceNumber)
    {
        lock (_lock)
        {
            foreach (var station in _stations)
            {
                if (station.Frequency == freq)
                {
                    switch (station.StationType)
                    {
                        case StationTypeEnum.DAB:
                            if (station.ServiceNumber == serviceNumber)
                            {
                                return station;
                            }
                        break;
                        case StationTypeEnum.FM:
                            return station;

                    }

                }
            }
        }
        return null;
    }

    private void Demodulator_OnServiceFound(object? sender, EventArgs e)
    {
        try
        {
            if (e is FMServiceFoundEventArgs fm)
            {
                var freq = _sdrDriver == null ? 0 : _sdrDriver.Frequency;

                // test if already exists
                var station = GetStationByFreqAndServiceNumber(freq, -1);
                if (station != null)
                {
                    return;
                }

                var freqAsString = (freq/1000000.0).ToString("N1") + " MHz";
                var st = new Station(StationTypeEnum.FM, freqAsString, 1, freq);
                lock (_lock)
                {
                    _stations.Add(st);
                }
                _gui.RefreshStations(_stations, st);
            }

            if (e is DABServiceFoundEventArgs dab)
            {
                var snum = Convert.ToInt32(dab?.Service?.ServiceNumber);
                var freq = _sdrDriver == null ?  0 : _sdrDriver.Frequency;

                // test if already exists
                var station = GetStationByFreqAndServiceNumber(freq, snum);
                if (station != null)
                {
                    return;
                }

                var st = GetStationByFrequencyAndServiceNumber(snum, freq);
                if (st == null)
                {
                    // new station
                    st = new Station(StationTypeEnum.DAB, dab?.Service?.ServiceName ?? "Unknown", snum, freq);

                    lock(_lock)
                    {
                        _stations.Add(st);
                    }

                    Station? playingStation = null;
                    if (_demodulator is DABProcessor dp && dp.ServiceNumber != -1)
                    {
                        playingStation = GetStationByFrequencyAndServiceNumber(dp.ServiceNumber, freq);
                    }

                    _gui.RefreshStations(_stations, playingStation);
                }

                // autoplay
                if (_demodulator is DABProcessor dabs)
                {
                    if (dabs.ServiceNumber == -1)
                    {
                        dabs.ServiceNumber = Convert.ToInt32(dab?.Service?.ServiceNumber);
                    }

                    if (dabs.ServiceNumber != dab?.Service?.ServiceNumber)
                    {
                        return;
                    }

                    Task.Run(async () =>
                    {
                        _logger?.Debug($"Autoplay \"{dab.Service.ServiceName}\"");
                        await Task.Delay(2000);
                        var freq = _sdrDriver == null ?  0 : _sdrDriver.Frequency;
                        Play(st);
                        _gui.RefreshStations(_stations, GetStationByFrequencyAndServiceNumber(dabs.ServiceNumber, freq));
                    });
                }
            }
        } finally
        {
            SaveStations();
        }
    }

    private void Play(Station station)
    {
        if ((_demodulator is FMDemodulator) && (station.StationType == StationTypeEnum.DAB))
        {
            BandChanged(this, new BandChangedEventArgs()
            {
                  FM = false
            });
        } else
        if ((_demodulator is DABProcessor) && (station.StationType == StationTypeEnum.FM))
        {
            BandChanged(this, new BandChangedEventArgs()
            {
                  FM = true
            });
        }

        if ((_sdrDriver?.Frequency != station.Frequency) &&
            (station.Frequency !=0))
        {
            FrequentionChanged(this, new FrequentionChangedEventArgs()
            {
                Frequention = station.Frequency
            });
        }

        if (_demodulator is DABProcessor dabs)
        {
            dabs.SetProcessingService(station.ServiceNumber);

            if (_audioPlayer != null)
            {
                _audioPlayer.ClearBuffer();
            }

            _appParams.Config.ServiceNumber = Convert.ToInt32(station.ServiceNumber);
            SaveConfig();
        }
    }

    private void DABProcessor_OnServicePlayed(object? sender, EventArgs e)
    {
       // Not used currently, but can be used to update UI when a service is played.
    }

    private void ProcessAACAudioData(AACDataDemodulatedEventArgs ed)
    {
        try
        {
            // Combine ADTS header and AAC payload into a single buffer before sending to the player/UDP.
            var adtsHeaderLength = ed.ADTSHeader?.Length ?? 0;
            var dataLength = ed.Data?.Length ?? 0;
            var adtsFrame = new byte[adtsHeaderLength + dataLength];
            if (ed.ADTSHeader != null)
            {
                Buffer.BlockCopy(ed.ADTSHeader, 0, adtsFrame, 0, adtsHeaderLength);
            }
            if (ed.Data != null)
            {
                Buffer.BlockCopy(ed.Data, 0, adtsFrame, adtsHeaderLength, dataLength);
            }

            if (!string.IsNullOrWhiteSpace(_appParams.UDP))
            {
                // creating UDP ADTS aac stream:
                // cvlc udp://@:8020 :demux=aac
                // mplayer -nocache -demuxer aac udp://127.0.0.1:8020

                if (_udpStreamer == null)
                {
                    var ipAndPort = _appParams.UDP.Split(":");
                    _udpStreamer = new UDPStreamer(_logger, ipAndPort[0], Convert.ToInt32(ipAndPort[1]));
                }
                _udpStreamer.SendByteArray(adtsFrame, adtsFrame.Length);
            } else
            {
                if (_audioPlayer != null)
                {
                    if (!_rawAudioPlayerInitialized)
                    {
                        var mediaOptions = new[]
                            {
                                ":demux=aac",
                                ":live-caching=0",
                                ":network-caching=0",
                                ":file-caching=0",
                                ":sout-mux-caching=0"
                            };

                            if (ed.AudioDescription == null)
                            {
                                throw new IOException("AudioDescription is null, cannot initialize audio player");
                            }

                        _audioPlayer.Init(ed.AudioDescription, _logger, mediaOptions);
                        _audioPlayer.SetMaxBufferSize(_appParams.Config.AACBufferSize);
                        _audioPlayer.Play();

                        _rawAudioPlayerInitialized = true;
                    }

                    _audioPlayer.AddData(adtsFrame);
                }
            }

            if (!string.IsNullOrWhiteSpace(_appParams.WaveFileName) && (_aacDecoder != null))
            {
                if (_wave == null)
                {
                    if (ed.AACHeader != null)
                    {
                        _aacDecoder?.Init(ed.AACHeader.SBRFlag == SBRFlagEnum.SBRUsed,
                          (int)ed.AACHeader.DacRate,
                           ed.AACHeader.AACChannelMode == AACChannelModeEnum.Mono ? 1 : 2,
                           ed.AACHeader.PSFlag == PSFlagEnum.PSUsed);
                    }

                    _wave = new Wave();
                    _wave.CreateWaveFile(_appParams.WaveFileName, new AudioDataDescription()
                    {
                        BitsPerSample = 16,
                        Channels = 2,
                        SampleRate = 48000
                    } // PCM audio from faad2 is always 16b, 48KHz, stereo
                    );
                }

                var pcmData = ed.Data != null ? _aacDecoder?.DecodeAAC(ed.Data) : null;
                if (pcmData != null)
                {
                    _wave.WriteSampleData(pcmData);
                }
            }

            if (!string.IsNullOrWhiteSpace(_appParams.AACFileName))
            {
                File.AppendAllBytes(_appParams.AACFileName, adtsFrame);
            }
        }
        catch (Exception ex)
        {
            _logger?.Error(ex);
            if (ex.Message != null)
            {
                _logger?.Error(ex.Message);
            }
        }
    }

    private void ProcessPCMAudioData(DataDemodulatedEventArgs ed)
    {
        try
        {
            if ((_audioPlayer != null) && (ed.Data != null) && (ed.Data.Length > 0) && (ed.AudioDescription != null))
            {
                if (!_rawAudioPlayerInitialized)
                {
                    var mediaOptions = new[]
                        {
                            ":demux=rawaud",
                            $":rawaud-channels={ed.AudioDescription.Channels}",
                            $":rawaud-samplerate={ed.AudioDescription.SampleRate}",
                            ":live-caching=50",
                            ":file-caching=50",
                            ":clock-jitter=0",
                            ":clock-synchro=0",
                            ":rawaud-fourcc=s16l"
                        };

                    _audioPlayer.Init(ed.AudioDescription, _logger, mediaOptions);
                    _audioPlayer.SetMaxBufferSize(_appParams.Config.PCMBufferSize);   // 1 s of stereo PCM 16 bit audio
                    _audioPlayer.Play();

                    _rawAudioPlayerInitialized = true;
                }

                _audioPlayer.AddData(ed.Data);
            }

            if (!string.IsNullOrWhiteSpace(_appParams.WaveFileName))
            {
                if ((_wave == null) && (ed.AudioDescription != null))
                {
                    _wave = new Wave();
                    _wave.CreateWaveFile(_appParams.WaveFileName, ed.AudioDescription);
                }
                if (_wave != null && ed.Data != null)
                {
                    _wave.WriteSampleData(ed.Data);
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.Error(ex);
        }
    }

    private void AppConsole_OnDemodulated(object? sender, EventArgs e)
    {
        if (e is AACDataDemodulatedEventArgs ed)
        {
            if (ed.Data == null || ed.Data.Length == 0)
            {
                return;
            }

            _lastDataReceivedTime = DateTime.UtcNow;

            ProcessAACAudioData(ed);
        } else
        {
            if (e is DataDemodulatedEventArgs dd)
            {

                if (dd.Data == null || dd.Data.Length == 0)
                {
                    return;
                }

                _lastDataReceivedTime = DateTime.UtcNow;

                ProcessPCMAudioData(dd);
            }
        }

        OnDemodulated?.Invoke(this, e);
    }

    private void AppConsole_OnFinished(object? sender, EventArgs e)
    {
        Stop();
    }

    public void Stop()
    {
        if (_demodulator is DABProcessor dab)
        {
            dab.Stop();
            dab.Stat(true);
        }

        if (_audioPlayer != null)
        {
            _audioPlayer.Stop();
        }
        _rawAudioPlayerInitialized = false;

        if (_sdrDriver != null)
        {
            _sdrDriver.Disconnect();
        }

        OnFinished?.Invoke(this, new EventArgs());
    }

    private void OutData(byte[] data, int size)
    {
        _demodulator?.AddSamples(data, size);

        _spectrumWorker.AddData(data, size);
    }

    public bool KillAnyProcess(string processName)
    {
        foreach (var process in Process.GetProcessesByName(processName))
        {
            try
            {
                process.Kill(entireProcessTree: true);
                process.WaitForExit(2000);
                process.Close();
                process.Dispose();

            } catch (Exception ex)
            {
                _logger?.Error(ex);
            }
        }

        return !Process.GetProcessesByName(processName).Any();
    }

    private async Task ProcessDriverData()
    {
        if (_sdrDriver == null)
        {
            _logger?.Error("SDR driver is not initialized.");
            return;
        }

        _sdrDriver.SetFrequency(_appParams.Config.Frequency);
        _sdrDriver.SetSampleRate(_appParams.Config.FM ? AudioTools.FMSampleRate : AudioTools.DABSampleRate);

        _sdrDriver.OnDataReceived += (sender, onDataReceivedEventArgs) =>
        {
            if (onDataReceivedEventArgs.Data == null || onDataReceivedEventArgs.Size == 0)
            {
                _logger?.Error("Received empty data from SDR driver.");
                return;
            }

            OutData(onDataReceivedEventArgs.Data, onDataReceivedEventArgs.Size);
        };

        var noProcessRunning = KillAnyProcess("rtl_tcp");
        if (!noProcessRunning)
        {
            _logger?.Error("rtl_tcp is still running!");
        }

        await _sdrDriver.Init(new DriverInitializationResult()
        {
            OutputRecordingDirectory = "/temp"
        });

       SetGain();
    }

    private void SetGain()
    {
        if (_sdrDriver == null)
        {
            return;
        }

        if (_appParams.Config.HWGain)
        {
            _sdrDriver.SetGain(0);
            _sdrDriver.SetGainMode(false);
            _sdrDriver.SetIfGain(true);
            _sdrDriver.SetAGCMode(true);
        } else
        {
            // always manual
            _sdrDriver.SetGainMode(true);
            if (_appParams.Config.SWGain)
            {
                _sdrDriver.SetGain(0);
                Task.Run( async () => await _sdrDriver.AutoSetGain());
            } else
            {
                _sdrDriver?.SetGain(_appParams.Config.Gain);
            }
        }
    }

    private async Task ProcessFile()
    {
        if (string.IsNullOrEmpty(_appParams.InputFileName) || !File.Exists(_appParams.InputFileName))
        {
            _logger?.Error("Input file is not specified or does not exist.");
            return;
        }
        _processingFilePercents = 0;

        await System.Threading.Tasks.Task.Run(() =>
        {
            var bitRateCalculator = new BitRateCalculation(_logger, "Read file");
            var bufferSize = 65535;
            var IQDataBuffer = new byte[bufferSize];

            var lastBufferFillNotify = DateTime.MinValue;

            using (var inputFs = new FileStream(_appParams.InputFileName, FileMode.Open, FileAccess.Read))
            {
                _logger?.Info($"Total bytes : {inputFs.Length}");
                long totalBytesRead = 0;

                while (inputFs.Position < inputFs.Length)
                {
                    var bytesRead = inputFs.Read(IQDataBuffer, 0, bufferSize);
                    totalBytesRead += bytesRead;
                    bitRateCalculator.UpdateBitRate(bytesRead);
                    _processingFileBitRate = bitRateCalculator.BitRateAsShortString;

                    if ((DateTime.UtcNow - lastBufferFillNotify).TotalMilliseconds > 1000)
                    {
                        lastBufferFillNotify = DateTime.UtcNow;
                        if (inputFs.Length > 0)
                        {
                            var percents = totalBytesRead / (inputFs.Length / 100);
                            _logger?.Debug($" Processing input file:                   {percents} %");
                            _processingFilePercents = Convert.ToInt32(percents);
                        }
                    }

                    OutData(IQDataBuffer, bytesRead);

                    System.Threading.Thread.Sleep(25);
                }
            }
        });
    }

    private void StartTune(Func<Task> tune)
    {
        _tuneCts = new CancellationTokenSource();

        _tuneTask = Task.Run(() =>
        {
                _ = tune(); // fire-and-forget
        });
    }

    private void StopTune()
    {
        if (_tuneCts is null)
            return;

        _tuneCts.Cancel();
        _tuneCts.Dispose();
        _tuneCts = null;
        if (_tuneTask != null)
        {
            _tuneTask.Dispose();
            _tuneTask = null;
        }
    }
}
