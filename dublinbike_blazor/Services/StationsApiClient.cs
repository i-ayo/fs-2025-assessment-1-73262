using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using dublinbike_blazor.Models;
namespace dublinbike_blazor.Services
{
    public class StationsApiClient
    {
        private readonly HttpClient _http;
        public StationsApiClient(HttpClient http) => _http = http;

        public async Task<PagedResult<BikeStation>> GetStations(string search, string status, int minBikes, int page)
        {
            // Protect against nulls & encode
            var qs = $"?search={Uri.EscapeDataString(search ?? "")}&status={Uri.EscapeDataString(status ?? "")}&minBikes={minBikes}&page={page}";
            var result = await _http.GetFromJsonAsync<PagedResult<BikeStation>>($"/api/v1/stations{qs}");
            return result ?? new PagedResult<BikeStation> { Page = page };
        }

        public Task<BikeStation?> Get(int number) =>
            _http.GetFromJsonAsync<BikeStation>($"/api/v1/stations/{number}");

        public Task<HttpResponseMessage> Create(BikeStation s) =>
            _http.PostAsJsonAsync("/api/v1/stations", s);

        public Task<HttpResponseMessage> Update(int number, BikeStation s) =>
            _http.PutAsJsonAsync($"/api/v1/stations/{number}", s);
    }
}