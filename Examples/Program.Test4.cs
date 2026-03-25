using RTLSDR.Common;
using LoggerService;
using RTLSDR.Audio;
using RTLSDR.DAB;
using System;

namespace RTLSDR.Examples
{
    internal partial class Program
    {
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
                Thread.Sleep(200);
            });

            playTask.Wait();
        }
    }
}