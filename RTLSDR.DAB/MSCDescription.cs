using System;

namespace RTLSDR.DAB
{
    public abstract class MSCDescription
    {
        public bool Primary { get; set; } = false;

        public bool AccessControl { get; set; } = false;
        // false: no access control or access control applies only to a part of the service component;
        // true: access control applies to the whole of the service component.
    }
}
