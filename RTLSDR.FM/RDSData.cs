namespace RTLSDR.FM;

/// <summary>
/// Holds parsed RDS (Radio Data System) information from FM broadcasts.
/// </summary>
public class RDSData
{
    /// <summary>
    /// Gets or sets the Program Identification code.
    /// </summary>
    public ushort PI { get; set; }

    /// <summary>
    /// Gets or sets the Program Service name (up to 8 characters).
    /// </summary>
    public string PS { get; set; } = "";

    /// <summary>
    /// Gets or sets the Radio Text (up to 64 characters).
    /// </summary>
    public string RadioText { get; set; } = "";

    /// <summary>
    /// Gets or sets the Program Type code (0-31).
    /// </summary>
    public int PTY { get; set; }

    /// <summary>
    /// Gets or sets the Traffic Program flag.
    /// </summary>
    public bool TP { get; set; }

    /// <summary>
    /// Gets or sets the Traffic Announcement flag.
    /// </summary>
    public bool TA { get; set; }

    /// <summary>
    /// Gets or sets whether the station broadcasts in stereo (Music/Speech flag).
    /// </summary>
    public bool IsStereo { get; set; }

    /// <summary>
    /// Gets or sets whether valid RDS data has been decoded.
    /// </summary>
    public bool Valid { get; set; }
}
