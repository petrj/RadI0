using System;
using System.Runtime.InteropServices;

namespace RTLSDR.Audio
{
    [StructLayout(LayoutKind.Sequential)]
    /// <summary>
    /// The AAC dec configuration.
    /// </summary>
    public struct AACDecConfiguration
    {
        /// <summary>
        /// The def object type.
        /// </summary>
        public byte defObjectType;
#if OS_WINDOWS
        /// <summary>
        /// The def sample rate.
        /// </summary>
        public uint defSampleRate;
#else
        /// <summary>
        /// The def sample rate.
        /// </summary>
        public ulong defSampleRate;
#endif
        /// <summary>
        /// The output format.
        /// </summary>
        public byte outputFormat;
        /// <summary>
        /// The down matrix.
        /// </summary>
        public byte downMatrix;
        /// <summary>
        /// The use old adts format.
        /// </summary>
        public byte useOldADTSFormat;
        /// <summary>
        /// The dont up sample implicit sbr.
        /// </summary>
        public byte dontUpSampleImplicitSBR;
    }
}
