
using LoggerService;
using RTLSDR.Common;

namespace RTLSDR.Audio
{
    /// <summary>
    /// Interface for raw audio players that handle audio playback from byte data.
    /// </summary>
    public interface IRawAudioPlayer
    {
        /// <summary>
        /// Initializes the audio player with the specified audio description.
        /// </summary>
        /// <param name="audioDescription">The audio data description.</param>
        /// <param name="loggingService">The logging service.</param>
        /// <param name="mediaOptions">Optional media options.</param>
        void Init(AudioDataDescription audioDescription, ILoggingService loggingService, string[]? mediaOptions = null);

        /// <summary>
        /// Starts audio playback.
        /// </summary>
        void Play();

        /// <summary>
        /// Adds audio data to the playback buffer.
        /// </summary>
        /// <param name="data">The audio data bytes.</param>
        void AddData(byte[] data);

        /// <summary>
        /// Stops audio playback.
        /// </summary>
        void Stop();

        /// <summary>
        /// Clears the audio buffer.
        /// </summary>
        void ClearBuffer();

        /// <summary>
        /// Gets the current audio data description.
        /// </summary>
        /// <returns>The audio data description, or null if not set.</returns>
        AudioDataDescription? GetAudioDataDescription();

        /// <summary>
        /// Sets the maximum buffer size in bytes.
        /// </summary>
        /// <param name="sizeInBytes">The maximum buffer size.</param>
        void SetMaxBufferSize(int sizeInBytes);
    }
}

