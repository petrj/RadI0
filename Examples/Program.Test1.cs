using RTLSDR.Common;
using LoggerService;
using RTLSDR.Audio;
using RTLSDR.DAB;
using System;

namespace RTLSDR.Examples
{
    internal partial class Program
    {
        static void PlayAudioWithLibVLC(ILoggingService loggingService, string samplesPath)
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
    }
}