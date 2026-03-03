using System;
using System.IO;

namespace RTLSDR.DAB
{

public class ADTSHeader
{
    public static byte[] CreateAdtsHeader(int profile, int sampleRate, int channels,int aacLength)
    {
        var samplingFrequencyIndex = GetSamplingFrequencyIndex(sampleRate);

        int frameLength = aacLength + 7;
        byte[] header = new byte[7];

        header[0] = 0xFF;
        header[1] = 0xF1; // 1111 0001 (sync + MPEG-4 + no CRC)

        header[2] = (byte)((profile << 6) |
                           (samplingFrequencyIndex << 2) |
                           (channels >> 2));

        header[3] = (byte)(((channels & 3) << 6) |
                           (frameLength >> 11));

        header[4] = (byte)((frameLength & 0x7FF) >> 3);

        header[5] = (byte)(((frameLength & 7) << 5) | 0x1F);

        header[6] = 0xFC;

        return header;
    }

    private static int GetSamplingFrequencyIndex(int sampleRate)
    {
        return sampleRate switch
        {
            96000 => 0,
            88200 => 1,
            64000 => 2,
            48000 => 3,
            44100 => 4,
            32000 => 5,
            24000 => 6,
            22050 => 7,
            16000 => 8,
            12000 => 9,
            11025 => 10,
            8000  => 11,
            7350  => 12,
            _ => throw new ArgumentException("Unsupported sample rate")
        };
    }
}

}