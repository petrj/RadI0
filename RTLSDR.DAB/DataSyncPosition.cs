using System;
using System.Numerics;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The data sync position.
    /// </summary>
    public class DataSyncPosition
    {
        /// <summary>
        /// Gets or sets a value indicating whether synced.
        /// </summary>
        public bool Synced { get; set; } = false;
        /// <summary>
        /// Gets or sets the start index.
        /// </summary>
        public int StartIndex { get; set; } = -1;
        /// <summary>
        /// Gets or sets the first ofdm buffer.
        /// </summary>
        public Complex[]? FirstOFDMBuffer { get; set; }
    }
}
