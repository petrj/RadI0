using System;

namespace RTLSDR.Common
{
    public class DataDemodulatedEventArgs : EventArgs
    {
        /// <summary>
        /// PCM Audio Data
        /// </summary>
        public byte[] Data { get; set; }
        public AudioDataDescription AudioDescription { get; set; }

        public byte[] ADTSFrame { get; set; }
    }
}
