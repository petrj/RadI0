using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR
{
    /// <summary>
    /// Result of a failed driver initialization.
    /// </summary>
    public class DriverInitializationFailedResult
    {
        /// <summary>
        /// Gets or sets the error ID.
        /// </summary>
        public int ErrorId { get; set; }

        /// <summary>
        /// Gets or sets the exception code.
        /// </summary>
        public int ExceptionCode { get; set; }

        /// <summary>
        /// Gets or sets the detailed description of the error.
        /// </summary>
        public string DetailedDescription { get; set; }
    }
}
