using System;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The aac seper frame hader demodulated event args.
    /// </summary>
    public class AACSeperFrameHaderDemodulatedEventArgs : EventArgs
    {
        /// <summary>
        /// The header.
        /// </summary>
        public AACSuperFrameHeader? Header;
    }
}
