using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

namespace RTLSDR.Common
{
    /// <summary>
    /// Calculates RF power from IQ data samples.
    /// </summary>
    public class PowerCalculation
    {
        // https://www.tek.com/en/blog/calculating-rf-power-iq-samples

        private DateTime _lastCalculationTime;
        private double _lastPower;
        private double _maxPower;

        /// <summary>
        /// Initializes a new instance of the <see cref="PowerCalculation"/> class.
        /// </summary>
        public PowerCalculation()
        {
            _lastCalculationTime = DateTime.MinValue;
            _lastPower = 0;
            _maxPower = MaxPower;
        }

        /// <summary>
        /// Gets the maximum possible power value.
        /// </summary>
        public static double MaxPower
        {
            get { return 238; }  // 10*ln(x) => 0 .. 238
        }

        /// <summary>
        /// Gets the current power as a percentage based on byte IQ data.
        /// </summary>
        /// <param name="IQData">The IQ data buffer.</param>
        /// <param name="bytesRead">The number of bytes read.</param>
        /// <returns>The power as a percentage.</returns>
        public double GetPowerPercent(byte[] IQData, int bytesRead)
        {
            var now = DateTime.Now;

            var totalSeconds = (now - _lastCalculationTime).TotalSeconds;
            if (totalSeconds > 1)
            {
                if (IQData.Length > 0)
                {
                    _lastPower = GetAvgPower(IQData, bytesRead, 100);
                }
                else
                {
                    _lastPower = 0;
                }

                _lastCalculationTime = now;
            }

            return _lastPower / (_maxPower / 100);
        }

        /// <summary>
        /// Gets the current power as a percentage based on short IQ data.
        /// </summary>
        /// <param name="IQData">The IQ data buffer.</param>
        /// <param name="count">The number of samples to consider.</param>
        /// <returns>The power as a percentage.</returns>
        public double GetPowerPercent(short[] IQData, int count)
        {
            var now = DateTime.Now;

            var c = count >= 100 ? 100 : count;

            var totalSeconds = (now - _lastCalculationTime).TotalSeconds;
            if (totalSeconds > 1)
            {
                if (IQData.Length > 0)
                {
                    _lastPower = GetAvgPower(IQData, c);
                }
                else
                {
                    _lastPower = 0;
                }

                _lastCalculationTime = now;
            }

            return _lastPower / (_maxPower / c);
        }

        /// <summary>
        /// Gets the current power as a percentage based on all short IQ data.
        /// </summary>
        /// <param name="IQData">The IQ data buffer.</param>
        /// <returns>The power as a percentage.</returns>
        public double GetPowerPercent(short[] IQData)
        {
            return GetPowerPercent(IQData, IQData.Length);
        }

        /// <summary>
        /// Calculates the current power for a single I/Q pair.
        /// </summary>
        /// <param name="I">The I component.</param>
        /// <param name="Q">The Q component.</param>
        /// <returns>The power value.</returns>
        public static double GetCurrentPower(int I, int Q)
        {
            if (I == 0 && Q == 0) return 0;

            return 10 * Math.Log(10 * (Math.Pow(I, 2) + Math.Pow(Q, 2)));
        }

        /// <summary>
        /// Calculates the average power from byte IQ data.
        /// </summary>
        /// <param name="IQData">The IQ data buffer.</param>
        /// <param name="bytesRead">The number of bytes read.</param>
        /// <param name="valuesCount">The number of values to average.</param>
        /// <returns>The average power.</returns>
        public static double GetAvgPower(byte[] IQData, int bytesRead, int valuesCount = 100)
        {
            // first 100 numbers:

            if (valuesCount > IQData.Length / 2)
            {
                valuesCount = IQData.Length / 2;
            }

            if (valuesCount > bytesRead * 2)
            {
                valuesCount = bytesRead * 2;
            }

            double avgPower = 0;

            for (var i = 0; i < valuesCount * 2; i = i + 2)
            {
                var power = GetCurrentPower(IQData[i + 0] - 127, IQData[i + 1] - 127);

                avgPower += power / valuesCount;
            }

            return avgPower;
        }

        /// <summary>
        /// Calculates the average power from short IQ data.
        /// </summary>
        /// <param name="IQData">The IQ data buffer.</param>
        /// <param name="valuesCount">The number of values to average.</param>
        /// <returns>The average power.</returns>
        public static double GetAvgPower(short[] IQData, int valuesCount = 100)
        {
            // first 100 numbers:

            if (valuesCount > IQData.Length / 2)
            {
                valuesCount = IQData.Length / 2;
            }

            double avgPower = 0;

            for (var i = 0; i < valuesCount * 2; i = i + 2)
            {
                var power = GetCurrentPower(IQData[i + 0], IQData[i + 1]);

                avgPower += power / valuesCount;
            }

            return avgPower;
        }
    }
}
