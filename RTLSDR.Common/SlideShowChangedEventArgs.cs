using System;

namespace RTLSDR.Common
{
    /// <summary>
    /// Event arguments for when a DAB MOT SlideShow image has been demodulated.
    /// </summary>
    public class SlideShowChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the demodulated DAB slide.
        /// </summary>
        public DABSlide Slide { get; set; } = new DABSlide();

        /// <summary>
        /// Convenience shortcut to the raw image bytes.
        /// </summary>
        public byte[] ImageBytes => Slide.ImageBytes;

        /// <summary>
        /// Convenience shortcut to the MIME type (e.g. "image/jpeg", "image/png").
        /// </summary>
        public string MimeType => Slide.MimeType;

        /// <summary>
        /// Convenience shortcut to the content name.
        /// </summary>
        public string ContentName => Slide.ContentName;
    }
}
