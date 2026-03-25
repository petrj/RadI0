using LoggerService;
using RTLSDR.Audio;
using RTLSDR.Common;
using System;

namespace RTLSDR.Examples
{
    internal partial class Program
    {
        static void PlayWAVEAudioWithLibVLC(ILoggingService loggingService, string samplesPath)
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
    }
}