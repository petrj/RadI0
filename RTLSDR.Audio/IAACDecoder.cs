using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR.Audio
{
    /// <summary>
    /// Defines the iaac decoder.
    /// </summary>
    public interface IAACDecoder
    {

        /// <summary>
        /// Init
        /// </summary>
        /// <param name="dacRate">0 .. 32khz, 1 .. 48khz</param>
        /// <param name="sbr">SBR used</param>
        /// <param name="channels">1 .. mono, 2 .. stereo</param>
        /// <param name="ps">PS used</param>
        public bool Init(bool sbrUsed, int dacRate, int channels, bool psUsed);
        byte[]? DecodeAAC(byte[] aacData);
        void Close();
    }
}
