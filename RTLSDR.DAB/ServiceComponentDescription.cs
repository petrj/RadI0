using System;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The service component description.
    /// </summary>
    public class ServiceComponentDescription
    {
        /// <summary>
        /// Gets or sets the msc stream audio desc.
        /// </summary>
        public MSCStreamAudioDescription MSCStreamAudioDesc { get; set; }
        /// <summary>
        /// Gets or sets the msc stream data desc.
        /// </summary>
        public MSCStreamDataDescription MSCStreamDataDesc { get; set; }
        /// <summary>
        /// Gets or sets the msc packet data.
        /// </summary>
        public MSCPacketDataDescription MSCPacketData { get; set; }

        public ServiceComponentDescription()
        {
            MSCPacketData = new MSCPacketDataDescription();
            MSCStreamDataDesc = new MSCStreamDataDescription();
            MSCStreamAudioDesc = new MSCStreamAudioDescription();
        }
    }
}
