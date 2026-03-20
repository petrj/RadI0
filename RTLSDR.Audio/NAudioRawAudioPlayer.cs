using System;
using System.Net.Sockets;
using System.Net;
using NAudio.Wave;
using RTLSDR.Common;
using LoggerService;
using System.Collections.Concurrent;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace RTLSDR.Audio
{
    /// <summary>
    /// An audio player that uses NAudio for raw audio playback on Windows systems.
    /// </summary>
    public class NAudioRawAudioPlayer : IRawAudioPlayer
    {
        private readonly ILoggingService _loggingService;

        private WaveOutEvent? _outputDevice;
        private BufferedWaveProvider? _bufferedWaveProvider;


        private AudioDataDescription? _audioDescription;

        private BalanceBuffer? _ballanceBuffer = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="NAudioRawAudioPlayer"/> class.
        /// </summary>
        /// <param name="loggingService">The logging service.</param>
        public NAudioRawAudioPlayer(ILoggingService loggingService)
        {
            _loggingService = loggingService;
            if (_loggingService == null)
            {
                _loggingService = new DummyLoggingService();
            }

            _loggingService.Info("Initializing NAudio raw audio player");

            _audioDescription = new AudioDataDescription()
            {
                BitsPerSample = 16,
                Channels = 2,
                SampleRate = 48000
            };
        }

        /// <summary>
        /// Gets the current audio data description.
        /// </summary>
        /// <returns>The audio data description, or null if not set.</returns>
        public AudioDataDescription? GetAudioDataDescription()
        {
            return _audioDescription;
        }

        /// <summary>
        /// Gets a value indicating whether the PCM data has been fully processed (always false for this player).
        /// </summary>
        public bool PCMProcessed
        {
            get
            {
                return false; // no Balance buffer
            }
        }

        /// <summary>
        /// Initializes the NAudio player with the specified audio description.
        /// </summary>
        /// <param name="audioDescription">The audio data description.</param>
        /// <param name="loggingService">The logging service.</param>
        /// <param name="mediaOptions">Optional media options (not used).</param>
        public void Init(AudioDataDescription audioDescription, ILoggingService loggingService, string[]? mediaOptions = null)
        {
            _audioDescription = audioDescription;
            _outputDevice = new WaveOutEvent();
            var waveFormat = new WaveFormat(audioDescription.SampleRate, audioDescription.BitsPerSample, audioDescription.Channels);
            _bufferedWaveProvider = new BufferedWaveProvider(waveFormat);
            //_bufferedWaveProvider.BufferDuration = new TimeSpan(0,0,10);
            //_bufferedWaveProvider.BufferLength = 10 * (audioDescription.SampleRate * audioDescription.Channels * audioDescription.BitsPerSample / 8);

            _outputDevice.Init(_bufferedWaveProvider);

            _ballanceBuffer = new BalanceBuffer(_loggingService, (data) =>
            {
                if (data == null)
                {
                    return;
                }

                _bufferedWaveProvider.AddSamples(data, 0, data.Length);
            });

            _ballanceBuffer.SetAudioDataDescription(audioDescription);
        }

        /// <summary>
        /// Starts audio playback.
        /// </summary>
        public void Play()
        {
            if (_outputDevice != null)
            {
                _outputDevice.Play();
            }
        }

        /// <summary>
        /// Adds audio data to the playback buffer.
        /// </summary>
        /// <param name="data">The audio data bytes.</param>
        public void AddData(byte[] data)
        {
            _ballanceBuffer.AddData(data);
        }

        /// <summary>
        /// Stops audio playback and clears buffers.
        /// </summary>
        public void Stop()
        {
            _outputDevice?.Stop();
            _bufferedWaveProvider?.ClearBuffer();
            _ballanceBuffer?.Stop();
        }

        /// <summary>
        /// Clears the audio buffer.
        /// </summary>
        public void ClearBuffer()
        {
            _ballanceBuffer.ClearBuffer();
        }

        /// <summary>
        /// Sets the maximum buffer size (not implemented).
        /// </summary>
        /// <param name="sizeInBytes">The maximum buffer size in bytes.</param>
        public void SetMaxBufferSize(int sizeInBytes)
        {

        }
    }
}