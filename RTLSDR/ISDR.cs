using LoggerService;
using System;
using System.Collections.Generic;
using System.Runtime;
using System.Text;

namespace RTLSDR
{
    /// <summary>
    /// Interface for Software Defined Radio (SDR) devices, providing methods and properties
    /// to control and interact with RTL-SDR hardware.
    /// </summary>
    public interface ISDR
    {
        /// <summary>
        /// Gets the name of the SDR device.
        /// </summary>
        string DeviceName { get; }

        /// <summary>
        /// Gets the current state of the SDR driver.
        /// </summary>
        DriverStateEnum State { get; }

        /// <summary>
        /// Gets the settings for the SDR driver.
        /// </summary>
        DriverSettings Settings { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the SDR device is installed.
        /// </summary>
        bool? Installed { get; set; }

        /// <summary>
        /// Gets the current frequency in Hz.
        /// </summary>
        int Frequency { get; }

        /// <summary>
        /// Gets the current gain value.
        /// </summary>
        int Gain { get; }

        /// <summary>
        /// Gets the type of tuner used by the SDR device.
        /// </summary>
        TunerTypeEnum TunerType { get; }

        /// <summary>
        /// Gets the RTL bitrate.
        /// </summary>
        long RTLBitrate { get; }

        /// <summary>
        /// Initializes the SDR device with the provided initialization result.
        /// </summary>
        /// <param name="driverInitializationResult">The result of the driver initialization.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task Init(DriverInitializationResult driverInitializationResult);

        /// <summary>
        /// Automatically sets the gain for optimal performance.
        /// Gain values are determined based on the current frequency and other factors to ensure the best signal quality.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task AutoSetGain();

        /// <summary>
        /// Disconnects from the SDR device.
        /// </summary>
        void Disconnect();

        /// <summary>
        /// Sends a command to the SDR device.
        /// </summary>
        /// <param name="command">The command to send.</param>
        void SendCommand(Command command);

        /// <summary>
        /// Sets the frequency of the SDR device.
        /// </summary>
        /// <param name="freq">The frequency in Hz.</param>
        void SetFrequency(int freq);

        /// <summary>
        /// Sets the frequency correction for the SDR device.
        /// </summary>
        /// <param name="correction">The correction value.</param>
        void SetFrequencyCorrection(int correction);

        /// <summary>
        /// Sets the sample rate for the SDR device.
        /// </summary>
        /// <param name="sampleRate">The sample rate in Hz.</param>
        void SetSampleRate(int sampleRate);

        /// <summary>
        /// Sets the direct sampling mode.
        /// </summary>
        /// <param name="value">The direct sampling value.</param>
        void SetDirectSampling(int value);

        /// <summary>
        /// Sets the gain mode (manual or automatic).
        /// </summary>
        /// <param name="manual">True for manual gain, false for automatic.</param>
        void SetGainMode(bool manual);

        /// <summary>
        /// Sets the gain value.
        /// </summary>
        /// <param name="gain">The gain value.</param>
        void SetGain(int gain);

        /// <summary>
        /// Sets the IF gain mode.
        /// </summary>
        /// <param name="ifGain">True to enable IF gain, false otherwise.</param>
        void SetIfGain(bool ifGain);

        /// <summary>
        /// Sets the AGC (Automatic Gain Control) mode.
        /// </summary>
        /// <param name="automatic">True for automatic AGC, false for manual.</param>
        void SetAGCMode(bool automatic);

        /// <summary>
        /// Sets the SDR device to an error state.
        /// </summary>
        void SetErrorState();

        /// <summary>
        /// Event raised when data is received from the SDR device.
        /// </summary>
        event EventHandler<OnDataReceivedEventArgs> OnDataReceived;
    }
}
