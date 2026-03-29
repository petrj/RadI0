namespace RTLSDR.Audio;

/// <summary>
/// The ASC header.
/// </summary>
public struct ASCHeader
{
    /// <summary>
    /// The data.
    /// </summary>
    public byte[]? Data;
    /// <summary>
    /// The lenght.
    /// </summary>
    public int? Lenght;

    public static ASCHeader? GetAsc(int dacRate, bool sbrUsed, int channels, bool psUsed)
    {
        var asc_len = 0;
        var asc = new byte[7];

        // 24/48/16/32 kHz
        int coreSrIndex;
        if (dacRate == 1)
        {
            coreSrIndex = sbrUsed ? 6 : 3;
        }
        else
        {
            coreSrIndex = sbrUsed ? 8 : 5;
        }
        var coreChConfig = channels;
        var extensionSrIndex = dacRate == 1 ? 3 : 5;    // 48/32 kHz

        asc[asc_len++] = Convert.ToByte(0b00010 << 3 | coreSrIndex >> 1);
        asc[asc_len++] = Convert.ToByte((coreSrIndex & 0x01) << 7 | coreChConfig << 3 | 0b100);

        if (sbrUsed)
        {
            // add SBR
            asc[asc_len++] = 0x56;
            asc[asc_len++] = 0xE5;
            asc[asc_len++] = Convert.ToByte(0x80 | (extensionSrIndex << 3));

            if (psUsed)
            {
                // add PS
                asc[asc_len - 1] |= 0x05;
                asc[asc_len++] = 0x48;
                asc[asc_len++] = 0x80;
            }
        }

        return new ASCHeader { Data = asc, Lenght = asc_len };
    }
}