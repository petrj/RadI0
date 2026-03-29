using System;
using System.Collections.Generic;
using System.Text;

namespace RTLSDR
{
    /// <summary>
    /// OnDataReceived event data.
    /// </summary>
    public class OnDataReceivedEventArgs
    {
        /// <summary>
        /// Gets or sets the Data.
        /// </summary>
        public byte[]? Data { get; set; }

        /// <summary>
        /// Gets or sets the size of the data.
        /// </summary>
        public int Size { get; set; } = 0;
    }
}
