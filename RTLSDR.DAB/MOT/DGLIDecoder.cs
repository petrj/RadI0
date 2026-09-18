using System;

namespace RTLSDR.DAB.MOT
{
    /// <summary>
    /// Decodes Data Group Length Indicator (DGLI) from X-PAD data subfields.
    /// DGLI indicates the length of the immediate next MSC data group (e.g. MOT data group).
    /// References: ETSI EN 300 401 §7.4.5.1.
    /// </summary>
    public class DGLIDecoder
    {
        private const int DGLI_DATA_LEN = 2;
        private const int CRC_LEN = 2;
        private const int TOTAL_NEEDED = DGLI_DATA_LEN + CRC_LEN;

        private readonly byte[] _buffer = new byte[TOTAL_NEEDED];
        private int _currentSize = 0;
        private int _dgliLen = 0;
        private readonly DABCRC _crc16 = new DABCRC(true, true, 0x1021);

        /// <summary>
        /// Resets the decoder state.
        /// </summary>
        public void Reset()
        {
            _currentSize = 0;
            _dgliLen = 0;
        }

        /// <summary>
        /// Processes a DGLI data subfield from X-PAD.
        /// </summary>
        public bool ProcessDataSubfield(bool start, byte[] xpad, int offset, int len)
        {
            if (start)
            {
                _currentSize = 0;
            }
            else if (_currentSize == 0)
            {
                return false;
            }

            int copyLen = Math.Min(len, TOTAL_NEEDED - _currentSize);
            if (copyLen <= 0)
                return false;

            Buffer.BlockCopy(xpad, offset, _buffer, _currentSize, copyLen);
            _currentSize += copyLen;

            if (_currentSize < TOTAL_NEEDED)
                return false;

            // Check CRC-16-CCITT over the 2 data bytes
            uint crcCalced = _crc16.CalcCRC(_buffer, 0, DGLI_DATA_LEN);
            ushort crcStored = (ushort)((_buffer[DGLI_DATA_LEN] << 8) | _buffer[DGLI_DATA_LEN + 1]);

            if (crcCalced == crcStored)
            {
                _dgliLen = ((_buffer[0] & 0x3F) << 8) | _buffer[1];
            }

            _currentSize = 0;
            return crcCalced == crcStored;
        }

        /// <summary>
        /// Retrieves the decoded length indicator and clears it.
        /// The length is only valid for the immediate next data group.
        /// </summary>
        public int GetDGLILen()
        {
            int result = _dgliLen;
            _dgliLen = 0;
            return result;
        }
    }
}
