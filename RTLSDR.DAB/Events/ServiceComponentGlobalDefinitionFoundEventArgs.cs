using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The service component global definition found event args.
    /// </summary>
    public class ServiceComponentGlobalDefinitionFoundEventArgs : EventArgs
    {
        /// <summary>
        /// Gets or sets the service global definition.
        /// </summary>
        public DABServiceComponentGlobalDefinition? ServiceGlobalDefinition { get; set; }
    }
}
