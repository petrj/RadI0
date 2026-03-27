using LoggerService;
using NLog;
using RTLSDR.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RTLSDR
{
    /// <summary>
    /// RTL-SDR driver
    /// https://hz.tools/rtl_tcp/
    /// </summary>
    public class RTLSDRDriver : ISDR
    {
        private Socket? _socket = null;
        private readonly object _lock = new object();

        public bool? Installed { get; set; } = null;

        private int _gain = 0;

        private const int ReadBufferSize = 1000000; // 1 MB buffer

        private const int RecordBufferSize = 1000000; // 1 MB buffer
        private readonly object _recordLock = new object();

        private readonly List<byte>_recordBuffer = new List<byte>();
        private bool _recording = false;

        public DriverStateEnum State { get; private set; } = DriverStateEnum.NotInitialized;

        public DriverSettings Settings { get; private set; }

        public Queue<Command> _commandQueue;

        private int[]? _supportedTcpCommands;

        private TunerTypeEnum _tunerType = TunerTypeEnum.RTLSDR_TUNER_UNKNOWN;

        private string? _magic = null;
        private string? _deviceName = null;

        private int _frequency = 104000000;

        private Thread? _dataWorker = null;
        private CancellationTokenSource? _dataWorkerCancellationTokenSource = null;

        private Thread? _commandWorker = null;
        private CancellationTokenSource? _commandWorkerCancellationTokenSource = null;

        private NetworkStream? _stream = null;
        protected ILoggingService? _loggingService = null;

        private double _RTLBitrate = 0;
        private double _powerPercent = 0;
        private readonly double _power = 0;
        private int _gainCount = 0;

        public event EventHandler<OnDataReceivedEventArgs> OnDataReceived = null!;

        /// <summary>
        /// Gets the current gain value.
        /// </summary>
        public int Gain
        {
            get
            {
                return _gain;
            }
        }

        /// <summary>
        /// Starts recording data.
        /// </summary>
        public void StartRecord()
        {
            _recording = true;
            lock (_recordLock)
            {
                _recordBuffer.Clear();
            }
        }

        /// <summary>
        /// Stops recording and returns the recorded data.
        /// </summary>
        /// <returns>The recorded data as a byte array.</returns>
        public byte[] StopRecord()
        {
            byte[] bytes;

            lock (_recordLock)
            {
                bytes =_recordBuffer.ToArray();
                _recordBuffer.Clear();
            }

            return bytes;
        }


        /// <summary>
        /// Gets the name of the SDR device.
        /// </summary>
        public string DeviceName
        {
            get
            {
                return String.IsNullOrEmpty(_deviceName) ? "RTLSDR device" : $"{_deviceName} ({_magic})";
            }
        }

        /// <summary>
        /// Gets the current frequency in Hz.
        /// </summary>
        public int Frequency
        {
            get
            {
                return _frequency;
            }
        }

        /// <summary>
        /// Gets the type of tuner used by the SDR device.
        /// </summary>
        public TunerTypeEnum TunerType
        {
            get
            {
                return _tunerType;
            }
        }

        /// <summary>
        /// Gets the RTL bitrate.
        /// </summary>
        public long RTLBitrate
        {
            get
            {
                return Convert.ToInt32(_RTLBitrate);
            }
        }

        /// <summary>
        /// Gets the power percentage.
        /// </summary>
        public double PowerPercent
        {
            get
            {
                return _powerPercent;
            }
        }

        /// <summary>
        /// Gets the power value.
        /// </summary>
        public double Power
        {
            get
            {
                return _power;
            }
        }

        /// <summary>
        /// Initializes a new instance of the RTLSDRDriver class.
        /// </summary>
        /// <param name="loggingService">The logging service to use.</param>
        public RTLSDRDriver(ILoggingService? loggingService)
        {
            Settings = new DriverSettings();
            _loggingService = loggingService;

            _commandQueue = new Queue<Command>();

            _loggingService?.Info("RTL SDR driver initialized");
        }

        public async Task AutoSetGain()
        {
            _loggingService?.Debug("Setting auto gain");

            var maxGain = 500;
            var minGain = -100;
            var gainStep = 20;

            var maxDiff = 0;
            var maxDiffGain = maxGain;

            var gainDelay = 50;
            var recordDelay = 150;
            var nextLoopDelay = 50;

            var actualGain = maxGain;

            var start = DateTime.Now;

            while (State == DriverStateEnum.Connected)
            {
                SetGain(actualGain);
                await Task.Delay(gainDelay);

                // recording 100 ms buffer
                StartRecord();
                await Task.Delay(recordDelay);
                var buffer = StopRecord();

                if (buffer.Length<200)
                    continue;

                byte min = 255;
                byte max = 0;
                foreach (var b in buffer)
                {
                    if (b<min)
                    {
                        min = b;
                    }
                    if (b>max)
                    {
                        max = b;
                    }
                }

                var diff = max-min;
                if (diff>=maxDiff)
                {
                    maxDiff = diff;
                    maxDiffGain = actualGain;
                }

                await Task.Delay(nextLoopDelay);

                actualGain -= gainStep;
                if (actualGain < minGain)
                {
                    // I am at the end
                    break;
                }
            }

            var msg = $"Setting gain: {maxDiffGain} ({(DateTime.Now-start).TotalSeconds.ToString("N2")} secs)";
            _loggingService?.Info(msg);

            SetGain(maxDiffGain);
        }

        private void DataWorkerThreadLoop(CancellationTokenSource token)
        {
            _loggingService?.Info($"_dataWorker started");

            var buffer = new byte[ReadBufferSize];
            var recordBuffer = new byte[RecordBufferSize];

            var bitRateCalculator = new BitRateCalculation(_loggingService, "SDR");

            while (!token.IsCancellationRequested)
            {
                try
                {
                    if (State == DriverStateEnum.Connected)
                    {
                        var bytesRead = 0;

                        // reading data
                        if (_stream != null && _stream.CanRead)
                        {
                            bytesRead = _stream.Read(buffer, 0, buffer.Length);

                            if (bytesRead > 0)
                            {
                                if (OnDataReceived != null)
                                {
                                    OnDataReceived(this, new OnDataReceivedEventArgs()
                                    {
                                        Data = buffer,
                                        Size = bytesRead
                                    });
                                }

                                if (_recording)
                                {
                                    lock(_recordLock)
                                    {
                                        if (bytesRead+_recordBuffer.Count<RecordBufferSize)
                                        {
                                            var data = new byte[bytesRead];
                                            Buffer.BlockCopy(buffer,0,data,0,bytesRead);
                                            _recordBuffer.AddRange(data);
                                        }
                                    }
                                }
                            }
                            else
                            {
                                _powerPercent = 0;
                            }
                        }
                        else
                        {
                            // no data on input
                            _powerPercent = 0;
                            Thread.Sleep(10);
                        }

                        // calculating speed
                        _RTLBitrate = bitRateCalculator.UpdateBitRate(bytesRead);
                    }
                    else
                    {
                        _RTLBitrate = 0;
                        _powerPercent = 0;

                        // no data on input
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    _loggingService?.Error(ex);
                    State = DriverStateEnum.Error;
                }
            }

            _loggingService?.Info($"_dataWorker finished");
        }

        private void CommandWorkerThreadLoop(CancellationTokenSource token)
        {
            _loggingService?.Info($"_commandWorker started");
    
            // worker can be finished after all commands sent to driver
            var finishWorker = false;

            while (!finishWorker)
            {
                try
                {
                    if (State == DriverStateEnum.Connected)
                    {
                        // executing commands

                        Command? command = null;

                        lock (_lock)
                        {
                            if (_commandQueue.Count > 0)
                            {
                                command = _commandQueue.Dequeue();
                            }

                            finishWorker = token.IsCancellationRequested && (_commandQueue.Count == 0);
                        }

                        if (command != null)
                        {
                            _loggingService?.Info($"Sending command: {command}");

                            if (_stream == null || !_stream.CanWrite)
                            {
                                throw new IOException("Cannot write to stream");
                            }

                            _stream.Write(command.ToByteArray(), 0, 5);

                            _loggingService?.Info($"Command {command} sent");
                        }
                    }
                    else
                    {
                        // no data on input
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    _loggingService?.Error(ex);
                    State = DriverStateEnum.Error;
                }
            }

            _loggingService?.Info($"_commandWorker finished");
        }

        public async Task Init(DriverInitializationResult driverInitializationResult)
        {
            _loggingService?.Info($"Initializing driver {driverInitializationResult.DeviceName}");

            if (driverInitializationResult != null)
            {
                _supportedTcpCommands = driverInitializationResult.SupportedTcpCommands??new int[0];

                if (_supportedTcpCommands != null)
                {
                    _loggingService?.Info($"Driver supported commands:");
                    foreach (var cmd in _supportedTcpCommands)
                    {
                        _loggingService?.Info($"{(CommandsEnum)cmd} ({cmd})");
                    }
                }

                _deviceName = driverInitializationResult.DeviceName??"Unknown device";
            }

            await Connect();
        }

        protected virtual async Task Connect()
        {
            try
            {
                _loggingService?.Info($"Connecting driver on {Settings.IP}:{Settings.Port}");

                var ipAddress = IPAddress.Parse(Settings.IP);
                var endPoint = new IPEndPoint(ipAddress, Settings.Port);

                _socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                // Set socket options:
                _socket.NoDelay = true;
                _socket.ReceiveTimeout = 500;
                _socket.SendTimeout = 500;

                await _socket.ConnectAsync(endPoint);

                _stream = new NetworkStream(_socket, FileAccess.ReadWrite, true);

                // Read magic value:
                byte[] buffer = new byte[4];
                int bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead != buffer.Length)
                {
                    _loggingService?.Error(null, "Could not read magic value");
                    return;
                }
                _magic = Encoding.ASCII.GetString(buffer);

                // Read tuner type:
                bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead != buffer.Length)
                {
                    _loggingService?.Error(null, "Could not read tuner type");
                    return;
                }

                try
                {
                    _tunerType = (TunerTypeEnum)buffer[3];
                }
                catch
                {
                    _loggingService?.Info("Unknown tuner type");
                }

                // Read gain count
                bytesRead = await _stream.ReadAsync(buffer, 0, buffer.Length);
                if (bytesRead != buffer.Length)
                {
                    _loggingService?.Error(null, "Could not read gain count");
                    return;
                }

                _gainCount = buffer[3];

                _loggingService?.Info($"Driver connected");

                _loggingService?.Info($"Gain count: {_gainCount}");
                _dataWorkerCancellationTokenSource = new CancellationTokenSource();
                _dataWorker = new Thread( () => DataWorkerThreadLoop(_dataWorkerCancellationTokenSource));
                _dataWorker.Start();

                _commandWorkerCancellationTokenSource = new CancellationTokenSource();
                _commandWorker = new Thread(() => CommandWorkerThreadLoop(_commandWorkerCancellationTokenSource));
                _commandWorker.Start();

                State = DriverStateEnum.Connected;
            }
            catch (Exception ex)
            {
                _loggingService?.Error(ex);
                State = DriverStateEnum.Error;
            }
        }

        public virtual void Disconnect()
        {
            _loggingService?.Info($"Disconnecting driver");

            _dataWorkerCancellationTokenSource?.Cancel();
            _dataWorker?.Join();

            _loggingService?.Info($"_dataWorker stopped");

            SendCommand(new Command(CommandsEnum.TCP_ANDROID_EXIT, 0));

            _commandWorkerCancellationTokenSource?.Cancel();
            _commandWorker?.Join();

            _loggingService?.Info($"_commandWorker stopped");

            State = DriverStateEnum.DisConnected;

            if (_stream != null)
            {
                _stream.Close();
                _stream = null;
            }

            if (_socket != null)
            {
                if (_socket.Connected)
                {
                    _socket.Disconnect(true);
                }
                _socket.Close();
                _socket = null;
            }

            _loggingService?.Info($"Driver disconnected");
        }

        /// <summary>
        /// Sets the SDR device to an error state.
        /// </summary>
        public void SetErrorState()
        {
            _loggingService?.Info($"Setting manually error state");
            State = DriverStateEnum.Error;
        }

        /// <summary>
        /// Sends a command to the SDR device.
        /// </summary>
        /// <param name="command">The command to send.</param>
        public void SendCommand(Command command)
        {
            _loggingService?.Info($"Enqueue command: {command}");

            lock (_lock)
            {
                _commandQueue.Enqueue(command);
            }
        }

        /// <summary>
        /// Sets the frequency of the SDR device.
        /// </summary>
        /// <param name="freq">The frequency in Hz.</param>
        public void SetFrequency(int freq)
        {
            _loggingService?.Info($"Setting frequency: {freq}");

            SendCommand(new Command(CommandsEnum.TCP_SET_FREQ, freq));

            _frequency = freq;
        }

        /// <summary>
        /// Sets the frequency correction for the SDR device.
        /// </summary>
        /// <param name="correction">The correction value.</param>
        public void SetFrequencyCorrection(int correction)
        {
            _loggingService?.Info($"Setting frequency correction: {correction}");

            SendCommand(new Command(CommandsEnum.TCP_SET_FREQ_CORRECTION, correction));
        }

        /// <summary>
        /// Sets the sample rate for the SDR device.
        /// </summary>
        /// <param name="sampleRate">The sample rate in Hz.</param>
        public void SetSampleRate(int sampleRate)
        {
            _loggingService?.Info($"Setting sample rate: {sampleRate}");
            SendCommand(new Command(CommandsEnum.TCP_SET_SAMPLE_RATE, sampleRate));

            Settings.SDRSampleRate = sampleRate;
        }

        /// <summary>
        /// Sets the direct sampling mode.
        /// </summary>
        /// <param name="value">The direct sampling value.</param>
        public void SetDirectSampling(int value)
        {
            _loggingService?.Info($"Setting direct sampling: {value}");
            SendCommand(new Command(CommandsEnum.TCP_SET_DIRECT_SAMPLING, value));
        }


        /// <summary>
        /// Setting gain mode
        /// </summary>
        /// <param name="manual">
        ///     true  => manual (1)
        ///     false => auto (0)</param>
        public void SetGainMode(bool manual)
        {
            _loggingService?.Info($"Setting {(manual ? "manual" : "automatic")} gain mode");

            SendCommand(new Command(CommandsEnum.TCP_SET_GAIN_MODE, (int) (manual ? 1 : 0)));
        }

        /// <summary>
        /// Sets the gain value.
        /// </summary>
        /// <param name="gain">The gain value.</param>
        public void SetGain(int gain)
        {
            _loggingService?.Info($"Setting gain: {gain}");

            _gain = gain;

            SendCommand(new Command(CommandsEnum.TCP_SET_GAIN, gain));
        }

        /// <summary>
        /// Sets the IF gain mode.
        /// </summary>
        /// <param name="ifGain">True to enable IF gain, false otherwise.</param>
        public void SetIfGain(bool ifGain)
        {
            _loggingService?.Info($"Setting ifGain: {(ifGain ? "YES" : "NO")}");

            SendCommand(new Command(CommandsEnum.TCP_SET_IF_TUNER_GAIN, (short)(ifGain ? 1 : 0)));
        }

        /// <summary>
        /// Automatic Gain Control
        /// </summary>
        ///     true  => automatic AGC on (1)
        ///     false => automatic AGC off (0)</param>
        public void SetAGCMode(bool automatic)
        {
            _loggingService?.Info($"Setting AGC: {(automatic ? "YES" : "NO")}");
            SendCommand(new Command(CommandsEnum.TCP_SET_AGC_MODE, (int)(automatic ? 1 : 0)));
        }
    }
}
