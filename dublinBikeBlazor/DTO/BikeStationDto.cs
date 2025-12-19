namespace dublinBikeBlazor.DTO
{
    public class BikeStationDto
    {
        public string id { get; set; } = string.Empty;
        public int Number { get; set; }
        public string ContractName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        // Location
        public double Lat { get; set; }
        public double Lng { get; set; }

        // Virtual joined position (UI only)
        public (double Lat, double Lng) Position
            => (Lat, Lng);

        // Capacity + availability
        public int BikeStands { get; set; }
        public int AvailableBikeStands { get; set; }
        public int AvailableBikes { get; set; }
        public string Status { get; set; } = "OPEN";

        // Timestamps (stored both UTC + local)
        public DateTimeOffset LastUpdateUtc { get; set; }
        public DateTimeOffset LastUpdateLocal { get; set; }

        // Calculated value
        public double Occupancy { get; set; }
    }
}
