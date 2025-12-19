using dublinBikeBlazor.DTO;
using System.Net.Http.Json;

namespace dublinBikeBlazor.Services
{
    public class StationsApiClient : IStationsApiClient
    {
        private readonly HttpClient _http;

        public StationsApiClient(HttpClient http)
        {
            _http = http;
        }

        // Get paged list of stations with optional filters, sorting, and paging
        public async Task<PagedResult<BikeStationDto>> GetStationsAsync(
            string? search = null,
            string? status = null,
            int? minBikes = null,
            int page = 1,
            int pageSize = 20,
            string? sort = null,
            string? dir = null)
        {
            // BUILD QUERY PARAMETERS
            var queryParams = new List<string>
            {
                $"page={page}",
                $"pageSize={pageSize}"
            };

            if (!string.IsNullOrWhiteSpace(search))
                queryParams.Add($"q={Uri.EscapeDataString(search)}");

            if (!string.IsNullOrWhiteSpace(status))
                queryParams.Add($"status={Uri.EscapeDataString(status)}");

            if (minBikes.HasValue && minBikes > 0)
                queryParams.Add($"minBikes={minBikes}");

            // ADD SORT PARAMETERS
            if (!string.IsNullOrWhiteSpace(sort))
                queryParams.Add($"sort={Uri.EscapeDataString(sort)}");

            if (!string.IsNullOrWhiteSpace(dir))
                queryParams.Add($"dir={Uri.EscapeDataString(dir)}");
            
            var query = string.Join("&", queryParams);
            var url = $"api/v2/stations?{query}";

            Console.WriteLine($"[API Client] Fetching: {url}");

            try
            {
                var response = await _http.GetFromJsonAsync<ApiResponse>(url);

                if (response != null)
                {
                    Console.WriteLine($"[API Client] Received {response.items?.Count ?? 0} stations, Total: {response.total}");
                    return new PagedResult<BikeStationDto>
                    {
                        Items = response.items ?? new List<BikeStationDto>(),
                        TotalCount = response.total
                    };
                }

                return new PagedResult<BikeStationDto>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API Client ERROR] {ex.Message}");
                throw;
            }
        }

        // Get single station by id
        public async Task<BikeStationDto?> GetStationAsync(string id)
        {
            try
            {
                Console.WriteLine($"[API Client] Fetching station: {id}");
                return await _http.GetFromJsonAsync<BikeStationDto>($"api/v2/stations/{id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API Client ERROR] GetStation: {ex.Message}");
                return null;
            }
        }

        // Create new station
        public async Task<bool> CreateStationAsync(BikeStationDto station)
        {
            try
            {
                Console.WriteLine($"[API Client] Creating station: {station.Number}");

                // Convert DTO to API model
                var apiModel = new
                {
                    id = station.id,
                    partitionKey = station.Number.ToString(),
                    number = station.Number,
                    contract_name = station.ContractName,
                    name = station.Name,
                    address = station.Address,
                    position = new { lat = station.Lat, lng = station.Lng },
                    banking = true,
                    bonus = false,
                    bike_stands = station.BikeStands,
                    available_bike_stands = station.AvailableBikeStands,
                    available_bikes = station.AvailableBikes,
                    status = station.Status
                };

                // Send POST request to create
                var response = await _http.PostAsJsonAsync("api/v2/stations", apiModel);
                var success = response.IsSuccessStatusCode;
                Console.WriteLine($"[API Client] Create result: {success}");
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API Client ERROR] CreateStation: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> UpdateStationAsync(BikeStationDto station)
        {
            try
            {
                Console.WriteLine($"[API Client] Updating station: {station.Number}");

                // Convert DTO to API model
                var apiModel = new
                {
                    id = station.id,
                    partitionKey = station.Number.ToString(),
                    number = station.Number,
                    contract_name = station.ContractName,
                    name = station.Name,
                    address = station.Address,
                    position = new { lat = station.Lat, lng = station.Lng },
                    banking = true,
                    bonus = false,
                    bike_stands = station.BikeStands,
                    available_bike_stands = station.AvailableBikeStands,
                    available_bikes = station.AvailableBikes,
                    status = station.Status
                };

                // Send PUT request to update
                var response = await _http.PutAsJsonAsync($"api/v2/stations/{station.Number}", apiModel);
                var success = response.IsSuccessStatusCode;
                Console.WriteLine($"[API Client] Update result: {success}");
                return success;
            }
            catch (Exception ex)
            {
                // Log error
                Console.WriteLine($"[API Client ERROR] UpdateStation: {ex.Message}");
                return false;
            }
        }

        // Delete station by id
        public async Task<bool> DeleteStationAsync(string id)
        {
            try
            {
                Console.WriteLine($"[API Client] Deleting station: {id}");
                var response = await _http.DeleteAsync($"api/v2/stations/{id}");
                var success = response.IsSuccessStatusCode;
                Console.WriteLine($"[API Client] Delete result: {success}");
                return success;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[API Client ERROR] DeleteStation: {ex.Message}");
                return false;
            }
        }

        // Helper class for API response (lowercase property names)
        private class ApiResponse
        {
            public int total { get; set; }
            public List<BikeStationDto>? items { get; set; }
        }
    }
}