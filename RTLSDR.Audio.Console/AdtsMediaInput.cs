using LibVLCSharp.Shared;
using System;
using System.IO;
using System.Threading;

public class AdtsMediaInput : MediaInput
{
    private readonly Stream _sourceStream;

    public AdtsMediaInput(Stream sourceStream)
    {
        _sourceStream = sourceStream;
    }

    public override int Read(IntPtr buffer, uint bufferSize)
    {
        byte[] temp = new byte[bufferSize];
        int read = _sourceStream.Read(temp, 0, temp.Length);

        if (read > 0)
            System.Runtime.InteropServices.Marshal.Copy(temp, 0, buffer, read);

        return read;
    }

    public override bool Seek(ulong offset)
    {
        return false;
    }

    public override void Close()
    {
        _sourceStream.Dispose();
    }

    public override bool Open(out ulong size)
    {
        size = 0; // unknown length (live stream)
        return true;
    }

}