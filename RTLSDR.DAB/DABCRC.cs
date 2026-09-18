using System;
namespace RTLSDR.DAB
{
    /// <summary>
    /// The DABCRC.
    /// </summary>
    public class DABCRC
    {
        private uint[] _crc_lut = Array.Empty<uint>();

        private readonly bool _initialInvert = false;
        private readonly bool _finalInvert = false;
        private readonly uint _gen_polynom;

        public DABCRC(bool initialInvert, bool finalInvert, uint genPolynom)
        {
            _initialInvert = initialInvert;
            _finalInvert = finalInvert;
            _gen_polynom = genPolynom;

            FillLUT();
        }

        /// <summary>
        /// Calculates the crc for a buffer slice.
        /// </summary>
        public uint CalcCRC(byte[] data, int offset, int count)
        {
            long crc = _initialInvert ? 0xFFFF : 0x0000;

            for (var i = 0; i < count; i++)
            {
                crc = ((crc << 8) & 0xFFFF) ^ _crc_lut[(crc >> 8) ^ data[offset + i]];
            }

            if (_finalInvert)
            {
                crc = ~crc; // binary invert
            }

            return Convert.ToUInt32(crc & 0xFFFF);
        }

        /// <summary>
        /// Calculates the crc
        /// </summary>
        /// <returns>The crc.</returns>
        /// <param name="data">Data.</param>
        public uint CalcCRC(byte[] data)
        {
            return CalcCRC(data, 0, data.Length);
        }

        private void FillLUT()
        {
            _crc_lut = new uint[256];

            for (int value = 0; value < 256; value++)
            {
                uint crc = Convert.ToUInt32((value << 8) & 0xFFFF); // 16 bit number

                for (int i = 0; i < 8; i++)
                {
                    if ((crc & 0x8000) != 0)
                    {
                        crc = ((crc << 1) & 0xFFFF) ^ _gen_polynom;
                    }
                    else
                    {
                        crc = (crc << 1) & 0xFFFF;
                    }
                }

                _crc_lut[value] = crc;
            }
        }
    }
}
