using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The sub channel found event args.
    /// </summary>
    public class SubChannelFoundEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the sub channel.
        /// </summary>
        public DABSubChannel? SubChannel { get; set; }
    }
}
