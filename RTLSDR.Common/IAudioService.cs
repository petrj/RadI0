using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR.Common
{
    /// <summary>
    /// Interface for audio services, providing a name identifier.
    /// </summary>
    public interface IAudioService
    {
        /// <summary>
        /// Gets or sets the name of the audio service.
        /// </summary>
        string ServiceName { get; set; }
    }
}
