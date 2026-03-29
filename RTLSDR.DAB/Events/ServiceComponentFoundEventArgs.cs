using System;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The service component found event args.
    /// </summary>
    public class ServiceComponentFoundEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the service component.
        /// </summary>
        public DABService? ServiceComponent { get; set; }
    }
}
