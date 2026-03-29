using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The service component label found event args.
    /// </summary>
    public class ServiceComponentLabelFoundEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the service label.
        /// </summary>
        public DABServiceComponentLabel ServiceLabel  { get;set; } = new DABServiceComponentLabel();
    }
}
