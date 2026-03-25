
using LoggerService;
using RTLSDR.Audio;
using RTLSDR.Common;
using System;
using System.IO;
using System.Linq;
using System.Threading;

namespace RTLSDR.Examples
{
    internal static partial class Program
    {
        private static void PlayAudio(string fname, IRawAudioPlayer audioPlayer)
        {
            ArgumentNullException.ThrowIfNull(audioPlayer, nameof(audioPlayer));
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
                    Thread.Sleep(20);
                }
                Console.WriteLine("Finished playing audio.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error playing audio: {ex}");
            }
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
                Thread.Sleep(2000);
                audioPlayer.Stop();
            });
            playTask.Wait();
            aacDecoder.Close();
        }
    }
}