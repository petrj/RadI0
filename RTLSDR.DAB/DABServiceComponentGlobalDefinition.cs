using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The DAB service component global definition.
    /// </summary>
    public class DABServiceComponentGlobalDefinition
    {
        /// <summary>
        /// Gets or sets the service identifier.
        /// </summary>
        public uint ServiceIdentifier { get; set; } = 0;
        /// <summary>
        /// Gets or sets the sc id s.
        /// </summary>
        public uint SCIdS { get; set; } = 0;
        /// <summary>
        /// Gets or sets the sc id.
        /// </summary>
        public uint SCId { get; set; } = 0;
        /// <summary>
        /// Gets or sets the sub ch id.
        /// </summary>
        public uint SubChId { get; set; } = 0;

        public override string ToString()
        {
            var res = new StringBuilder();

            res.AppendLine($"\t----Service component global definition-----------------");
            res.AppendLine($"\tServiceIdentifier:           {ServiceIdentifier}");
            res.AppendLine($"\tSCIdS:                       {SCIdS}");
            res.AppendLine($"\tSCId:                        {SCId}");
            res.AppendLine($"\tSubChId:                     {SubChId}");
            res.AppendLine($"\t----------------------------------------");

            return res.ToString();
        }
    }
}
