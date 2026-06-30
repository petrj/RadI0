namespace RadI0;

public class DelStationEventArgs : EventArgs
{
        public Station? SelectedSation { get; set; } = null;
        public bool DeleteAllFM { get; set; } = false;
        public bool DeleteAllDAB { get; set; } = false;
}
