using System;
namespace RTLSDR.DAB
{
    /// <summary>
    /// The fic queue item.
    /// </summary>
    public struct FICQueueItem
    {
        /// <summary>
        /// Gets or sets the fic no.
        /// </summary>
        public int FicNo{ get; set; }
        /// <summary>
        /// Gets or sets the data.
        /// </summary>
        public sbyte[]? Data { get; set; }
    }
}
