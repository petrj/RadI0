using System;
using System.Text;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The DAB programme service label.
    /// </summary>
    public class DABProgrammeServiceLabel
    {
        /// <summary>
        /// Gets or sets the service label.
        /// </summary>
        public string? ServiceLabel { get; set; } = null;

        /// <summary>
        /// Gets or sets the service number.
        /// </summary>
        public uint ServiceNumber { get; set; } = 0;
        /// <summary>
        /// Gets or sets the country id.
        /// </summary>
        public string CountryId { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the extended country code.
        /// </summary>
        public string ExtendedCountryCode { get; set; } = string.Empty; // ECC

        public override string ToString()
        {
            var res = new StringBuilder();

            res.AppendLine($"\t----Service-----------------------------");
            res.AppendLine($"\tServiceLabel:           {ServiceLabel}");
            res.AppendLine($"\tServiceIdentifier:      {ServiceNumber}");
            res.AppendLine($"\tCountryId:              {CountryId}");
            res.AppendLine($"\t----------------------------------------");

            return res.ToString();
        }
    }
}
