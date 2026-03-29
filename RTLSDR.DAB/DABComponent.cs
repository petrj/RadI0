using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The DAB component.
    /// </summary>
    public class DABComponent
    {
        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public MSCDescription? Description { get; set; }
        /// <summary>
        /// Gets or sets the sub channel.
        /// </summary>
        public DABSubChannel? SubChannel { get; set; }
    }
}
