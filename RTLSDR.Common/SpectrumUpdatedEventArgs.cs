using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR.Common
{
    /// <summary>
    /// Event arguments for spectrum update events, containing data points and axis limits.
    /// </summary>
    public class SpectrumUpdatedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the spectrum data points.
        /// </summary>
        public Point[]? Data { get; set; }
        public int YMax { get; set; }
        public int YMin { get; set; }
        public int XMax { get; set; }
        public int XMin { get; set; }
    }
}
