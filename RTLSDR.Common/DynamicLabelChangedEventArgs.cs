using System;

namespace RTLSDR.Common
{
    /// <summary>
    /// Event arguments for when data has been demodulated, containing audio data and descriptions.
    /// </summary>
    public class DynamicLabelChangedEventArgs : EventArgs
    {
       public string Label { get;set;}
    }
}
