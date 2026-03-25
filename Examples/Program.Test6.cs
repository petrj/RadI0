using LoggerService;
using RTLSDR.Audio;
using System;

namespace RTLSDR.Examples
{
    internal static partial class Program
    {
        static void PlayDABSuperFramesAndDecodeWithWindowsAAC(ILoggingService loggingService, string samplesPath)
        {
            var aacDecoder = new AACDecoderWindows(loggingService);
            PLayDABSuperFramesAndDecodeWithAAC(loggingService, samplesPath, aacDecoder);
        }
    }
}