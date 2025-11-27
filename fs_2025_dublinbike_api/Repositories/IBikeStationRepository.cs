using fs_2025_dublinbike_api.Models;

namespace fs_2025_dublinbike_api.Repositories
{
    // Repository contract used by service and hosted background updater.
    public interface IBikeStationRepository
    {
        // Return all stations (mutable collection is allowed for repository internal handling)
        Task<List<BikeStation>> GetAllAsync();

        // Get a single station by number, or null if not found
        Task<BikeStation?> GetByNumberAsync(int number);

        // Add new station
        Task AddAsync(BikeStation station);

        // Update existing station (replace the object with same number)
        Task<bool> UpdateAsync(BikeStation station);

        // Replace all stations in the repository (used by background updater)
        Task ReplaceAllAsync(IEnumerable<BikeStation> stations);

    }
}
