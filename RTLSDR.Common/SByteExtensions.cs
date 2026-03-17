using System;
using System.Collections.Generic;
using System.Text;

namespace RTLSDR.Common
{
    /// <summary>
    /// Provides extension methods for sbyte arrays.
    /// </summary>
    public static class SByteExtensions
    {
        /// <summary>
        /// Clones a portion of the sbyte array.
        /// </summary>
        /// <param name="array">The source array.</param>
        /// <param name="count">The number of elements to clone; if -1, clones the entire array.</param>
        /// <returns>A new sbyte array containing the cloned elements.</returns>
        public static sbyte[] CloneArray(this sbyte[] array, int count = -1)
        {
            if (count == -1)
                count = array.Length;

            var arrayCopy = new sbyte[count];
            Buffer.BlockCopy(array, 0, arrayCopy, 0, count);
            return arrayCopy;
        }
    }
}
