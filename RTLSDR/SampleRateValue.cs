using System;
using System.Collections.Generic;
using System.Text;

namespace RTLSDR
{
    /// <summary>
    /// Sample rate value for the SDR.
    /// </summary>
    public class SampleRateValue
    {
        /// <summary>
        /// Gets or sets the sample rate value in Hz.
        /// </summary>
        public int Value { get; set; } = 1000000;

        /// <summary>
        /// Initializes a new instance of the SampleRateValue class.
        /// </summary>
        /// <param name="value">The sample rate value in Hz.</param>
        public SampleRateValue(int value)
        {
            Value = value;
        }

        /// <summary>
        /// Returns a string representation of the sample rate.
        /// </summary>
        /// <returns>The string representation.</returns>
        public override string ToString()
        {
            var val = Value / 1000;

            return val.ToString($"{val.ToString("N0")} Ks/s");
        }
    }
}
