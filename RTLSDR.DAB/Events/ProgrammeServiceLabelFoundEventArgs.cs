using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The programme service label found event args.
    /// </summary>
    public class ProgrammeServiceLabelFoundEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the programme service label.
        /// </summary>
        public DABProgrammeServiceLabel? ProgrammeServiceLabel { get; set; }
    }
}
