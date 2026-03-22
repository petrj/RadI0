using LoggerService;
using RTLSDR;
using RTLSDR.Common;
using RTLSDR.Audio;
using RTLSDR.DAB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace RTLSDR.Examples
{
    internal class Program
    {
        static void PLayAudioWithLibVLC(ILoggingService loggingService,
            string samplesPath)
        {
            var appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var audioPlayer = new VLCSoundAudioPlayer();

            audioPlayer.Init(new AudioDataDescription
            {
                SampleRate = 44100,
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

            var playTask = Task.Run(() =>
            {
                var fPath = Path.Combine(samplesPath, "sample.aac");
                PlayAudio(fPath, audioPlayer);
            });
            playTask.Wait();
        }

        static void PLayWAVEAudioWithLibVLC(ILoggingService loggingService,
            string samplesPath)
        {
            var appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var audioPlayer = new VLCSoundAudioPlayer();

            audioPlayer.Init(new AudioDataDescription
            {
                SampleRate = 44100,
                Channels = 2,
                BitsPerSample = 16
            }, loggingService);

            audioPlayer.Play();

            var playTask = Task.Run(() =>
            {
                var fPath = Path.Combine(samplesPath, "sample.wav");
                PlayAudio(fPath, audioPlayer);
            });
            playTask.Wait();
        }

        private static void PlayAudio(string fname, IRawAudioPlayer audioPlayer)
        {
            if (audioPlayer == null)
                throw new ArgumentNullException(nameof(audioPlayer));

            Console.WriteLine($"PlayAudio: fname={fname}");
            Console.WriteLine($"PlayAudio: file exists={System.IO.File.Exists(fname)}");
            Console.WriteLine($"PlayAudio: player null={audioPlayer == null}");

            try
            {
                using var fs = new FileStream(fname, FileMode.Open, FileAccess.Read);
                var buffer = new byte[1024];
                int bytesRead;
                while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                {
                    var bufferPart = new byte[bytesRead];
                    Buffer.BlockCopy(buffer, 0, bufferPart, 0, bytesRead);
                    audioPlayer!.AddData(bufferPart);

                    Thread.Sleep(20); // Sleep to simulate real-time playback, adjust as needed
                }

                Console.WriteLine("Finished playing audio.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing audio: {ex}");
            }
        }

        private static void PLayWAVEAudioWithALSA(ILoggingService loggingService,
            string samplesPath)
        {
            var appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var audioPlayer = new AlsaSoundAudioPlayer();

            audioPlayer.Init(new AudioDataDescription
            {
                SampleRate = 44100,
                Channels = 2,
                BitsPerSample = 16
            }, loggingService);

            audioPlayer.Play();

            var playTask = Task.Run(() =>
            {
                var fPath = Path.Combine(samplesPath, "sample.wav");
                PlayAudio(fPath, audioPlayer);
            });
            playTask.Wait();
        }

        static void Main(string[] args)
        {
            var appPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? string.Empty;

            var loggingService = new NLogLoggingService( Path.Combine(appPath,"NLog.config"));

            AppDomain.CurrentDomain.UnhandledException += (s,e) =>
            {
                loggingService.Error(e.ExceptionObject as Exception);
            };

            Console.WriteLine("RTL-SDR Test/Example");

            Console.WriteLine("List of examples:");
            Console.WriteLine("1. Play AAC audio sample (adts header) using libVLC");
            Console.WriteLine("2. Play PCM WAVE audio sample using libVLC");
            Console.WriteLine("3. Linux - Play PCM WAVE audio sample using ALSA");

            Console.Write("Press number:");

            var samplesPath = System.IO.Path.Combine(appPath, "samples/");

            var key = Console.ReadLine();
            //var key = "3"; // For testing
            switch (key)
            {
                case "1":
                    PLayAudioWithLibVLC(loggingService, samplesPath);
                    break;
                case "2":
                    PLayWAVEAudioWithLibVLC(loggingService, samplesPath);
                    break;
                case "3":
                    PLayWAVEAudioWithALSA(loggingService, samplesPath);
                    break;
                default:
                    Console.WriteLine("Invalid option");
                    break;
            }
        }

    }
}
