using System;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The msc packet data description.
    /// </summary>
    public class MSCPacketDataDescription : MSCDescription
    {
        /// <summary>
        /// Gets or sets the service component identifier.
        /// </summary>
        public uint ServiceComponentIdentifier { get; set; }   // this 12-bit field shall uniquely identify the service component within the ensemble
    }
}
