using System;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The aac seper frame hader demodulated event args.
    /// </summary>
    public class AACSuperFrameHaderDemodulatedEventArgs : EventArgs
    {
        /// <summary>
        /// The Header.
        /// </summary>
        public AACSuperFrameHeader? Header;
    }
}
