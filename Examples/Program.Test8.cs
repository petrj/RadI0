using LoggerService;
using RTLSDR.Audio;
using RTLSDR.Common;
using RTLSDR.DAB;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RTLSDR.Examples
{
    internal static partial class Program
    {
        private static void DemodulateDABAndPlayWithVLC(ILoggingService loggingService)
        {
            var audioPlayer = new VLCSoundAudioPlayer();

            audioPlayer.Init(new AudioDataDescription
            {
                SampleRate = 48000,
                Channels = 2,
                BitsPerSample = 16
            }, loggingService,
            new[]
            {
                ":demux=aac",
                ":live-caching=0",
                ":network-caching=0",
                ":file-caching=0",
                ":sout-mux-caching=0"
            });

            audioPlayer.SetMaxBufferSize(8000);
            audioPlayer.Play();

            var dabProcessor = new DABProcessor(loggingService);
            var serviceSelected = false;

            dabProcessor.OnServiceFound += (sender, e) =>
            {
                if (!serviceSelected && e is DABServiceFoundEventArgs args && args.Service != null)
                {
                    Console.WriteLine($"DAB service found: {args.Service.ServiceNumber} '{args.Service.ServiceName}'");
                    dabProcessor.SetProcessingService(args.Service);
                    serviceSelected = true;
                }
            };

            dabProcessor.OnServicePlayed += (sender, e) =>
            {
                if (e is DABServicePlayedEventArgs played)
                {
                    Console.WriteLine($"Processing DAB service {played.Service?.ServiceNumber} '{played.Service?.ServiceName}'");
                }
            };

            dabProcessor.OnDemodulated += (sender, e) =>
            {
                if (e is AACDataDemodulatedEventArgs args && args.Data != null)
                {
                    if (args.ADTSHeader != null)
                    {
                        var adtsFrame = new byte[args.ADTSHeader.Length + args.Data.Length];
                        Buffer.BlockCopy(args.ADTSHeader, 0, adtsFrame, 0, args.ADTSHeader.Length);
                        Buffer.BlockCopy(args.Data, 0, adtsFrame, args.ADTSHeader.Length, args.Data.Length);
                        audioPlayer.AddData(adtsFrame);
                    }
                }
            };

            var playTask = Task.Run(() =>
            {
                var dabRawPath = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.MyMusic), "DAB.raw");
                if (!File.Exists(dabRawPath))
                {
                    Console.WriteLine($"File not found: {dabRawPath}");
                    return;
                }

                var buffer = new byte[64 * 1024];

                using FileStream fs = new(dabRawPath, FileMode.Open, FileAccess.Read);
                dabProcessor.Start();

                int bytesRead;
                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    dabProcessor.AddSamples(buffer, bytesRead);
                    Thread.Sleep(20);
                }

                dabProcessor.Stop();
            });

            playTask.Wait();

            Thread.Sleep(200);
            audioPlayer.Stop();
        }
    }
}
