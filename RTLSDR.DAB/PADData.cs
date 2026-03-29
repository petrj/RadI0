namespace RTLSDR.DAB;

/// <summary>
/// The pad data.
/// </summary>
public class PADData
{
    /// <summary>
    /// Gets or sets a value indicating whether present.
    /// </summary>
    public bool Present { get; set; } = false;
    /// <summary>
    /// Gets or sets the XPAD.
    /// </summary>
    public byte[]? XPAD { get; set; }
    /// <summary>
    /// Gets or sets the FPAD.
    /// </summary>
    public byte[]? FPAD { get; set; }
}
