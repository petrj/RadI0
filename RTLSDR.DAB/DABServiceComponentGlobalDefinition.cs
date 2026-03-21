using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR.DAB
{
    public class DABServiceComponentGlobalDefinition
    {
        public uint ServiceIdentifier { get; set; } = 0;
        public uint SCIdS { get; set; } = 0;
        public uint SCId { get; set; } = 0;
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
