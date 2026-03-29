namespace RTLSDR.FM;

/// <summary>
/// The fm service found event args.
/// </summary>
public class FMServiceFoundEventArgs: EventArgs
{
    /// <summary>
    /// Gets or sets the Frequency.
    /// </summary>
    public int Frequency { get;set; }
    /// <summary>
    /// Gets or sets the Percents.
    /// </summary>
    public double Percents { get;set; }
}