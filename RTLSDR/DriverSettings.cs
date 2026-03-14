using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR
{
    /// <summary>
    /// Settings for the SDR driver.
    /// </summary>
    public class DriverSettings
    {
        /// <summary>
        /// Gets or sets the port number for the driver.
        /// </summary>
        public int Port { get; set; } = 1234;

        /// <summary>
        /// Gets or sets the SDR sample rate.
        /// </summary>
        public int SDRSampleRate { get; set; } = 1056000;

        /// <summary>
        /// Gets or sets the IP address.
        /// </summary>
        public string IP { get; set; } = "127.0.0.1";

        /// <summary>
        /// Gets or sets the stream port.
        /// </summary>
        public int Streamport { get; set; } = 1235;
    }
}
