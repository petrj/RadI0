using System;

namespace RTLSDR.DAB.MOT
{
    /// <summary>
    /// Reassembles MSC Data Groups from X-PAD data subfields for MOT.
    /// References: ETSI EN 300 401 §5.3.3 (MSC Data Group), ETSI TS 101 499 §4.3.
    /// </summary>
    public class MOTDecoder
    {
        private const int MAX_DG_SIZE = 16384; // 2^14 bytes maximum MSC data group size
        private const int CRC_LEN = 2;

        private readonly byte[] _dgRaw = new byte[MAX_DG_SIZE];
        private int _dgSize = 0;
        private int _motLen = 0;
        private readonly DABCRC _crc16 = new DABCRC(true, true, 0x1021);

        /// <summary>
        /// Resets the data group reassembler state.
        /// </summary>
        public void Reset()
        {
            _dgSize = 0;
            _motLen = 0;
        }

        /// <summary>
        /// Sets the announced length of the data group (typically from a DGLI).
        /// </summary>
        public void SetLen(int motLen)
        {
            _motLen = motLen;
        }

        /// <summary>
        /// Processes a data subfield chunk for an MOT data group.
        /// </summary>
        public bool ProcessDataSubfield(bool start, byte[] xpad, int offset, int len)
        {
            if (start)
            {
                _dgSize = 0;
            }
            else if (_dgSize == 0)
            {
                return false;
            }

            int copyLen = Math.Min(len, MAX_DG_SIZE - _dgSize);
            if (copyLen <= 0)
                return false;

            Buffer.BlockCopy(xpad, offset, _dgRaw, _dgSize, copyLen);
            _dgSize += copyLen;

            // If length was not announced via DGLI, attempt to infer it from DG + segmentation headers
            if (_motLen == 0 && _dgSize >= 6)
            {
                bool extFlag = (_dgRaw[0] & 0x80) != 0;
                int sessOffset = 2 + (extFlag ? 2 : 0);
                if (_dgSize >= sessOffset + 3)
                {
                    int lenInd = _dgRaw[sessOffset + 2] & 0x0F;
                    int segOffset = sessOffset + 3 + lenInd;
                    if (_dgSize >= segOffset + 2)
                    {
                        int segSize = ((_dgRaw[segOffset] & 0x1F) << 8) | _dgRaw[segOffset + 1];
                        _motLen = segOffset + 2 + segSize + CRC_LEN;
                    }
                }
            }

            if (_motLen < CRC_LEN || _dgSize < _motLen)
                return false;

            return CheckCRCAndComplete();
        }

        private bool CheckCRCAndComplete()
        {
            int dataLen = _motLen - CRC_LEN;
            if (dataLen < 0 || _dgSize < _motLen)
            {
                Reset();
                return false;
            }

            uint crcCalced = _crc16.CalcCRC(_dgRaw, 0, dataLen);
            ushort crcStored = (ushort)((_dgRaw[dataLen] << 8) | _dgRaw[dataLen + 1]);

            if (crcStored != crcCalced)
            {
                Reset();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Retrieves the completed MSC Data Group bytes and resets the decoder.
        /// </summary>
        public byte[] GetMOTDataGroup()
        {
            var result = new byte[_motLen];
            Buffer.BlockCopy(_dgRaw, 0, result, 0, _motLen);
            Reset();
            return result;
        }
    }
}
