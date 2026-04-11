namespace RTLSDR.Common;

public class StatValue
{
    public string? Title { get; set; } 
    public object? Value { get; set; }
    public string? Unit { get; set; }

    public StatValue(string title, object value, string? unit = null)
    {
        Title = title;
        Value = value;
        Unit = unit;
    }

    public static StatValue CreateFromBitrate(string title, double bitRate)
    {
        return CreateFromValue(title, bitRate, "Mb/s", "Kb/s", "s");
    }

    public static StatValue CreateFromFrequency(string title, double freq)
    {
        return CreateFromValue(title, freq, "MHz", "KHz", "Hz");
    } 

    public static StatValue CreateFromValue(string title, double val, string unitM, string unitK, string unit)
    {
        if (val > 1E+06) 
        {
            return new StatValue(title, val / 1000000.0, unitM);
        } else
        if (val > 1E+3) 
        {
            return new StatValue(title, val / 1000.0, unitK);
        } 

        return new StatValue(title, val, unit);
    }   

}
