using System;
using System.Collections.Generic;
using System.Threading;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The viterbi decision.
    /// </summary>
    public class ViterbiDecision
    {
        /// <summary>
        /// The numstates constant.
        /// </summary>
        public const int NUMSTATES = 64;

        /// <summary>
        /// The w.
        /// </summary>
        public uint[] w = new uint[NUMSTATES/32];

        public override string ToString()
        {
            return $"W[0]={w[0]}, W[1]={w[1]}";
        }
    }

    /// <summary>
    /// The viterbi metric.
    /// </summary>
    public class ViterbiMetric
    {
        /// <summary>
        /// The numstates constant.
        /// </summary>
        public const int NUMSTATES = 64;

        /// <summary>
        /// The t.
        /// </summary>
        public uint[] t = new uint[NUMSTATES];
    }

    /// <summary>
    /// The viterbi state info.
    /// </summary>
    public class ViterbiStateInfo
    {
        private bool _swapped = false;

        private readonly ViterbiMetric _metrics1 = new ViterbiMetric();
        private readonly ViterbiMetric _metrics2 = new ViterbiMetric();

        /// <summary>
        /// Swap new and old metrics
        /// </summary>
        public void Swap()
        {
            _swapped = !_swapped;
        }

        public ViterbiMetric OldMetrics
        {
            get
            {
                if (_swapped)
                {
                    return _metrics2;
                }
                else
                {
                    return _metrics1;
                }
            }
        }

        public ViterbiMetric NewMetrics
        {
            get
            {
                if (_swapped)
                {
                    return _metrics1;
                }
                else
                {
                    return _metrics2;
                }
            }
        }

        /// <summary>
        /// Gets or sets the Decisions.
        /// </summary>
        public List<ViterbiDecision> Decisions { get; set; } = new List<ViterbiDecision> ();

        /// <summary>
        /// The starting state.
        /// </summary>
        public ViterbiStateInfo(int NUMSTATES, int starting_state = 0)
        {
            for (int i = 0; i < NUMSTATES; i++)
                _metrics1.t[i] = 63;

            /* Bias known start state */
            OldMetrics.t[starting_state & (NUMSTATES - 1)] = 0;
        }
    }
}
