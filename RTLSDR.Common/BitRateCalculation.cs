using LoggerService;
using System;
using System.Collections.Generic;
using System.Text;

namespace RTLSDR.Common
{
    /// <summary>
    /// Calculates and tracks the bitrate of data streams, providing formatted string representations.
    /// </summary>
    public class BitRateCalculation
    {
        private ILoggingService _loggingService;
        private DateTime _lastSpeedCalculationTime;
        private int _bytesReadFromLastSpeedCalculationTime;
        private double _bitRate;
        private string _description;

        /// <summary>
        /// Initializes a new instance of the <see cref="BitRateCalculation"/> class.
        /// </summary>
        /// <param name="loggingService">The logging service for diagnostics.</param>
        /// <param name="description">A description of the bitrate calculation context.</param>
        public BitRateCalculation(ILoggingService loggingService, string description)
        {
            _loggingService = loggingService;
            _description = description;

            _loggingService.Info($"Initializing BitRateCalculation: {_description}");

            _bytesReadFromLastSpeedCalculationTime = 0;
            _lastSpeedCalculationTime = DateTime.Now;
            _bitRate = 0;
        }

        /// <summary>
        /// Gets the bitrate as a formatted string (e.g., "1.23 Mb/s" or "128 Kb/s").
        /// </summary>
        public string BitRateAsString
        {
            get
            {
                return AudioTools.GetBitRateAsString(_bitRate);
            }
        }

        /// <summary>
        /// Gets the bitrate as a short formatted string without padding.
        /// </summary>
        public string BitRateAsShortString
        {
            get
            {
                if (_bitRate > 1000000)
                {
                    return $"{(_bitRate / 1000000).ToString("N2")}  Mb/s";
                }
                else
                {
                    return $"{(_bitRate / 1000).ToString("N0")}  Kb/s";
                }
            }
        }

        /// <summary>
        /// Gets the current bitrate in bits per second.
        /// </summary>
        public double BitRate
        {
            get
            {
                return _bitRate;
            }
        }

        /// <summary>
        /// Updates the bitrate calculation with the number of bytes read.
        /// </summary>
        /// <param name="bytesRead">The number of bytes read since the last update.</param>
        /// <returns>The current bitrate in bits per second.</returns>
        public double UpdateBitRate(int bytesRead)
        {
            var now = DateTime.Now;

            var totalSeconds = (now - _lastSpeedCalculationTime).TotalSeconds;
            if (totalSeconds > 1)
            {
                _bitRate = _bytesReadFromLastSpeedCalculationTime * 8 / totalSeconds;
                _lastSpeedCalculationTime = now;
                _bytesReadFromLastSpeedCalculationTime = 0;
            }
            else
            {
                _bytesReadFromLastSpeedCalculationTime += bytesRead;
            }

            return _bitRate;
        }
    }
}
