using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR.DAB
{
    public class Peak : IComparable
    {
        public int Index { get; set; } = -1;
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
