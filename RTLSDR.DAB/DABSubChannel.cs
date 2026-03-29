using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The DAB sub channel.
    /// </summary>
    public class DABSubChannel
    {
        /// <summary>
        /// Gets or sets the sub ch id.
        /// </summary>
        public uint SubChId { get; set; } = 0;
        /// <summary>
        /// Gets or sets the start addr.
        /// </summary>
        public uint StartAddr { get; set; } = 0;
        /// <summary>
        /// Gets or sets the Length.
        /// </summary>
        public uint Length { get; set; } = 0;
        /// <summary>
        /// Gets or sets the Bitrate.
        /// </summary>
        public int Bitrate { get; set; } = 0;
        /// <summary>
        /// Gets or sets the protection level.
        /// </summary>
        public EEPProtectionLevel ProtectionLevel { get; set; } = EEPProtectionLevel.EEP_1;

        /// <summary>
        /// Gets or sets the protection profile.
        /// </summary>
        public EEPProtectionProfile ProtectionProfile { get; set; } = EEPProtectionProfile.EEP_A;


        public override string ToString()
        {
            var res = new StringBuilder();

            res.AppendLine($"\t----Sub channel-----------------");
            res.AppendLine($"\tSubChId:           {SubChId}");
            res.AppendLine($"\tStartAddr:         {StartAddr}");
            res.AppendLine($"\tLength:            {Length}");
            res.AppendLine($"\tBitrate:           {Bitrate}");
            res.AppendLine($"\tEEP:               {ProtectionLevel}");
            res.AppendLine($"\t----------------------------------------");

            return res.ToString();
        }
    }
}
