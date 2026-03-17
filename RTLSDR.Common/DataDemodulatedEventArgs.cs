using System;

namespace RTLSDR.Common
{
    /// <summary>
    /// Event arguments for when data has been demodulated, containing audio data and descriptions.
    /// </summary>
    public class DataDemodulatedEventArgs : EventArgs
    {
        /// <summary>
        /// PCM Audio Data
        /// </summary>
        public byte[] Data { get; set; }

        /// <summary>
        /// Description of the audio data parameters.
        /// </summary>
        public AudioDataDescription AudioDescription { get; set; }

        /// <summary>
        /// The ADTS frame data if applicable.
        /// </summary>
        public byte[] ADTSFrame { get; set; }
    }
}
