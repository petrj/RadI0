using System;
using System.Collections.Generic;
using System.Text;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The DAB service played event args.
    /// </summary>
    public class DABServicePlayedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the Service.
        /// </summary>
        public DABService? Service { get; set; }
        /// <summary>
        /// Gets or sets the sub channel.
        /// </summary>
        public DABSubChannel? SubChannel { get; set; }
    }
}
