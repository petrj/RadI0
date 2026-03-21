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

    public class RaidI0Config
    {
        public bool HWGain { get; set; } = true;
        public bool FM { get; set; } = false;
        public bool DAB { get; set; } = false;
        public bool Mono { get; set; } = false;

        public int ServiceNumber { get; set; } = -1;

        public int Gain { get; set;} = 0;
        public bool SWGain { get; set;} = false;

        public int Frequency { get; set; } = -1;

        public int SampleRate { get; set; } = 1000000;


        public int PCMBufferSize { get; set; } = 96000*16*2/8; //  1 s of stereo PCM 16 bit audio
        public int AACBufferSize { get; set; } = 8000;
    }


}