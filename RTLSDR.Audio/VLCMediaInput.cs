using System;
using LibVLCSharp.Shared;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;

namespace RTLSDR.Audio
{
    /// <summary>
    /// A media input class for VLC that provides audio data from a buffer.
    /// </summary>
    public class VLCMediaInput : MediaInput
    {
        /// <summary>
        /// Gets or sets the maximum data request size in bytes.
        /// </summary>
        public uint MaxDataRequestSize { get;set; } = 96000*16*2/8; // 1 s of stereo audio

        private readonly BlockingCollection<byte[]> _buffer = new BlockingCollection<byte[]>();

        private bool _stopped = false;

        /// <summary>
        /// Opens the media input.
        /// </summary>
        /// <param name="size">The result size (always 0).</param>
        /// <returns>Always true.</returns>
        public override bool Open(out ulong size)
        {
            _stopped = false;
            size = 0;
            return true;
        }

        /// <summary>
        /// Reads data from the buffer into the provided unmanaged buffer.
        /// </summary>
        /// <param name="buf">The unmanaged buffer to read into.</param>
        /// <param name="len">The length of data to read.</param>
        /// <returns>The number of bytes read.</returns>
        public override int Read(nint buf, uint len)
        {
            try
            {
                if (len > MaxDataRequestSize)
                    len = MaxDataRequestSize;

                int totalBytes = 0;
                Span<byte> tempSpan = stackalloc byte[(int)Math.Min(len, 65536)]; // small temp buffer

                while (totalBytes < len)
                {
                    if (_buffer.TryTake(out var chunk, 20))
                    {
                        int toCopy = Math.Min(chunk.Length, (int)(len - totalBytes));

                        // Copy safely from chunk into unmanaged buffer
                        Marshal.Copy(chunk, 0, buf + totalBytes, toCopy);

                        totalBytes += toCopy;

                        if (totalBytes >= len)
                            break;
                    }
                    else
                    {
                        if (_stopped)
                            break; // stop if signaled
                        Thread.Sleep(10);
                    }
                }

                return totalBytes > 0 ? totalBytes : 100; // never return 0
            }
            catch (Exception ex)
            {
                Console.WriteLine($"VLCMediaInput Read exception: {ex}");
                try
                {
                    var dummy = new byte[100];
                    Marshal.Copy(dummy, 0, buf, dummy.Length);
                    return dummy.Length;
                }
                catch { return 100; } // fallback
            }
        }

        /// <summary>
        /// Closes the media input.
        /// </summary>
        public override void Close()
        {
            _stopped = true;
            //_buffer.CompleteAdding();
        }

        /// <summary>
        /// Pushes audio data into the buffer.
        /// </summary>
        /// <param name="data">The audio data bytes.</param>
        public void PushData(byte[] data)
        {
            //Console.WriteLine($"Feeding data: {data.Length/1000} KB");

            _buffer.Add(data);
        }

        /// <summary>
        /// Clears the data buffer.
        /// </summary>
        public void ClearBuffer()
        {
            while (_buffer.TryTake(out var chunk))
            {
            }
        }

        /// <summary>
        /// Seeks to the specified offset (not supported).
        /// </summary>
        /// <param name="offset">The offset to seek to.</param>
        /// <returns>Always false.</returns>
        public override bool Seek(ulong offset)
        {
            return false;
        }
    }
}