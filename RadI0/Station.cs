using RTLSDR.DAB;

namespace RadI0;

    /// <summary>
    /// The Station.
    /// </summary>
    public class Station
    {
        public StationTypeEnum StationType { get;set;} = StationTypeEnum.FM;

        /// <summary>
        /// Gets or sets the Name.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Gets or sets the ServiceNumber.
        /// </summary>
        public int ServiceNumber { get; set; }
        /// <summary>
        /// Gets or sets the Frequency.
        /// </summary>
        public int Frequency { get; set; }

        /// <summary>
        /// Gets or sets the dab service.
        /// </summary>
        public DABService? Service { get; set; }

        public Station(StationTypeEnum stationType, string name, int serviceNumber, int frequency)
        {
            StationType = stationType;
            Name = name;
            ServiceNumber = serviceNumber;
            Frequency = frequency;
        }
    }