using LoggerService;
using RTLSDR;
using RTLSDR.Common;
using RTLSDR.Audio;
using RTLSDR.DAB;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RTLSDR.Examples
{
    internal class Program
    {
        static void PlayAudioWithLibVLC(ILoggingService loggingService,
            string samplesPath)
        {
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

        static void PlayWAVEAudioWithLibVLC(ILoggingService loggingService,
            string samplesPath)
        {
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

        private static void PlayWAVEAudioWithALSA(ILoggingService loggingService,
            string samplesPath)
        {
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

        static void PlayDABSuperFramesWithLibVLC(ILoggingService loggingService, string samplesPath)
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

            var playTask = Task.Run(() =>
            {
                var files = Directory.GetFiles(samplesPath, "AU*.aac").OrderBy(f => f).ToArray();
                if (files.Length == 0)
                {
                    Console.WriteLine("No AU*.aac files found in " + samplesPath);
                    return;
                }

                foreach (var file in files)
                {
                    try
                    {
                        Console.WriteLine($"Processing {Path.GetFileName(file)}");
                        var aacData = File.ReadAllBytes(file);

                        var spHeader = new AACSuperFrameHeader()
                        {
                            DacRate = DacRateEnum.DacRate48KHz,
                            SBRFlag = SBRFlagEnum.SBRUsed,
                            AACChannelMode = AACChannelModeEnum.Stereo
                        };

                        var adtsHeader = ADTSHeader.CreateAdtsHeader((int)AACProfileEnum.AACLC, spHeader.GetCoreSampleRate(), spHeader.GetChannels(), aacData.Length);
                        var adtsFrame = new byte[adtsHeader.Length + aacData.Length];
                        Buffer.BlockCopy(adtsHeader, 0, adtsFrame, 0, adtsHeader.Length);
                        Buffer.BlockCopy(aacData, 0, adtsFrame, adtsHeader.Length, aacData.Length);

                        audioPlayer.AddData(adtsFrame);

                        Thread.Sleep(75);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing {Path.GetFileName(file)}: {ex}");
                    }
                }

                Console.WriteLine("Finished playing DAB AAC superframes.");

                // Keep the player alive for a little while to drain the buffer
                Thread.Sleep(200);
            });

            playTask.Wait();
        }

        static void PlayDABSuperFramesAndDecodeWithLinuxAAC(ILoggingService loggingService, string samplesPath)
        {
            var aacDecoder = new AACDecoderLinux(loggingService);
            PLayDABSuperFramesAndDecodeWithAAC(loggingService, samplesPath, aacDecoder);
        }

        static void PlayDABSuperFramesAndDecodeWithWindowsAAC(ILoggingService loggingService, string samplesPath)
        {
            var aacDecoder = new AACDecoderWindows(loggingService);
            PLayDABSuperFramesAndDecodeWithAAC(loggingService, samplesPath, aacDecoder);
        }

        static void PLayDABSuperFramesAndDecodeWithAAC(ILoggingService loggingService, string samplesPath, IAACDecoder aacDecoder)
        {
            var audioPlayer = new VLCSoundAudioPlayer();

            var audioDescription = new AudioDataDescription
            {
                SampleRate = 48000,
                Channels = 2,
                BitsPerSample = 16
            };

            audioPlayer.Init(audioDescription, loggingService);
            audioPlayer.SetMaxBufferSize(8000);
            audioPlayer.Play();

            // Initialize AAC decoder for the expected parameters (48k, stereo, no SBR, no PS)
            if (!aacDecoder.Init(true, 1, 2, false))
            {
                Console.WriteLine("AAC decoder initialization failed");
                return;
            }

            var playTask = Task.Run(() =>
            {
                var files = Directory.GetFiles(samplesPath, "AU*.aac").OrderBy(f => f).ToArray();
                if (files.Length == 0)
                {
                    Console.WriteLine("No AU*.aac files found in " + samplesPath);
                    return;
                }

                foreach (var file in files)
                {
                    try
                    {
                        Console.WriteLine($"Processing {Path.GetFileName(file)}");
                        var aacData = File.ReadAllBytes(file);

                        var pcmData = aacDecoder.DecodeAAC(aacData);
                        if (pcmData == null || pcmData.Length == 0)
                        {
                            Console.WriteLine($"AAC decode returned empty for {Path.GetFileName(file)}");
                            continue;
                        }

                        audioPlayer.AddData(pcmData);
                        Thread.Sleep(20);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error decoding/playing {Path.GetFileName(file)}: {ex}");
                    }
                }

                // allow playback drain
                Thread.Sleep(2000);
                audioPlayer.Stop();
            });

            playTask.Wait();
            aacDecoder.Close();
        }

        private static int GetCoreSampleRate(int dacRate, int sbrFlag)
        {
            if (dacRate == 0 && sbrFlag == 1) return 16000;
            if (dacRate == 1 && sbrFlag == 1) return 24000;
            if (dacRate == 0 && sbrFlag == 0) return 32000;
            if (dacRate == 1 && sbrFlag == 0) return 48000;
            return 48000;
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
