namespace RTLSDR.Audio;

/// <summary>
/// The AAC dec frame info.
/// </summary>
public class AACDecFrameInfo
{
    /// <summary>
    /// Gets or sets the bytesconsumed.
    /// </summary>
    public ulong bytesconsumed { get; set; }
    /// <summary>
    /// Gets or sets the samples.
    /// </summary>
    public ulong samples { get; set; }
    /// <summary>
    /// Gets or sets the channels.
    /// </summary>
    public char channels { get; set; }
    /// <summary>
    /// Gets or sets the error.
    /// </summary>
    public char error { get; set; }
    /// <summary>
    /// Gets or sets the samplerate.
    /// </summary>
    public long samplerate { get; set; }
    /// <summary>
    /// Gets or sets the sbr.
    /// </summary>
    public char sbr { get; set; }
    /// <summary>
    /// Gets or sets the object type.
    /// </summary>
    public char object_type { get; set; }
    /// <summary>
    /// Gets or sets the header type.
    /// </summary>
    public char header_type { get; set; }
    /// <summary>
    /// Gets or sets the num front channels.
    /// </summary>
    public char num_front_channels { get; set; }
    /// <summary>
    /// Gets or sets the num side channels.
    /// </summary>
    public char num_side_channels { get; set; }
    /// <summary>
    /// Gets or sets the num back channels.
    /// </summary>
    public char num_back_channels { get; set; }
    /// <summary>
    /// Gets or sets the num lfe channels.
    /// </summary>
    public char num_lfe_channels { get; set; }
    /// <summary>
    /// Gets or sets the channel position.
    /// </summary>
    public char[] channel_position  { get; set; } = new char[1024];
    /// <summary>
    /// The ps.
    /// </summary>
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
