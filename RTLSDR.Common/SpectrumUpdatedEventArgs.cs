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
        /// <summary>
        /// Gets or sets the YMax.
        /// </summary>
        public int YMax { get; set; } = 0;
        /// <summary>
        /// Gets or sets the YMin.
        /// </summary>
        public int YMin { get; set; } = 0;
        /// <summary>
        /// Gets or sets the XMax.
        /// </summary>
        public int XMax { get; set; } = 0;
        /// <summary>
        /// Gets or sets the XMin.
        /// </summary>
        public int XMin { get; set; } = 0;
    }
}
