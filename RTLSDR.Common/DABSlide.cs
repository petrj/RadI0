using System;

namespace RTLSDR.Common
{
    /// <summary>
    /// Represents a decoded DAB MOT (Multimedia Object Transfer) SlideShow image.
    /// References: ETSI EN 301 234, ETSI TS 101 499.
    /// </summary>
    public class DABSlide
    {
        /// <summary>
        /// Raw binary data of the image (JPEG, PNG, etc.).
        /// </summary>
        public byte[] ImageBytes { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// MIME type of the image (e.g. "image/jpeg", "image/png").
        /// </summary>
        public string MimeType { get; set; } = string.Empty;

        /// <summary>
        /// File name or title from the MOT header ContentName parameter.
        /// </summary>
        public string ContentName { get; set; } = string.Empty;

        /// <summary>
        /// Optional category title from the MOT header CategoryTitle parameter.
        /// </summary>
        public string CategoryTitle { get; set; } = string.Empty;

        /// <summary>
        /// Optional click-through URL from the MOT header ClickThroughURL parameter.
        /// </summary>
        public string ClickThroughUrl { get; set; } = string.Empty;

        /// <summary>
        /// MOT Transport ID associated with this slide.
        /// </summary>
        public int TransportId { get; set; }

        /// <summary>
        /// Timestamp when this slide was completed.
        /// </summary>
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
