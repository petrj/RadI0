namespace RTLSDR.FM;

/// <summary>
/// Event args raised when RDS data is decoded from an FM broadcast.
/// </summary>
public class RDSServiceFoundEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets the decoded RDS data.
    /// </summary>
    public RDSData? RDSData { get; set; }
}
