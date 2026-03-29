using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The ensemble found event args.
    /// </summary>
    public class EnsembleFoundEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the ensemble.
        /// </summary>
        public DABEnsemble? Ensemble { get; set; }
    }
}
