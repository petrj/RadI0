using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The Peak.
    /// </summary>
    public class Peak : IComparable
    {
        /// <summary>
        /// Gets or sets the Index.
        /// </summary>
        public int Index { get; set; } = -1;
        /// <summary>
        /// Gets or sets the Value.
        /// </summary>
        public double Value { get; set; } = 0;

        public int CompareTo(object? obj)
        {
            if (obj is Peak p)
            {
                return p.Value.CompareTo(Value);
            }

            throw new ArgumentException("Object is not a Peak");
        }

        public override string ToString()
        {
            return $"Peak: Index: {Index}, Value: {Value}";
        }
    }
}
