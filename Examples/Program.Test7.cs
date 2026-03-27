using LoggerService;
using RTLSDR.Audio;
using RTLSDR.Common;
using RTLSDR.FM;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RTLSDR.Examples
{
    internal static partial class Program
    {
        private static void DemodulateFMAndPlayWithALSA(ILoggingService loggingService)
        {
            var demodulator = new FMDemodulator(loggingService);
            var audioPlayer = new AlsaSoundAudioPlayer();
            var audioDesc = new AudioDataDescription
            {
                SampleRate = 96000,
                Channels = 2,
                BitsPerSample = 16
            };
            audioPlayer.Init(audioDesc, loggingService);
            audioPlayer.Play();

            // Use BalanceBuffer for smooth playback
            var balanceBuffer = new BalanceBuffer(loggingService, audioPlayer.AddData);
            balanceBuffer.SetAudioDataDescription(audioDesc);

            demodulator.OnDemodulated += (sender, e) =>
            {
                if (e is DataDemodulatedEventArgs args && args.Data != null)
                {
                    balanceBuffer.AddData(args.Data);
                }
            };

            var playTask = Task.Run(() =>
            {
                var fmRawPath = "/temp/FM.raw";
                if (!File.Exists(fmRawPath))
                {
                    Console.WriteLine($"File not found: {fmRawPath}");
                    return;
                }
                var buffer = new byte[demodulator.BufferSize];

                using (FileStream fs = new(fmRawPath, FileMode.Open, FileAccess.Read))
                {
                    int bytesRead;
                    demodulator.Start();
                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        demodulator.AddSamples(buffer, bytesRead);
                        Task.Delay(20).Wait();
                    }
                    demodulator.Stop();
                    balanceBuffer.Stop();
                }
            });
            playTask.Wait();
        }
    }
}
