using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The DAB service component label.
    /// </summary>
    public class DABServiceComponentLabel
    {
        /// <summary>
        /// Gets or sets the service identifier.
        /// </summary>
        public uint ServiceIdentifier { get; set; } = 0;

        public override string ToString()
        {
            var res = new StringBuilder();

            res.AppendLine($"\t----Service component label-----------------");
            res.AppendLine($"\tServiceIdentifier:           {ServiceIdentifier}");
            res.AppendLine($"\t----------------------------------------");

            return res.ToString();
        }
    }
}
