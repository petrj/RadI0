using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR.Audio
{
    [StructLayout(LayoutKind.Sequential)]
    /// <summary>
    /// The AAC dec frame info linux.
    /// </summary>
    public struct AACDecFrameInfoLinux
    {
        /// <summary>
        /// The bytesconsumed.
        /// </summary>
        public ulong bytesconsumed;
        /// <summary>
        /// The samples.
        /// </summary>
        public ulong samples;

        /// <summary>
        /// The channels.
        /// </summary>
        public char channels;
        /// <summary>
        /// The error.
        /// </summary>
        public char error;

        /// <summary>
        /// The samplerate.
        /// </summary>
        public long samplerate;

        /// <summary>
        /// The sbr.
        /// </summary>
        public char sbr;
        /// <summary>
        /// The object type.
        /// </summary>
        public char object_type;
        /// <summary>
        /// The header type.
        /// </summary>
        public char header_type;
        /// <summary>
        /// The num front channels.
        /// </summary>
        public char num_front_channels;
        /// <summary>
        /// The num side channels.
        /// </summary>
        public char num_side_channels;
        /// <summary>
        /// The num back channels.
        /// </summary>
        public char num_back_channels;
        /// <summary>
        /// The num lfe channels.
        /// </summary>
        public char num_lfe_channels;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        /// <summary>
        /// The channel position.
        /// </summary>
        public char[] channel_position;
        /// <summary>
        /// The ps.
        /// </summary>
        public char ps;
    }
}
