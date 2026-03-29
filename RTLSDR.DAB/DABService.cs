using RTLSDR.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The DAB service.
    /// </summary>
    public class DABService : IAudioService
    {
        /// <summary>
        /// Gets or sets the service name.
        /// </summary>
        public string ServiceName { get; set; } = "";
        /// <summary>
        /// Gets or sets the service number.
        /// </summary>
        public uint ServiceNumber { get; set; } = 0; // Service reference

        /// <summary>
        /// Gets or sets the country id.
        /// </summary>
        public string? CountryId { get; set; }
        /// <summary>
        /// Gets or sets the extended country code.
        /// </summary>
        public string? ExtendedCountryCode { get; set; } // ECC

        /// <summary>
        /// Gets or sets the Components.
        /// </summary>
        public List<DABComponent>? Components { get; set; }

        public DABService()
        {
            Components = new List<DABComponent>();
        }

        public int SubChannelsCount
        {
            get
            {
                if (Components == null)
                    return 0;

                return Components.Count;
            }
        }

        public DABSubChannel? FirstSubChannel
        {
            get
            {
                if ((Components == null) ||
                    (Components.Count == 0) ||
                    (Components[0].SubChannel == null)
                    )
                {
                    return null;
                }

                return Components[0].SubChannel;
            }
        }

        public void SetSubChannels(Dictionary<uint,DABSubChannel> SubChanels)
        {
            if (Components == null)
            {
                return;
            }

            foreach (var component in Components)
            {
                foreach (var subc in SubChanels)
                {
                    if (component.SubChannel == null &&
                        component.Description is MSCStreamAudioDescription a &&
                        subc.Key == a.SubChId)
                    {
                        component.SubChannel = subc.Value;
                    }
                }
            }
        }

        public void SetServiceLabels(Dictionary<uint, DABProgrammeServiceLabel> ServiceLabels)
        {
            if (ServiceName == null)
            {
                foreach (var label in ServiceLabels)
                {
                    if (label.Key == ServiceNumber && ServiceName == null)
                    {
                        ServiceName = label.Value.ServiceLabel ?? string.Empty;
                    }
                }
            }
        }

        public DABComponent? GetComponentBySubChId(uint subChId)
        {
            if (Components == null)
            {
                return null;
            }

            foreach (var component in Components)
            {
                if ((component.Description is MSCStreamAudioDescription ad) && (ad.SubChId == subChId))
                {
                    return component;
                }
                if ((component.Description is MSCStreamDataDescription dd) && (dd.SubChId == subChId))
                {
                    return component;
                }
            }

            return null;
        }

        public override string ToString()
        {
            var res = new StringBuilder();

            res.AppendLine($"\tServiceName:             {ServiceName}");
            res.AppendLine($"\tServiceNumber:           {ServiceNumber}");
            res.AppendLine($"\tCountryId:               {CountryId}");
            res.AppendLine($"\tExtendedCountryCode:     {ExtendedCountryCode}");
            res.AppendLine($"\tComponentsCount:         {Components?.Count}");

            if (Components != null)
            {
                for (var i=0;i< Components.Count;i++)
                {
                    if (Components[i].SubChannel == null)
                    {
                        res.AppendLine($"\t                         No sub channel yet");
                    } else
                    {
                        res.AppendLine($"\tSubchannel:");
                        res.AppendLine($"\t  StartAddr:             {Components[i]?.SubChannel?.StartAddr}");
                        res.AppendLine($"\t  Length   :             {Components[i]?.SubChannel?.Length}");
                    }

                    if (Components[i].Description is MSCStreamAudioDescription a)
                    {
                        res.AppendLine($"\t#{i.ToString().PadLeft(5,' ')}:    SubChId:     {a.SubChId} (pr: {a.Primary})");

                        if (Components[i].SubChannel != null)
                        {
                            res.AppendLine($"\t           BitRate:     {Components[i]?.SubChannel?.Bitrate}");
                            res.AppendLine($"\t           EEP    :     {Components[i]?.SubChannel?.ProtectionLevel}");
                        }
                        res.AppendLine($"\t           Audio");
                    }
                    if (Components[i].Description is MSCStreamDataDescription d)
                    {
                        res.AppendLine($"\t#{i.ToString().PadLeft(5,' ')}:    SubChId :     {d.SubChId} (pr: {d.Primary})");
                        res.AppendLine($"\t           Data");
                    }
                    if (Components[i].Description is MSCPacketDataDescription p)
                    {
                        res.AppendLine($"\t#{i.ToString().PadLeft(5, ' ')}:    Identifier:      {p.ServiceComponentIdentifier} (pr: {p.Primary})");
                        res.AppendLine($"\t           Packets");
                    }
                }
            }
            res.AppendLine($"\t----------------------------------------");
            return res.ToString();
        }
    }
}
