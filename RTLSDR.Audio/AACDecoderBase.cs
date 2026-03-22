using System.Runtime.InteropServices;
using LoggerService;

namespace RTLSDR.Audio;

public abstract class AACDecoderBase : IAACDecoder
{
    protected readonly ILoggingService _loggingService;
    private ulong _samplerate;
    private ulong _channels;

    public AACDecoderBase(ILoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    public virtual bool Init(bool sbrUsed, int dacRate, int channels, bool psUsed)
    {
        try
            {
                _loggingService.Debug("Initializing faad2");

                InitConfig(0, 1);  // dontUpSampleImplicitSBR = 0, FAAD_FMT_16BIT

                var asc = ASCHeader.GetAsc(dacRate, sbrUsed, channels, psUsed);

                if (asc == null || asc?.Data == null || asc?.Lenght == null)
                {
                    _loggingService.Error(null, "Error preparing ASC header for faad2");
                    Close();
                    return false;
                }

                int result = DecInit2(asc?.Data ?? new byte[0], (uint)(asc?.Lenght ?? 0), out _samplerate, out _channels);

                if (result != 0)
                {
                    _loggingService.Error(null, "Error initializing faad2");
                    Close();
                    return false;
                }

                _loggingService.Debug($"faad2 initialized: samplerate: {_samplerate}, channels: {_channels}");

                return true;
            }
            catch (Exception ex)
            {
                _loggingService.Error(ex, "Error initializing faad2");
                return false;
            }
    }

    public virtual void Close()
    {
        throw new NotImplementedException();
    }

    public virtual int DecInit2(byte[] buffer, uint size, out ulong samplerate, out ulong channels)
    {
        throw new NotImplementedException();
    }

    public virtual IntPtr DecDecode(out AACDecFrameInfo hInfo, byte[] buffer, ulong buffer_size)
    {
        throw new NotImplementedException();
    }

    public virtual IntPtr DecGetCurrentConfiguration()
    {
        throw new NotImplementedException();
    }

    public virtual IntPtr DecSetConfiguration(IntPtr configPtr)
    {
        throw new NotImplementedException();
    }

    protected virtual bool InitConfig(byte dontUpSampleImplicitSBR, byte outputFormat)
    {
        // set general config
        var configPtr = DecGetCurrentConfiguration();

        var configPtrStr = Marshal.PtrToStructure(configPtr, typeof(AACDecConfiguration));
        if (configPtrStr is AACDecConfiguration config)
        {
            config.dontUpSampleImplicitSBR = dontUpSampleImplicitSBR;
            config.outputFormat = outputFormat; // FAAD_FMT_16BIT

            Marshal.StructureToPtr(config, configPtr, false);

            var setConfigRes = DecSetConfiguration(configPtr);
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
        return true;
    }

    public virtual byte[]? DecodeAAC(byte[] aacData)
    {
         try
        {
            byte[]? pcmData = null;

            AACDecFrameInfo frameInfo;

            var resultPtr = DecDecode(out frameInfo, aacData, (ulong)aacData.Length);

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

}
