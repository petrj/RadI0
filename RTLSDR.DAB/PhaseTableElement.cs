using System;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The phase table element.
    /// </summary>
    public class PhaseTableElement
    {
        /// <summary>
        /// Gets or sets the k min.
        /// </summary>
        public int KMin { get; set; } = 0;
        /// <summary>
        /// Gets or sets the k max.
        /// </summary>
        public int KMax { get; set; } = 0;
        /// <summary>
        /// Gets or sets the I.
        /// </summary>
        public int I { get; set; } = 0;
        /// <summary>
        /// Gets or sets the N.
        /// </summary>
        public int N { get; set; } = 0;
    }
}
