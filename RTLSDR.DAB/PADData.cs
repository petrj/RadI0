namespace RTLSDR.DAB;

public class PADData
{
    public bool Present { get; set; } = false;
    public byte[] XPAD { get; set; }
    public byte[] FPAD { get; set; }
}
