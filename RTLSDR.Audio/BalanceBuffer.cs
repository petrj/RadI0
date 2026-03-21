using System.Threading;
using RTLSDR.Common;
using System.Collections.Generic;
using System.Collections.Concurrent;
using LoggerService;
using System.Runtime.CompilerServices;
using Microsoft.VisualBasic;

namespace RTLSDR.Audio;

/// <summary>
/// A buffer that balances audio data input and output to maintain smooth playback.
/// </summary>
public class BalanceBuffer
{
    private readonly Thread _thread;
    private bool _running = false;

    private readonly Action<byte[]> _actionPlay;

    private readonly ConcurrentQueue<byte[]> _queue = new ConcurrentQueue<byte[]>();

    private const int MinThreadNoDataMSDelay = 25;
    private const int CycleMSDelay = 100; // 10x per sec

    private const int PreliminaryOutputBufferMS = 1000;

    private long _pcmBytesInput = 0;
    private long _pcmBytesOutput = 0;

    private readonly ILoggingService _loggingService;

    private AudioDataDescription _audioDescription;

    /// <summary>
    /// Gets or sets the buffer read time in milliseconds.
    /// </summary>
    public int BufferReadMS { get; set; } = 100;

    /// <summary>
    /// Initializes a new instance of the <see cref="BalanceBuffer"/> class.
    /// </summary>
    /// <param name="loggingService">The logging service.</param>
    /// <param name="actionPlay">The action to perform when playing audio data.</param>
    public BalanceBuffer(ILoggingService loggingService, Action<byte[]> actionPlay)
    {
        _loggingService = loggingService;

        _actionPlay = actionPlay;

        _loggingService.Info("Starting Balance buffer");

        _queue = new ConcurrentQueue<byte[]>();

        _audioDescription = new AudioDataDescription();

        _running = true;
        _thread = new Thread(ThreadLoop);
        _thread.Start();
    }

    /// <summary>
    /// Sets the audio data description for the buffer.
    /// </summary>
    /// <param name="audioDescription">The audio data description.</param>
    public void SetAudioDataDescription(AudioDataDescription audioDescription)
    {
        _audioDescription = audioDescription;

        _loggingService.Info($"Adio Balance      : Samplerate: {_audioDescription.SampleRate}");
        _loggingService.Info($"Adio Channels     : Channels: {_audioDescription.Channels}");
        _loggingService.Info($"Adio BitsPerSample: BitsPerSample: {_audioDescription.BitsPerSample}");
    }

    /// <summary>
    /// Adds audio data to the buffer.
    /// </summary>
    /// <param name="data">The audio data bytes.</param>
    public void AddData(byte[] data)
    {
        _queue.Enqueue(data);
    }

    /// <summary>
    /// Clears the audio buffer.
    /// </summary>
    public void ClearBuffer()
    {
        _queue.Clear();
    }

    /// <summary>
    /// Stops the balance buffer thread.
    /// </summary>
    public void Stop()
    {
        _running = false;
    }

    private void ThreadLoop()
    {
        _loggingService.Info("Starting Balance thread");

        DateTime cycleStartTime;
        DateTime lastNotifiTime = DateAndTime.Now;
        List<byte> _audioBuffer = new List<byte>();
        byte[] data = new byte[0];

        var loopStartTime = DateTime.Now;

        try
        {
            while (_running)
            {
                cycleStartTime = DateTime.Now; // start of next cycle

                var totalBytesRead = 0;

                // wait for data
                while ((DateTime.Now-cycleStartTime).TotalMilliseconds<CycleMSDelay)
                {
                    // fill buffer
                    var ok = _queue.TryDequeue(out data);

                    if (data != null && data.Length > 0)
                    {
                        totalBytesRead+=data.Length;
                        _audioBuffer.AddRange(data);
                        _pcmBytesInput += data.Length;
                    }

                    Thread.Sleep(MinThreadNoDataMSDelay);
                }

                var bytesPerSample = (_audioDescription.BitsPerSample/8)*_audioDescription.Channels;
                var bytesPerSec = _audioDescription.SampleRate*bytesPerSample;
                var secsFromLastCycle = (DateTime.Now - cycleStartTime).TotalSeconds;

                var cycleBytes = (Convert.ToInt32(secsFromLastCycle * bytesPerSec)/bytesPerSample)*(bytesPerSample)-bytesPerSample;

                var preliminaryOutputBufferBytes = bytesPerSec*PreliminaryOutputBufferMS/1000;

                if ((_pcmBytesInput-_pcmBytesOutput)<preliminaryOutputBufferBytes)
                {
                    cycleBytes = preliminaryOutputBufferBytes - cycleBytes;
                }

                // deque bytesFromLastCycle bytes:
                var bufferState = "OK";
                if ((cycleBytes > 0 ) && (_audioBuffer.Count>0))
                {
                    if (cycleBytes > _audioBuffer.Count)
                    {
                        cycleBytes = _audioBuffer.Count;
                    }

                    var thisCycleBytes = _audioBuffer.GetRange(0, Convert.ToInt32(cycleBytes));
                    _audioBuffer.RemoveRange(0, Convert.ToInt32(cycleBytes));

                    _actionPlay(thisCycleBytes.ToArray());
                    _pcmBytesOutput += cycleBytes;
                } else
                {
                  // no data in buffer! Notify slow CPU
                    bufferState = "Empty!";
                }

                if ((DateTime.Now-lastNotifiTime).TotalSeconds>2)
                {
                    _loggingService.Debug($" Audio buffer: {bufferState}  (processed {_pcmBytesOutput/1000} kB)");
                    lastNotifiTime = DateTime.Now;
                }
            }
        }
        catch (Exception ex)
        {
            _loggingService.Error(ex);
        }

        _loggingService.Info("Balance thread stopped");
    }

}
