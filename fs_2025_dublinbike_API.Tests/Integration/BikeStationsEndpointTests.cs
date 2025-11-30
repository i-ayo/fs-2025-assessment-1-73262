using fs_2025_dublinbike_API_;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Json;

// Integration tests for Bike Stations API endpoint
public class BikeStationsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public BikeStationsEndpointTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    // Verify that GET /api/v1/stations returns 200 OK and valid JSON
    public async Task Get_V1_Stations_Returns200()
    {
        // Act
        // Send GET request to V1 stations endpoint
        var response = await _client.GetAsync("/api/v1/stations");

        // Assert
        // Verify status code
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify JSON content
        var json = await response.Content.ReadFromJsonAsync<dynamic>();
        Assert.NotNull(json);
    }
}
