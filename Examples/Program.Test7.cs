using LoggerService;
using RTLSDR.Audio;
using RTLSDR.Common;
using RTLSDR.FM;
using System;
using System.IO;
using System.Threading.Tasks;

namespace RTLSDR.Examples
{
    internal partial class Program
    {
        private static void DemodulateFMAndPlayWithALSA(ILoggingService loggingService)
        {
            var demodulator = new FMDemodulator(loggingService);
            var audioPlayer = new AlsaSoundAudioPlayer();
            audioPlayer.Init(new AudioDataDescription
            {
                SampleRate = 96000,
                Channels = 2,
                BitsPerSample = 16
            }, loggingService);
            audioPlayer.Play();

            var playTask = Task.Run(() =>
            {
                var fmRawPath = "/temp/FM.raw";
                if (!File.Exists(fmRawPath))
                {
                    Console.WriteLine($"File not found: {fmRawPath}");
                    return;
                }
                var buffer = new byte[demodulator.BufferSize];
                using var fs = new FileStream(fmRawPath, FileMode.Open, FileAccess.Read);
                int bytesRead;
                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    demodulator.AddSamples(buffer, bytesRead);
                    // Wait for demodulation and get PCM data via event
                    var demodulated = false;
                    DataDemodulatedEventArgs? lastArgs = null;
                    void handler(object? sender, EventArgs e)
                    {
                        if (e is DataDemodulatedEventArgs args)
                        {
                            lastArgs = args;
                            demodulated = true;
                        }
                    }
                    demodulator.OnDemodulated += handler;
                    // Start demodulation
                    demodulator.Start();
                    // Wait for demodulation (simple, not optimal)
                    int wait = 0;
                    while (!demodulated && wait < 100)
                    {
                        Task.Delay(10).Wait();
                        wait++;
                    }
                    demodulator.OnDemodulated -= handler;
                    demodulator.Stop();
                    if (lastArgs != null && lastArgs.Data != null)
                    {
                        audioPlayer.AddData(lastArgs.Data);
                    }
                }
            });
            playTask.Wait();
        }
    }
}
