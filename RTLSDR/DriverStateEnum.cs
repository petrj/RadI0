using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTLSDR
{
    /// <summary>
    /// SDR driver state.
    /// </summary>
    public enum DriverStateEnum
    {
        NotInitialized = 0,
        Connected = 1,
        DisConnected = 2,
        Error = 3
    }
}

