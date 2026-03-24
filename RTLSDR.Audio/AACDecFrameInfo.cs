namespace RTLSDR.Audio;

public class AACDecFrameInfo
{
    public ulong bytesconsumed { get; set; }
    public ulong samples { get; set; }
    public char channels { get; set; }
    public char error { get; set; }
    public long samplerate { get; set; }
    public char sbr { get; set; }
    public char object_type { get; set; }
    public char header_type { get; set; }
    public char num_front_channels { get; set; }
    public char num_side_channels { get; set; }
    public char num_back_channels { get; set; }
    public char num_lfe_channels { get; set; }
    public char[] channel_position  { get; set; } = new char[1024];
    public char ps;

    public static AACDecFrameInfo CreateFromLinuxFrameInfo(AACDecFrameInfoLinux hInfoLinux)
    {
        return new AACDecFrameInfo
        {
            bytesconsumed = hInfoLinux.bytesconsumed,
            samples = hInfoLinux.samples,
            channels = hInfoLinux.channels,
            error = hInfoLinux.error,
            samplerate = hInfoLinux.samplerate,
            sbr = hInfoLinux.sbr,
            object_type = hInfoLinux.object_type,
            header_type = hInfoLinux.header_type,
            num_front_channels = hInfoLinux.num_front_channels,
            num_side_channels = hInfoLinux.num_side_channels,
            num_back_channels = hInfoLinux.num_back_channels,
            num_lfe_channels = hInfoLinux.num_lfe_channels,
            channel_position = hInfoLinux.channel_position,
            ps = hInfoLinux.ps
        };
    }

    public static AACDecFrameInfo CreateFromWindowsFrameInfo(AACDecFrameInfoWindows hInfoWindows)
    {
        return new AACDecFrameInfo
        {
            bytesconsumed = hInfoWindows.bytesconsumed,
            samples = hInfoWindows.samples,
            channels = hInfoWindows.channels,
            error = hInfoWindows.error,
            samplerate = hInfoWindows.samplerate,
            sbr = hInfoWindows.sbr,
            object_type = hInfoWindows.object_type,
            header_type = hInfoWindows.header_type,
            num_front_channels = hInfoWindows.num_front_channels,
            num_side_channels = hInfoWindows.num_side_channels,
            num_back_channels = hInfoWindows.num_back_channels,
            num_lfe_channels = hInfoWindows.num_lfe_channels,
            channel_position = hInfoWindows.channel_position,
            ps = hInfoWindows.ps
        };
    }
}
