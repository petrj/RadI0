using System;
using System.Runtime.InteropServices;
using System.Xml.Linq;
using LoggerService;

namespace RTLSDR.Audio
{
    public class AACDecoderWindows : IAACDecoder
    {
        public const string libPath = "libfaad2_dll.dll";

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
        private static extern IntPtr NeAACDecDecode(IntPtr hpDecoder, out AACDecFrameInfoWindows hInfo, byte[] buffer, ulong buffer_size);

        private IntPtr _hDecoder = IntPtr.Zero;
        private ulong _samplerate;
        private ulong _channels;
        private readonly ILoggingService _loggingService;

        public AACDecoderWindows(ILoggingService loggingService)
        {
            _loggingService = loggingService;
        }

        /// <summary>
        /// Init
        /// </summary>
        /// <param name="dacRate">0 .. 32khz, 1 .. 48khz</param>
        /// <param name="sbr">SBR used</param>
        /// <param name="channels">1 .. mono, 2 .. stereo</param>
        /// <param name="ps">PS used</param>
        public bool Init(bool sbrUsed, int dacRate, int channels, bool psUsed)
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

                // set general config
                var configPtr = NeAACDecGetCurrentConfiguration(_hDecoder);

                var configPtrStr = Marshal.PtrToStructure(configPtr, typeof(AACDecConfiguration));
                if (configPtrStr is AACDecConfiguration config)
                {
                    config.dontUpSampleImplicitSBR = 0;
                    config.outputFormat = 1; // FAAD_FMT_16BIT

                    Marshal.StructureToPtr(config, configPtr, false);

                    var setConfigRes = NeAACDecSetConfiguration(_hDecoder, configPtr);
                    if (setConfigRes != 1)
                    {
                        _loggingService.Error(null, "Error initializing faad2");
                        return false;
                    }
                }
                else
                {
                    _loggingService.Error(null, "Error initializing faad2: failed to get current configuration");
                    return false;
                }

                var asc_len = 0;
                var asc = new byte[7];

                // 24/48/16/32 kHz
                int coreSrIndex;
                if (dacRate == 1)
                {
                    coreSrIndex = sbrUsed ? 6 : 3;
                }
                else
                {
                    coreSrIndex = sbrUsed ? 8 : 5;
                }
                var coreChConfig = channels;
                var extensionSrIndex = dacRate == 1 ? 3 : 5;    // 48/32 kHz

                asc[asc_len++] = Convert.ToByte(0b00010 << 3 | coreSrIndex >> 1);
                asc[asc_len++] = Convert.ToByte((coreSrIndex & 0x01) << 7 | coreChConfig << 3 | 0b100);

                if (sbrUsed)
                {
                    // add SBR
                    asc[asc_len++] = 0x56;
                    asc[asc_len++] = 0xE5;
                    asc[asc_len++] = Convert.ToByte(0x80 | (extensionSrIndex << 3));

                    if (psUsed)
                    {
                        // add PS
                        asc[asc_len - 1] |= 0x05;
                        asc[asc_len++] = 0x48;
                        asc[asc_len++] = 0x80;
                    }
                }

                int result = NeAACDecInit2(_hDecoder, asc, (uint)asc_len, out _samplerate, out _channels);

                if (result != 0)
                {
                    _loggingService.Error(null, "Error initializing faad2");
                    NeAACDecClose(_hDecoder);
                    return false;
                }

                _loggingService.Debug($"faad2 initialized: samplerate: {_samplerate}, channels: {_channels}");

                return true;
            } catch (Exception ex)
            {
                _loggingService.Error(ex, "Error initializing faad2");
                return false;
            }
        }

        public byte[]? DecodeAAC(byte[] aacData)
        {
            try
            {
                byte[]? pcmData = null;
                AACDecFrameInfoWindows frameInfo;

                var resultPtr = NeAACDecDecode(_hDecoder, out frameInfo, aacData, (ulong)aacData.Length);

                if (Convert.ToInt32(frameInfo.bytesconsumed) != aacData.Length)
                {
                    _loggingService.Info($"DecodeAAC failed : only {frameInfo.bytesconsumed}/{aacData.Length} bytes processed");
                    return null; // consumed only part
                }

                if (frameInfo.samples > 0)
                {
                    pcmData = new byte[frameInfo.samples * 2];
                    Marshal.Copy(resultPtr, pcmData, 0, pcmData.Length);
                }

                return pcmData;
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "DecodeAAC failed");
                return null;
            }
        }

        public void Close()
        {
            NeAACDecClose(_hDecoder);
        }
    }

}
