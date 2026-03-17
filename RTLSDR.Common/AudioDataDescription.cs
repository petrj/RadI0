using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR.Common
{
    /// <summary>
    /// Describes the audio data parameters such as sample rate, channels, bits per sample, and SBR flag.
    /// </summary>
    public class AudioDataDescription
    {
        /// <summary>
        /// Gets or sets the sample rate of the audio in Hz.
        /// </summary>
        public int SampleRate { get; set; } = 0;

        /// <summary>
        /// Gets or sets the number of audio channels (e.g., 1 for mono, 2 for stereo).
        /// </summary>
        public short Channels { get; set; } = 0;

        /// <summary>
        /// Gets or sets the number of bits per sample (e.g., 16 for 16-bit audio).
        /// </summary>
        public short BitsPerSample { get; set; } = 0;

        /// <summary>
        /// Gets or sets a value indicating whether Spectral Band Replication (SBR) is used.
        /// </summary>
        public bool SBR { get; set; } = false;
    }
}
