using AutoMapper;
using fs_2025_dublinbike_API_.Endpoints;
using fs_2025_dublinbike_API_.Hosted;
using fs_2025_dublinbike_API_.Profiles;
using fs_2025_dublinbike_API_.Repositories;
using fs_2025_dublinbike_API_.Services;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. CORE SERVICES
// ---------------------------------------------------------
//

// Logging + configuration (default)
builder.Services.AddLogging();
builder.Services.AddMemoryCache();

//
// ---------------------------------------------------------
// 2. REPOSITORIES (V1 + V2)
// ---------------------------------------------------------
//

// V1 repository (file-based JSON store)
builder.Services.AddSingleton<IBikeStationRepository, FileJsonBikeStationRepository>();

// Needed because RandomBikeUpdater *requests the concrete type*
builder.Services.AddSingleton<FileJsonBikeStationRepository>();

// V2 repository (in-memory Cosmos simulation)
builder.Services.AddSingleton<SimulatedCosmosBikeStationRepository>();

//
// ---------------------------------------------------------
// 3. SERVICES (Business logic)
// ---------------------------------------------------------
//

// Service used by both V1 and V2 endpoints
builder.Services.AddTransient<BikeStationService>();

//
// ---------------------------------------------------------
// 4. AUTOMAPPER
// ---------------------------------------------------------
//

// Loads all profiles (BikeStationProfile)
builder.Services.AddAutoMapper(typeof(BikeStationProfile).Assembly);

//
// ---------------------------------------------------------
// 5. SWAGGER (API Explorer)
// ---------------------------------------------------------
//

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "DublinBike API",
        Version = "v1"
    });
});

//
// ---------------------------------------------------------
// 6. BACKGROUND SERVICE (V1 only)
// ---------------------------------------------------------
//

// Background service that updates file-repository every X seconds
builder.Services.AddHostedService<RandomBikeUpdater>(sp =>
{
    var repo = sp.GetRequiredService<FileJsonBikeStationRepository>();
    var logger = sp.GetRequiredService<ILogger<RandomBikeUpdater>>();
    var cfg = sp.GetRequiredService<IConfiguration>();
    return new RandomBikeUpdater(repo, logger, cfg);
});

//
// ---------------------------------------------------------
// 7. BUILD THE APP
// ---------------------------------------------------------
//

var app = builder.Build();

//
// ---------------------------------------------------------
// 8. INITIALIZE COSMOS SIMULATION (for V2)
// ---------------------------------------------------------
//

// Ensure Cosmos-like repository has seed data before API starts
using (var scope = app.Services.CreateScope())
{
    var cosmosRepo = scope.ServiceProvider.GetRequiredService<SimulatedCosmosBikeStationRepository>();
    await cosmosRepo.InitializeAsync();
}

//
// ---------------------------------------------------------
// 9. SWAGGER UI (development only)
// ---------------------------------------------------------
//

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "DublinBike API v1");
        c.RoutePrefix = "swagger"; // Swagger UI available at /swagger
    });
}

//
// ---------------------------------------------------------
// 10. BASIC HEALTH CHECK
// ---------------------------------------------------------
//

app.MapGet("/ping", () =>
    Results.Ok(new { status = "alive", now = DateTimeOffset.UtcNow })
);

//
// ---------------------------------------------------------
// 11. ENDPOINTS (V1 + V2)
// ---------------------------------------------------------
//

// Registers all station endpoints (v1 + v2)
app.MapBikeStations();

//
// ---------------------------------------------------------
// 12. RUN
// ---------------------------------------------------------
//

app.Run();

// Required for integration tests
public partial class Program { }
