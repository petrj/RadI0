using System;
using System.Collections.Generic;

namespace RTLSDR.DAB.MOT
{
    /// <summary>
    /// Reassembles segments of an MOT entity (MOT Header or MOT Body).
    /// Reference: ETSI EN 301 234 §5.
    /// </summary>
    public class MOTEntity
    {
        private readonly Dictionary<int, byte[]> _segs = new Dictionary<int, byte[]>();
        private int _lastSegNumber = -1;
        private int _totalSize = 0;

        /// <summary>
        /// Total size of all collected segments so far.
        /// </summary>
        public int TotalSize => _totalSize;

        /// <summary>
        /// The segment number of the last segment, or -1 if not yet known.
        /// </summary>
        public int LastSegNumber => _lastSegNumber;

        /// <summary>
        /// Adds a segment to this entity.
        /// </summary>
        public void AddSeg(int segNumber, bool lastSeg, byte[] data, int offset, int length)
        {
            if (lastSeg)
            {
                _lastSegNumber = segNumber;
            }

            if (_segs.ContainsKey(segNumber))
                return;

            var seg = new byte[length];
            Buffer.BlockCopy(data, offset, seg, 0, length);
            _segs[segNumber] = seg;
            _totalSize += length;
        }

        /// <summary>
        /// Checks if all segments from 0 to lastSegNumber have been received.
        /// </summary>
        public bool IsFinished()
        {
            if (_lastSegNumber == -1)
                return false;

            for (int i = 0; i <= _lastSegNumber; i++)
            {
                if (!_segs.ContainsKey(i))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Concatenates and returns all segments in sequential order.
        /// </summary>
        public byte[] GetData()
        {
            var result = new byte[_totalSize];
            int offset = 0;
            for (int i = 0; i <= _lastSegNumber; i++)
            {
                if (_segs.TryGetValue(i, out var seg))
                {
                    Buffer.BlockCopy(seg, 0, result, offset, seg.Length);
                    offset += seg.Length;
                }
            }
            return result;
        }

        /// <summary>
        /// Resets the entity state for new segment collection.
        /// </summary>
        public void Reset()
        {
            _segs.Clear();
            _lastSegNumber = -1;
            _totalSize = 0;
        }
    }
}
