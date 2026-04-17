using System;
using System.Collections.Generic;
using Terminal.Gui;
using NStack;
using LoggerService;
using RTLSDR.Audio;
using RTLSDR;
using RTLSDR.Common;

namespace RadI0
{

    /// <summary>
    /// The RaidI0 config.
    /// </summary>
    public class RaidI0Config
    {
        /// <summary>
        /// Gets or sets a value indicating whether true.
        /// </summary>
        public bool HWGain { get; set; } = true;
        /// <summary>
        /// Gets or sets a value indicating whether false.
        /// </summary>
        public bool FM { get; set; } = false;
        /// <summary>
        /// Gets or sets a value indicating whether false.
        /// </summary>
        public bool DAB { get; set; } = false;
        /// <summary>
        /// Gets or sets a value indicating whether false.
        /// </summary>
        public bool Mono { get; set; } = false;

        /// <summary>
        /// Gets or sets the ServiceNumber.
        /// </summary>
        public int ServiceNumber { get; set; } = -1;

        /// <summary>
        /// Gets or sets the Gain.
        /// </summary>
        public int Gain { get; set;} = 0;
        /// <summary>
        /// Gets or sets a value indicating whether false.
        /// </summary>
        public bool SWGain { get; set;} = false;

        /// <summary>
        /// Frequency.
        /// </summary>
        public int Frequency { get; set; } = -1;

        /// <summary>
        /// Gets or sets the PCMBufferSize.
        /// </summary>
        public int PCMBufferSize { get; set; } = 96000*16*2/8; //  1 s of stereo PCM 16 bit audio
        /// <summary>
        /// Gets or sets the AACBufferSize.
        /// </summary>
        public int AACBufferSize { get; set; } = 8000;
    }


}