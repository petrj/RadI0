using System;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The msc stream audio description.
    /// </summary>
    public class MSCStreamAudioDescription : MSCStreamDescription
    {
        /// <summary>
        /// Gets or sets the audio service component type.
        /// </summary>
        public uint AudioServiceComponentType { get; set; }  //  ETSI TS 101 756 [3], table 2a.
    }
}
