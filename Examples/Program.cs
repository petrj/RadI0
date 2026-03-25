
using LoggerService;
using RTLSDR;
using RTLSDR.Common;
using RTLSDR.Audio;
using RTLSDR.DAB;
using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RTLSDR.Examples
{
    internal partial class Program
    {
        static void Main(string[] args)
        {
            var appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty;

            var loggingService = new NLogLoggingService(Path.Combine(appPath, "NLog.config"));

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                loggingService.Error(e.ExceptionObject as Exception);
            };

            Console.WriteLine("RTL-SDR Test/Example");
            Console.WriteLine("List of examples:");
            Console.WriteLine("1. Play AAC audio sample (adts header) using libVLC");
            Console.WriteLine("2. Play PCM WAVE audio sample using libVLC");
            Console.WriteLine("3. Linux - Play PCM WAVE audio sample using ALSA");
            Console.WriteLine("4. Play DAB AU*.aac superframes with generated ADTS headers using libVLC");
            Console.WriteLine("5. Decode DAB AU*.aac to PCM with Linux AAC decoder and play via raw VLC");
            Console.WriteLine("6. Decode DAB AU*.aac to PCM with Windows AAC decoder and play via raw VLC");

            Console.Write("Press number: ");

            var samplesPath = System.IO.Path.Combine(appPath, "samples/");

            var key = Console.ReadLine();

            switch (key)
            {
                case "1":
                    PlayAudioWithLibVLC(loggingService, samplesPath);
                    break;
                case "2":
                    PlayWAVEAudioWithLibVLC(loggingService, samplesPath);
                    break;
                case "3":
                    PlayWAVEAudioWithALSA(loggingService, samplesPath);
                    break;
                case "4":
                    PlayDABSuperFramesWithLibVLC(loggingService, samplesPath);
                    break;
                case "5":
                    PlayDABSuperFramesAndDecodeWithLinuxAAC(loggingService, samplesPath);
                    break;
                case "6":
                    PlayDABSuperFramesAndDecodeWithWindowsAAC(loggingService, samplesPath);
                    break;
                default:
                    Console.WriteLine("Invalid option");
                    break;
            }
        }
    }
}
