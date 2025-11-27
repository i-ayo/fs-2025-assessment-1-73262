using System.Text.Json;
using fs_2025_dublinbike_api.Models;

namespace fs_2025_dublinbike_api.Repositories
{
    // File-based repository for V1 (reads JSON at startup into memory).
    // Registered as a singleton so the same in-memory list is used across the app.

    // File-based repo for V1 — loads JSON into memory at startup and exposes operations.
    public class FileJsonBikeStationRepository : IBikeStationRepository
    {
        private readonly List<BikeStation> _store = new();

        public FileJsonBikeStationRepository(IConfiguration config)
        {
            // find file path (Data/dublinbike.json)
            var baseDir = AppContext.BaseDirectory;
            var file = Path.Combine(baseDir, "Data", "dublinbike.json");

            if (!File.Exists(file))
                throw new FileNotFoundException($"Required data file not found: {file}");

            var json = File.ReadAllText(file);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var items = JsonSerializer.Deserialize<List<BikeStation>>(json, opts) ?? new List<BikeStation>();
            _store.AddRange(items);
        }

        public Task AddAsync(BikeStation station)
        {
            _store.Add(station);
            return Task.CompletedTask;
        }

        public Task<BikeStation?> GetByNumberAsync(int number)
        {
            var item = _store.FirstOrDefault(s => s.number == number);
            return Task.FromResult(item);
        }

        public Task<List<BikeStation>> GetAllAsync()
        {
            // Return a shallow copy to avoid external mutation of internal list
            return Task.FromResult(_store.ToList());
        }

        public Task ReplaceAllAsync(IEnumerable<BikeStation> stations)
        {
            lock (_store)
            {
                _store.Clear();
                _store.AddRange(stations);
            }
            return Task.CompletedTask;
        }

        public Task<bool> UpdateAsync(BikeStation station)
        {
            var idx = _store.FindIndex(s => s.number == station.number);
            if (idx == -1) return Task.FromResult(false);
            _store[idx] = station;
            return Task.FromResult(true);
        }
    }
}


