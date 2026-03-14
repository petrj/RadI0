using LoggerService;
using RTLSDR.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading;

namespace RTLSDR
{
    /// <summary>
    /// Test driver for RTL-SDR, implementing the ISDR interface for testing purposes.
    /// </summary>
    public class RTLSRDTestDriver : ISDR
    {
        public DriverStateEnum State { get; private set; } = DriverStateEnum.NotInitialized;

        private ILoggingService _loggingService;
        private double _bitrate = 0;
        private string _inputDirectory = null;

        /// <summary>
        /// Initializes a new instance of the RTLSRDTestDriver class.
        /// </summary>
        /// <param name="loggingService">The logging service to use.</param>
        public RTLSRDTestDriver(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        /// <summary>
        /// Gets the current gain value.
        /// </summary>
        public int Gain
        {
            get
            {
                return 0;
            }
        }

        private int _frequency = 104000000;

        /// <summary>
        /// Gets the name of the SDR device.
        /// </summary>
        public string DeviceName
        {
            get
            {
                return "Test driver";
            }
        }

        public async Task AutoSetGain()
        {
        }

        public DriverSettings Settings { get; private set; } = new DriverSettings();

        public bool? Installed { get; set; } = true;

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
                return TunerTypeEnum.RTLSDR_TUNER_UNKNOWN;
            }
        }

        /// <summary>
        /// Gets the RTL bitrate.
        /// </summary>
        public long RTLBitrate
        {
            get
            {
                return Convert.ToInt64(_bitrate);
            }
        }

        /// <summary>
        /// Gets the power percentage.
        /// </summary>
        public double PowerPercent
        {
            get
            {
                return 100;
            }
        }

        /// <summary>
        /// Gets the power value.
        /// </summary>
        public double Power
        {
            get
            {
                return 1;
            }
        }

        public event EventHandler<OnDataReceivedEventArgs> OnDataReceived;

        /// <summary>
        /// Disconnects from the SDR device.
        /// </summary>
        public void Disconnect()
        {
            _loggingService.Info($"Disconnecting driver");

            State = DriverStateEnum.DisConnected;
        }

        /// <summary>
        /// Sets the SDR device to an error state.
        /// </summary>
        public void SetErrorState()
        {
            _loggingService.Info($"Setting manually error state");
            State = DriverStateEnum.Error;
        }

        private void ProcessInput()
        {
            new Thread(() =>
            {
                    _loggingService.Info($"RTLSRDTestDriver thread started");

                    var bitRateCalculator = new BitRateCalculation(_loggingService, "Test driver");

                    State = DriverStateEnum.Connected;

                    var lastBufferFillNotify = DateTime.MinValue;

                    var fName = Path.Combine(_inputDirectory, (Frequency <= 108000000 ? "FM.raw" : "DAB.raw"));
                    var bufferSize = Frequency <= 108000000 ? 125 * 1024 : 1024 * 1024;

                    var IQDataBuffer = new byte[bufferSize];

                    using (var inputFs = new FileStream(fName, FileMode.Open, FileAccess.Read))
                    {
                        _loggingService.Info($"Total bytes : {inputFs.Length}");
                        long totalBytesRead = 0;

                        while (inputFs.Position < inputFs.Length && State == DriverStateEnum.Connected)
                        {
                            var bytesRead = inputFs.Read(IQDataBuffer, 0, bufferSize);
                            totalBytesRead += bytesRead;

                            if (OnDataReceived != null)
                            {
                                //_powerPercent = Demodulator.PercentSignalPower;

                                OnDataReceived(this, new OnDataReceivedEventArgs()
                                {
                                    Data = IQDataBuffer,
                                    Size = bytesRead
                                });

                                _bitrate = bitRateCalculator.UpdateBitRate(bytesRead);
                            }

                            if ((DateTime.Now - lastBufferFillNotify).TotalMilliseconds > 1000)
                            {
                                lastBufferFillNotify = DateTime.Now;
                                if (inputFs.Length > 0)
                                {
                                    var percents = totalBytesRead / (inputFs.Length / 100);
                                    _loggingService.Debug($" Processing input file:                   {percents} %");
                                }
                            }

                            System.Threading.Thread.Sleep(16);
                        }
                    }

                    _bitrate = 0;

            }).Start();
        }

        public async Task Init(DriverInitializationResult driverInitializationResult)
        {
            _inputDirectory = driverInitializationResult.OutputRecordingDirectory;

            if (string.IsNullOrEmpty(_inputDirectory))
            {
                throw new Exception("No input directory");
            }
        }

        /// <summary>
        /// Sends a command to the SDR device.
        /// </summary>
        /// <param name="command">The command to send.</param>
        public void SendCommand(Command command)
        {

        }

        /// <summary>
        /// Sets the AGC mode.
        /// </summary>
        /// <param name="automatic">True for automatic AGC, false for manual.</param>
        public void SetAGCMode(bool automatic)
        {

        }

        /// <summary>
        /// Sets the direct sampling mode.
        /// </summary>
        /// <param name="value">The direct sampling value.</param>
        public void SetDirectSampling(int value)
        {

        }

        /// <summary>
        /// Sets the frequency of the SDR device.
        /// </summary>
        /// <param name="freq">The frequency in Hz.</param>
        public void SetFrequency(int freq)
        {
            _frequency = freq;

            ProcessInput();
        }

        /// <summary>
        /// Sets the frequency correction for the SDR device.
        /// </summary>
        /// <param name="correction">The correction value.</param>
        public void SetFrequencyCorrection(int correction)
        {

        }

        /// <summary>
        /// Sets the gain value.
        /// </summary>
        /// <param name="gain">The gain value.</param>
        public void SetGain(int gain)
        {

        }

        /// <summary>
        /// Sets the gain mode.
        /// </summary>
        /// <param name="manual">True for manual gain, false for automatic.</param>
        public void SetGainMode(bool manual)
        {

        }

        /// <summary>
        /// Sets the IF gain mode.
        /// </summary>
        /// <param name="ifGain">True to enable IF gain, false otherwise.</param>
        public void SetIfGain(bool ifGain)
        {

        }

        /// <summary>
        /// Sets the sample rate for the SDR device.
        /// </summary>
        /// <param name="sampleRate">The sample rate in Hz.</param>
        public void SetSampleRate(int sampleRate)
        {

        }
    }
}
