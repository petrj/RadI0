using System;

namespace RTLSDR.Common
{
    /// <summary>
    /// Interface for demodulators that process IQ data and produce audio output.
    /// </summary>
    public interface IDemodulator
    {
        /// <summary>
        /// Gets or sets the sample rate for the demodulator.
        /// </summary>
        int Samplerate { get; set; }

        /// <summary>
        /// Gets the current audio bitrate.
        /// </summary>
        double AudioBitrate { get; }

        /// <summary>
        /// Adds IQ data samples for processing.
        /// </summary>
        /// <param name="IQData">The IQ data buffer.</param>
        /// <param name="length">The length of the data to process.</param>
        void AddSamples(byte[] IQData, int length);

        /// <summary>
        /// Inform that all data from input has been processed
        /// </summary>
        void Finish();

        /// <summary>
        /// Starts the demodulation process.
        /// </summary>
        void Start();

        /// <summary>
        /// Stops the demodulation process.
        /// </summary>
        void Stop();

        /// <summary>
        /// Gets a status string for the demodulator.
        /// </summary>
        /// <param name="detailed">Whether to include detailed information.</param>
        /// <returns>A status string.</returns>
        string Stat(bool detailed);

        /// <summary>
        /// Gets a value indicating whether the demodulator is synced.
        /// </summary>
        bool Synced { get; }

        /// <summary>
        /// Gets the current queue size.
        /// </summary>
        int QueueSize { get; }

        /// <summary>
        /// Event raised when data has been demodulated.
        /// </summary>
        event EventHandler OnDemodulated;

        /// <summary>
        /// Event raised when demodulation is finished.
        /// </summary>
        event EventHandler OnFinished;

        /// <summary>
        /// Event raised when a service is found.
        /// </summary>
        event EventHandler OnServiceFound;

    }
}
