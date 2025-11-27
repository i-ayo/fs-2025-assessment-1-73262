using fs_2025_dublinbike_api.Models;

namespace fs_2025_dublinbike_api.DTOs
{
    // DTO returned to clients — C#-style naming and computed fields.
    public class BikeStationDto
    {
        public int Number { get; set; }
        public string ContractName { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public float Lat { get; set; }
        public float Lng { get; set; }
        public int BikeStands { get; set; }
        public int AvailableBikeStands { get; set; }
        public int AvailableBikes { get; set; }
        public string Status { get; set; } = string.Empty;
        // Converted last_update (epoch ms) -> DateTimeOffset in Europe/Dublin and as UTC
        public DateTimeOffset LastUpdateUtc { get; set; }
        public DateTimeOffset LastUpdateLocal { get; set; } // Europe/Dublin
        // occupancy computed in service: availableBikes / bikeStands as value between 0 and 1
        public double Occupancy { get; set; }
    }
}
