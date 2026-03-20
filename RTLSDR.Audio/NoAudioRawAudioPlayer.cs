
using RTLSDR.Common;
using LoggerService;

namespace RTLSDR.Audio
{
    /// <summary>
    /// A dummy audio player that does nothing, used when no audio output is desired.
    /// </summary>
    public class NoAudioRawAudioPlayer : IRawAudioPlayer
    {
        /// <summary>
        /// Initializes the player (no-op).
        /// </summary>
        /// <param name="audioDescription">The audio data description.</param>
        /// <param name="loggingService">The logging service.</param>
        /// <param name="mediaOptions">Optional media options.</param>
        public void Init(AudioDataDescription audioDescription, ILoggingService loggingService, string[]? mediaOptions = null)
        {
        }

        /// <summary>
        /// Gets a default audio data description.
        /// </summary>
        /// <returns>A new audio data description instance.</returns>
        public AudioDataDescription? GetAudioDataDescription()
        {
            return new AudioDataDescription();
        }

        /// <summary>
        /// Gets a value indicating whether PCM processing is complete (always true).
        /// </summary>
        public bool PCMProcessed
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Starts playback (no-op).
        /// </summary>
        public void Play()
        {
        }

        /// <summary>
        /// Adds data (no-op).
        /// </summary>
        /// <param name="data">The audio data.</param>
        public void AddData(byte[] data)
        {
        }

        /// <summary>
        /// Stops playback (no-op).
        /// </summary>
        public void Stop()
        {
        }

        /// <summary>
        /// Clears the buffer (no-op).
        /// </summary>
        public void ClearBuffer()
        {
        }

        /// <summary>
        /// Sets the maximum buffer size (no-op).
        /// </summary>
        /// <param name="sizeInBytes">The maximum buffer size.</param>
        public void SetMaxBufferSize(int sizeInBytes)
        {

        }
    }
}