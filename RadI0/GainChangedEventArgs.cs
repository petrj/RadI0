namespace RadI0;

/// <summary>
/// The gain changed event args.
/// </summary>
public class GainChangedEventArgs : EventArgs
{
    /// <summary>
    /// Gets or sets a value indicating whether bool.
    /// </summary>
    public bool HWGain {get;set;}
    /// <summary>
    /// Gets or sets a value indicating whether bool.
    /// </summary>
    public bool SWGain {get;set;}
    /// <summary>
    /// Gets or sets the ManualGainValue.
    /// </summary>
    public int ManualGainValue {get;set;}
}
