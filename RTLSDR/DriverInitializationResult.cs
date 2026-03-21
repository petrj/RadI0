using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR
{
    /// <summary>
    /// Result of a successful driver initialization.
    /// </summary>
    public class DriverInitializationResult
    {
        /// <summary>
        /// Gets or sets the supported TCP commands.
        /// </summary>
        public int[]? SupportedTcpCommands { get; set; }

        /// <summary>
        /// Gets or sets the device name.
        /// </summary>
        public string? DeviceName { get; set; }

        /// <summary>
        /// Gets or sets the output recording directory.
        /// </summary>
        public string? OutputRecordingDirectory { get; set; }
    }
}
