using LoggerService;
using NStack;
using RTLSDR;
using RTLSDR.Audio;
using RTLSDR.DAB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Terminal.Gui;

namespace RadI0
{
    internal static class Program
    {
        static async Task Main(string[] args)
        {
            var appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

            var loggingService = new NLogLoggingService( Path.Combine(appPath ?? "", "NLog.config"));

            AppDomain.CurrentDomain.UnhandledException += (s,e) =>
            {
                loggingService.Error(e.ExceptionObject as Exception);
            };

            var appParams = new AppParams("RadI0");
            if (!appParams.ParseArgs(args))
            {
                return;
            }

            IAACDecoder ?aacDecoder = null;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                aacDecoder = new AACDecoderWindows(loggingService);
            }
            else // OSPlatform.Linux??
            {
                aacDecoder = new AACDecoderLinux(loggingService);
            }

            var sdrDriver = new RTLSDRPCDriver(loggingService);

            var gui = new RadI0GUI();
            var app = new RadI0App(sdrDriver,loggingService,gui, appParams, aacDecoder);
            _ = Task.Run(() => app.StartAsync(args));
            gui.Run();
        }
    }
}
