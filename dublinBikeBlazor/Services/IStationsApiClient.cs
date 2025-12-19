using dublinBikeBlazor.DTO;

namespace dublinBikeBlazor.Services
{
    public interface IStationsApiClient
    {
        Task<PagedResult<BikeStationDto>> GetStationsAsync(
            string? search = null,
            string? status = null,
            int? minBikes = null,
            int page = 1,
            int pageSize = 20,
            string? sort = null,
            string? dir = null);

        Task<BikeStationDto?> GetStationAsync(string id);
        Task<bool> CreateStationAsync(BikeStationDto station);
        Task<bool> UpdateStationAsync(BikeStationDto station);
        Task<bool> DeleteStationAsync(string id);
    }
}