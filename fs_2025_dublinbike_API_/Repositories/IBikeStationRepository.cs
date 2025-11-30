using fs_2025_dublinbike_API_.Models;

namespace fs_2025_dublinbike_API_.Repositories
{
    // Contract for reading/writing station data (file or cosmos)
    public interface IBikeStationRepository
    {
        Task<List<BikeStation>> GetAllAsync();   // Get full list
        Task<BikeStation?> GetByNumberAsync(int number); // Lookup by station number
        Task<BikeStation?> GetByIdAsync(string id); // Lookup by internal id

        Task AddAsync(BikeStation station); // Add new station
        Task<bool> UpdateAsync(BikeStation station); // Update existing station
        Task ReplaceAllAsync(IEnumerable<BikeStation> stations); // Replace all stations
    }
}
