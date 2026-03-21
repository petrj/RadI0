using System;
using System.Collections.Generic;
using System.Text;

namespace RTLSDR.Common
{
    /// <summary>
    /// A utility class for writing bits to a string buffer and converting them to bytes.
    /// </summary>
    public class StringBitWriter
    {
        private StringBuilder? _buffer = null;

        /// <summary>
        /// Initializes a new instance of the <see cref="StringBitWriter"/> class.
        /// </summary>
        public StringBitWriter()
        {
            Clear();
        }

        /// <summary>
        /// Clears the internal buffer.
        /// </summary>
        public void Clear()
        {
            _buffer = new StringBuilder();
        }

        /// <summary>
        /// Adds the specified number of bits from the value to the buffer.
        /// </summary>
        /// <param name="value">The integer value to add bits from.</param>
        /// <param name="numberOfBits">The number of bits to add.</param>
        public void AddBits(int value, int numberOfBits)
        {
            _buffer.Append(Convert.ToString(value, 2).PadLeft(numberOfBits, '0'));
        }

        /// <summary>
        /// Converts the accumulated bits to a list of bytes.
        /// </summary>
        /// <returns>A list of bytes representing the bits.</returns>
        public List<byte> GetBytes()
        {
            var res = new List<byte>();
            var allAsString = _buffer.ToString();

            while (allAsString.Length >= 8)
            {
                var b = allAsString.Substring(0, 8);
                allAsString = allAsString.Remove(0, 8);
                res.Add(Convert.ToByte(b, 2));
            }

            return res;
        }
    }
}
