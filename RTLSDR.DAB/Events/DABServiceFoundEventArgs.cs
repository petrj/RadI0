using System;
using System.Collections.Generic;
using System.Text;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The DAB service found event args.
    /// </summary>
    public class DABServiceFoundEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the Service.
        /// </summary>
        public DABService? Service { get; set; }
    }
}
