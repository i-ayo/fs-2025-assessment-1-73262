namespace fs_2025_dublinbike_API_.Models
{
    // Holds a latitude/longitude pair
    public class Position
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }

    // Internal domain model (stored in file / cosmos)
    public class BikeStation
    {
        // Cosmos expects 'id' string; we use it as the canonical DB id.
        public string id { get; set; } = string.Empty;

        // optional partitionKey,not necessary when partition is /id
        public string? partitionKey { get; set; }

        // keep number for human-friendly lookup
        public int number { get; set; }

        public string contract_name { get; set; } = string.Empty;
        public string name { get; set; } = string.Empty;
        public string address { get; set; } = string.Empty;
        public Position? position { get; set; }
        public bool banking { get; set; }
        public bool bonus { get; set; }
        public int bike_stands { get; set; }
        public int available_bike_stands { get; set; }
        public int available_bikes { get; set; }
        public string status { get; set; } = "OPEN";

        // Updated timestamps
        public DateTimeOffset last_update_utc { get; set; }
        public DateTimeOffset last_update_local { get; set; }


    }
}
