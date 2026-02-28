
using LoggerService;
using RTLSDR.Common;

namespace RTLSDR.Audio
{
    public interface IRawAudioPlayer
    {
        void Init(AudioDataDescription audioDescription, ILoggingService loggingService, string[] mediaOptions = null);

        void Play();

        void AddData(byte[] data);

        void Stop();

        void ClearBuffer();

        AudioDataDescription? GetAudioDataDescription();
    }
}

