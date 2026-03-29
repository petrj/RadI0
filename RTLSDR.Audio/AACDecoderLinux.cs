using LoggerService;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR.Audio
{
    /// <summary>
    /// The AAC decoder linux.
    /// </summary>
    public class AACDecoderLinux : AACDecoderBase
    {
        /// <summary>
        /// The lib path constant.
        /// </summary>
        public const string libPath = "libfaad.so.2";

        [DllImport(libPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr NeAACDecOpen();

        [DllImport(libPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern void NeAACDecClose(IntPtr hDecoder);

        [DllImport(libPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr NeAACDecGetCurrentConfiguration(IntPtr hDecoder);

        [DllImport(libPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern int NeAACDecSetConfiguration(IntPtr hDecoder, IntPtr config);

        [DllImport(libPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern int NeAACDecInit(IntPtr hDecoder, byte[] buffer, uint buffer_size, out uint samplerate, out uint channels);

        [DllImport(libPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern int NeAACDecInit2(IntPtr hDecoder, byte[] buffer, uint size, out ulong samplerate, out ulong channels);

        [DllImport(libPath, CallingConvention = CallingConvention.Cdecl)]
        private static extern IntPtr NeAACDecDecode(IntPtr hpDecoder, out AACDecFrameInfoLinux hInfo, byte[] buffer, ulong buffer_size);

        private IntPtr _hDecoder = IntPtr.Zero;

        public AACDecoderLinux(ILoggingService loggingService) : base(loggingService)
        {
        }

        /// <summary>
        /// Init
        /// </summary>
        /// <param name="dacRate">0 .. 32khz, 1 .. 48khz</param>
        /// <param name="sbr">SBR used</param>
        /// <param name="channels">1 .. mono, 2 .. stereo</param>
        /// <param name="ps">PS used</param>
        public override bool Init(bool sbrUsed, int dacRate, int channels, bool psUsed)
        {
            try
            {
                _loggingService.Debug("Initializing faad2");

                _hDecoder = NeAACDecOpen();
                if (_hDecoder == IntPtr.Zero)
                {
                    _loggingService.Error(null, "Error initializing faad2");
                    return false;
                }

                return base.Init(sbrUsed, dacRate, channels, psUsed);
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error initializing faad2");
                return false;
            }
        }

        public override void Close()
        {
            NeAACDecClose(_hDecoder);
        }

        public override int DecInit2(byte[] buffer, uint size, out ulong samplerate, out ulong channels)
        {
            return NeAACDecInit2(_hDecoder, buffer, size, out samplerate, out channels);
        }

        public override IntPtr DecDecode(out AACDecFrameInfo hInfo, byte[] buffer, ulong buffer_size)
        {
            AACDecFrameInfoLinux hInfoLinux;

            var ptr = NeAACDecDecode(_hDecoder, out hInfoLinux, buffer, buffer_size);
            hInfo = AACDecFrameInfo.CreateFromLinuxFrameInfo(hInfoLinux);
            return ptr;
        }

        public override IntPtr DecGetCurrentConfiguration()
        {
            return NeAACDecGetCurrentConfiguration(_hDecoder);
        }

        public override IntPtr DecSetConfiguration(IntPtr configPtr)
        {
            return NeAACDecSetConfiguration(_hDecoder, configPtr);
        }
    }
}
