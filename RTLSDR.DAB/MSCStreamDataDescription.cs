using System;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The msc stream data description.
    /// </summary>
    public class MSCStreamDataDescription : MSCStreamDescription
    {
        /// <summary>
        /// Gets or sets the data service component type.
        /// </summary>
        public uint DataServiceComponentType { get; set; }   //  ETSI TS 101 756 [3], table 2b.
    }
}
