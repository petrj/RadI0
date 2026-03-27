using LoggerService;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace RTLSDR.Common
{
    /// <summary>
    /// A generic thread worker that processes items from a queue or executes actions periodically.
    /// </summary>
    /// <typeparam name="T">The type of items to process.</typeparam>
    public class ThreadWorker<T> : IThreadWorkerInfo
    {
        private ConcurrentQueue<T>? _queue;
        private Thread? _thread = null;
        private readonly string _name;
        private readonly ILoggingService? _logger = null;

        private int _actionMSDelay = 1000;

        private DateTime _timeStarted = DateTime.MinValue;

        private const int MinThreadNoDataMSDelay = 25;

        private Action<T>? _action = null;

        private bool _running = false;

        private bool _threadRunning = false;

        private double _workingTimeMS = 0;

        /// <summary>
        /// Gets or sets a value indicating whether the worker is reading from the queue.
        /// </summary>
        public bool ReadingQueue { get; set; } = false;
        private long _cycles = 0;

        /// <summary>
        /// Initializes a new instance of the <see cref="ThreadWorker{T}"/> class.
        /// </summary>
        /// <param name="logger">The logging service.</param>
        /// <param name="name">The name of the thread worker.</param>
        public ThreadWorker(ILoggingService? logger, string name = "Threadworker")
        {
            _logger = logger;
            _name = name;
            _logger?.Debug($"Starting Threadworker {name}");
        }

        /// <summary>
        /// Gets a value indicating whether the thread is running.
        /// </summary>
        public bool ThreadRunning
        {
            get
            {
                return _threadRunning;
            }
        }

        /// <summary>
        /// Sets the action to execute and the delay between executions.
        /// </summary>
        /// <param name="action">The action to execute.</param>
        /// <param name="actionMSDelay">The delay in milliseconds between actions.</param>
        public void SetThreadMethod(Action<T> action, int actionMSDelay)
        {
            _action = action;
            _actionMSDelay = actionMSDelay;
        }

        /// <summary>
        /// Sets the queue to read items from.
        /// </summary>
        /// <param name="queue">The concurrent queue.</param>
        public void SetQueue(ConcurrentQueue<T> queue)
        {
            _queue = queue;
        }

        /// <summary>
        /// Starts the thread worker.
        /// </summary>
        public void Start()
        {
            _logger?.Debug($"Threadworker {_name} starting");
            _timeStarted = DateTime.Now;

            _running = true;
            _thread = new Thread(ThreadLoop);
            _thread.Start();
        }

        /// <summary>
        /// Stops the thread worker.
        /// </summary>
        public void Stop()
        {
            _logger?.Debug($"Stopping Threadworker {_name}");

            while (_threadRunning)
            {
                _running = false;
                Thread.Sleep(MinThreadNoDataMSDelay);
            }

            _logger?.Debug($"Threadworker {_name} stopped");
        }

        private void ThreadLoop()
        {
            try
            {
                _threadRunning = true;
                while (_running)
                {
                    _cycles++;
                    var data = default(T);

                    if (ReadingQueue)
                    {
                        var ok = _queue?.TryDequeue(out data);

                        if (_action != null && ok == true && data != null)
                        {
                            var startTime = DateTime.Now;

                            _action(data);

                            _workingTimeMS += (DateTime.Now - startTime).TotalMilliseconds;
                        } else
                        {
                            Thread.Sleep(_actionMSDelay);
                        }
                    } else
                    {
                        if (_action != null)
                        {
                            var startTime = DateTime.Now;

                            _action(default(T)!);

                            _workingTimeMS += (DateTime.Now - startTime).TotalMilliseconds;
                        }

                        Thread.Sleep(_actionMSDelay);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.Error(ex);
            }

            _threadRunning = false;
            _logger?.Debug($"Threadworker {_name} stopped");
        }

        /// <summary>
        /// Gets the total uptime in milliseconds.
        /// </summary>
        public double UpTimeMS
        {
            get
            {
                if (_timeStarted == DateTime.MinValue)
                    return 0;

                return (DateTime.Now - _timeStarted).TotalMilliseconds;
            }
        }

        /// <summary>
        /// Gets the working time in milliseconds.
        /// </summary>
        public double WorkingTimeMS
        {
            get
            {
                return _workingTimeMS;
            }
        }

        /// <summary>
        /// Gets the total uptime in seconds.
        /// </summary>
        public int UpTimeS
        {
            get
            {
                return Convert.ToInt32(UpTimeMS / 1000);
            }
        }

        /// <summary>
        /// Gets the name of the thread worker.
        /// </summary>
        public string Name
        {
            get
            {
                return _name;
            }
        }

        /// <summary>
        /// Gets the number of items in the queue.
        /// </summary>
        public int QueueItemsCount
        {
            get
            {
                if (_queue == null)
                    return 0;

                return _queue.Count;
            }
        }

        /// <summary>
        /// Gets the number of processing cycles completed.
        /// </summary>
        public long CyclesCount
        {
            get
            {
                return _cycles;
            }
        }
    }
}
