using System;

namespace RTLSDR.DAB
{
    /// <summary>
    /// The DAB ensemble.
    /// </summary>
    public class DABEnsemble
    {
        /// <summary>
        /// Gets or sets the ensemble label.
        /// </summary>
        public string? EnsembleLabel { get; set; } = null;
        /// <summary>
        /// Gets or sets the ensemble identifier.
        /// </summary>
        public int EnsembleIdentifier { get; set; } = -1;

        public override string ToString()
        {
            return $"{EnsembleLabel} (id {EnsembleIdentifier})";
        }
    }
}
