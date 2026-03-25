using LoggerService;
using RTLSDR.Audio;
using System;

namespace RTLSDR.Examples
{
    internal partial class Program
    {
        static void PlayDABSuperFramesAndDecodeWithLinuxAAC(ILoggingService loggingService, string samplesPath)
        {
            var aacDecoder = new AACDecoderLinux(loggingService);
            PLayDABSuperFramesAndDecodeWithAAC(loggingService, samplesPath, aacDecoder);
        }
    }
}