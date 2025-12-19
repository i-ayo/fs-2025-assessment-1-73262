using dublinBikeBlazor.DTO;

namespace dublinBikeBlazor.Services
{
    // Interface for Stations API client
    public interface IStationsApiClient
    {
        // Get paged list of stations with optional filters, sorting, and paging
        Task<PagedResult<BikeStationDto>> GetStationsAsync(
            string? search = null,
            string? status = null,
            int? minBikes = null,
            int page = 1,
            int pageSize = 20,
            string? sort = null,
            string? dir = null);
        
        Task<BikeStationDto?> GetStationAsync(string id); // Get station by ID
        Task<bool> CreateStationAsync(BikeStationDto station); // Create new station
        Task<bool> UpdateStationAsync(BikeStationDto station); // Update existing station
        Task<bool> DeleteStationAsync(string id); // Delete station by ID
    }
}