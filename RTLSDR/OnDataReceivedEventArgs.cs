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
        public byte[]? Data { get; set; }

        /// <summary>
        /// Gets or sets the size of the data.
        /// </summary>
        public int Size { get; set; } = 0;
    }
}
