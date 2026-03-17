using LoggerService;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace RTLSDR.Common
{
    /// <summary>
    /// Interface providing information about a thread worker's performance and status.
    /// </summary>
    public interface IThreadWorkerInfo
    {
        /// <summary>
        /// Gets the total uptime in milliseconds.
        /// </summary>
        double UpTimeMS { get; }

        /// <summary>
        /// Gets the working time in milliseconds.
        /// </summary>
        double WorkingTimeMS { get; }

        /// <summary>
        /// Gets the total uptime in seconds.
        /// </summary>
        int UpTimeS { get; }

        /// <summary>
        /// Gets the name of the thread worker.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the number of items in the queue.
        /// </summary>
        int QueueItemsCount { get; }

        /// <summary>
        /// Gets the number of processing cycles completed.
        /// </summary>
        long CyclesCount { get; }
    }
}


