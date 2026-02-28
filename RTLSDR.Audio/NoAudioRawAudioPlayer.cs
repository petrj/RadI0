
using RTLSDR.Common;
using LoggerService;

namespace RTLSDR.Audio
{
    public class NoAudioRawAudioPlayer : IRawAudioPlayer
    {
        public void Init(AudioDataDescription audioDescription, ILoggingService loggingService, string[] mediaOptions = null)
        {
        }

        public AudioDataDescription? GetAudioDataDescription()
        {
            return new AudioDataDescription();
        }

        public bool PCMProcessed
        {
            get
            {
                return true;
            }
        }

        public void Play()
        {
        }

        public void AddData(byte[] data)
        {
        }

        public void Stop()
        {
        }

        public void ClearBuffer()
        {
        }
    }
}