using System;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The msc stream description.
    /// </summary>
    public class MSCStreamDescription : MSCDescription
    {
        /// <summary>
        /// Gets or sets the sub ch id.
        /// </summary>
        public uint SubChId { get; set; }  // (Sub-channel Identifier) : this 6-bit field shall identify the sub-channel in which the service
    }
}
