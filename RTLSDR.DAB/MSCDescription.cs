using System;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The msc description.
    /// </summary>
    public abstract class
    {
        /// <summary>
        /// Gets or sets a value indicating whether primary.
        /// </summary>
        public bool Primary { get; set; } = false;

        /// <summary>
        /// false: no access control or access control applies only to a part of the service component;
        /// true: access control applies to the whole of the service component.
        /// </summary>
        public bool AccessControl { get; set; } = false;
    }
}
