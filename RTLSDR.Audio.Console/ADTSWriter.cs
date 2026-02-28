using System;
using System.IO;

public class ADTSWriter
{
    private readonly int _profile;              // 1 = AAC LC
    private readonly int _samplingFrequencyIndex;
    private readonly int _channelConfig;

    public ADTSWriter(int profile, int sampleRate, int channels)
    {
        _profile = profile - 1; // ADTS stores profile - 1
        _samplingFrequencyIndex = GetSamplingFrequencyIndex(sampleRate);
        _channelConfig = channels;
    }

    public void WriteFrame(Stream output, byte[] aacFrame)
    {
        byte[] adtsHeader = CreateAdtsHeader(aacFrame.Length);
        output.Write(adtsHeader, 0, adtsHeader.Length);
        output.Write(aacFrame, 0, aacFrame.Length);
    }

    private byte[] CreateAdtsHeader(int aacLength)
    {
        int frameLength = aacLength + 7;
        byte[] header = new byte[7];

        header[0] = 0xFF;
        header[1] = 0xF1; // 1111 0001 (sync + MPEG-4 + no CRC)

        header[2] = (byte)((_profile << 6) |
                           (_samplingFrequencyIndex << 2) |
                           (_channelConfig >> 2));

        header[3] = (byte)(((_channelConfig & 3) << 6) |
                           (frameLength >> 11));

        header[4] = (byte)((frameLength & 0x7FF) >> 3);

        header[5] = (byte)(((frameLength & 7) << 5) | 0x1F);

        header[6] = 0xFC;

        return header;
    }

    private int GetSamplingFrequencyIndex(int sampleRate)
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