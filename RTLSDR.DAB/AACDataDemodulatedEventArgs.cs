using RTLSDR.Common;
namespace RTLSDR.DAB;

public class AACDataDemodulatedEventArgs : DataDemodulatedEventArgs
{
    public AACSuperFrameHeader? AACHeader {get;set;}
    public byte[]? ADTSHeader { get;set; }
    public string? DynamicLabel { get;set; }
}
