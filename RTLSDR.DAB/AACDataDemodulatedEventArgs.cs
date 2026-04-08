using RTLSDR.Common;
namespace RTLSDR.DAB;

/// <summary>
/// The aac data demodulated event args.
/// </summary>
public class AACDataDemodulatedEventArgs : DataDemodulatedEventArgs
{
    /// <summary>
    /// Gets or sets the aac header.
    /// </summary>
    public AACSuperFrameHeader? AACHeader {get;set;}
    /// <summary>
    /// Gets or sets the adts header.
    /// </summary>
    public byte[]? ADTSHeader { get;set; }

  
}
