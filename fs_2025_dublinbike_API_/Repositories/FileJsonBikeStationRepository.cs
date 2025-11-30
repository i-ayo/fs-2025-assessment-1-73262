using System.Text.Json;
using fs_2025_dublinbike_API_.Models;

namespace fs_2025_dublinbike_API_.Repositories
{
    // File-based JSON repository for BikeStation
    public class FileJsonBikeStationRepository : IBikeStationRepository
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private readonly List<BikeStation> _store = new();

        // Constructor - loads data from JSON file
        public FileJsonBikeStationRepository(IConfiguration config)
        {
            var baseDir = AppContext.BaseDirectory;
            var file = Path.Combine(baseDir, "Data", "dublinbike.json");
            
            // Check file exists
            if (!File.Exists(file))
                throw new FileNotFoundException($"Required data file not found: {file}");
            
            // Load and deserialize JSON
            var json = File.ReadAllText(file);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var items = JsonSerializer.Deserialize<List<BikeStation>>(json, opts) ?? new List<BikeStation>();

            // ensure id is set
            foreach (var s in items)
            {
                if (string.IsNullOrWhiteSpace(s.id)) s.id = s.number.ToString();
                _store.Add(s);
            }
        }

        // Add new station
        public async Task AddAsync(BikeStation station)
        {
            // Ensure id is set
            await _semaphore.WaitAsync();
            // Add to store
            try
            {
                if (string.IsNullOrWhiteSpace(station.id)) station.id = station.number.ToString();
                _store.Add(station);
            }
            finally { _semaphore.Release(); }
        }

        // Get station by number
        public async Task<BikeStation?> GetByNumberAsync(int number)
        {
            // Get station by number
            await _semaphore.WaitAsync();
            try { return _store.FirstOrDefault(s => s.number == number); }
            finally { _semaphore.Release(); }
        }

        // Get station by id
        public async Task<BikeStation?> GetByIdAsync(string id)
        {
            // Get station by id
            await _semaphore.WaitAsync();
            try { return _store.FirstOrDefault(s => s.id == id); }
            finally { _semaphore.Release(); }
        }

        // Get all stations
        public async Task<List<BikeStation>> GetAllAsync()
        {
            // Return a copy of the list
            await _semaphore.WaitAsync();
            try { return _store.Select(s => s).ToList(); }
            finally { _semaphore.Release(); }
        }

        // Replace all stations
        public async Task ReplaceAllAsync(IEnumerable<BikeStation> stations)
        {
            // Ensure all stations have id set
            var newList = stations.Select(s =>
            {
                if (string.IsNullOrWhiteSpace(s.id)) s.id = s.number.ToString();
                return s;
            }).ToList();

            // Replace store contents
            await _semaphore.WaitAsync();
            try
            {
                _store.Clear();
                _store.AddRange(newList);
            }
            finally { _semaphore.Release(); }
        }

        // Update existing station
        public async Task<bool> UpdateAsync(BikeStation station)
        {
            // Find by number and update
            await _semaphore.WaitAsync();
            try
            {
                var idx = _store.FindIndex(s => s.number == station.number);
                if (idx == -1) return false;
                if (string.IsNullOrWhiteSpace(station.id)) station.id = station.number.ToString();
                _store[idx] = station;
                return true;
            }
            finally { _semaphore.Release(); }
        }
    }
}
