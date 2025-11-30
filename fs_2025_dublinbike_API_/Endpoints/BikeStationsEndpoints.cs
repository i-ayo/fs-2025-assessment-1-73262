using AutoMapper;
using fs_2025_dublinbike_API_.Models;
using fs_2025_dublinbike_API_.Repositories;
using fs_2025_dublinbike_API_.Services;
using Microsoft.Extensions.Caching.Memory;

namespace fs_2025_dublinbike_API_.Endpoints
{
    // Defines all API routes for both V1 (file repo) and V2 (cosmos repo)
    public static class BikeStationsEndpoints
    {
        public static void MapBikeStations(this WebApplication app)
        {
            var api = app.MapGroup("/api");

            // --------------------
            // V1 ENDPOINTS (File repo)
            // --------------------

            var v1 = api.MapGroup("/v1");

            // V1 uses FileJsonBikeStationRepository
            // Query stations with filtering, sorting & paging
            v1.MapGet("/stations", async (
                FileJsonBikeStationRepository repo,
                IMemoryCache cache,
                ILogger<BikeStationService> logger,
                IMapper mapper,
                string? status, int? minBikes, string? q, string? sort, string? dir, int? page, int? pageSize) =>
            {
                var svc = new BikeStationService(repo, cache, logger, mapper);
                var qp = new BikeStationService.QueryParams
                {
                    Status = status,
                    MinBikes = minBikes,
                    Q = q,
                    Sort = sort,
                    Dir = dir,
                    Page = page ?? 1,
                    PageSize = pageSize ?? 20
                };
                var (items, total) = await svc.QueryAsync(qp);
                return Results.Ok(new { total, items });
            });

            // Get a single station by number
            v1.MapGet("/stations/{number:int}", async (
                FileJsonBikeStationRepository repo,
                IMemoryCache cache,
                ILogger<BikeStationService> logger,
                IMapper mapper,
                int number) =>
            {
                var svc = new BikeStationService(repo, cache, logger, mapper);
                var dto = await svc.GetByNumberAsync(number);
                return dto is not null ? Results.Ok(dto) : Results.NotFound();
            });

            // Summary aggregates
            v1.MapGet("/stations/summary", async (
                FileJsonBikeStationRepository repo,
                IMemoryCache cache,
                ILogger<BikeStationService> logger,
                IMapper mapper) =>
            {
                var svc = new BikeStationService(repo, cache, logger, mapper);
                var summary = await svc.GetSummaryAsync();
                return Results.Ok(summary);
            });

            // Add a new station
            v1.MapPost("/stations", async (
                FileJsonBikeStationRepository repo,
                IMemoryCache cache,
                ILogger<BikeStationService> logger,
                IMapper mapper,
                BikeStation station) =>
            {
                // Validate station number is unique
                var svc = new BikeStationService(repo, cache, logger, mapper);
                await svc.AddAsync(station);
                return Results.Created($"/api/v1/stations/{station.number}", station);
            });

            // Update an existing station
            v1.MapPut("/stations/{number:int}", async (
                FileJsonBikeStationRepository repo,
                IMemoryCache cache,
                ILogger<BikeStationService> logger,
                IMapper mapper,
                int number, BikeStation station) =>
            {
                // Validate URL number matches station.number
                if (number != station.number) return Results.BadRequest(new { message = "URL number must match station.number" });
                var svc = new BikeStationService(repo, cache, logger, mapper);
                var ok = await svc.UpdateAsync(station);
                return ok ? Results.NoContent() : Results.NotFound();
            });

            // --------------------
            // V2 ENDPOINTS (Simulated Cosmos repo)
            // --------------------
            var v2 = api.MapGroup("/v2");

            // V2 uses SimulatedCosmosBikeStationRepository
            // Query stations with filtering, sorting & paging
            // (Note: identical to V1 except for repo type)
            v2.MapGet("/stations", async (
                SimulatedCosmosBikeStationRepository repo,
                IMemoryCache cache,
                ILogger<BikeStationService> logger,
                IMapper mapper,
                string? status, int? minBikes, string? q, string? sort, string? dir, int? page, int? pageSize) =>
            {
                var svc = new BikeStationService(repo, cache, logger, mapper);
                var qp = new BikeStationService.QueryParams
                {
                    Status = status,
                    MinBikes = minBikes,
                    Q = q,
                    Sort = sort,
                    Dir = dir,
                    Page = page ?? 1,
                    PageSize = pageSize ?? 20
                };
                var (items, total) = await svc.QueryAsync(qp);
                return Results.Ok(new { total, items });
            });

            // Get a single station by number
            v2.MapGet("/stations/{number:int}", async (
                SimulatedCosmosBikeStationRepository repo,
                IMemoryCache cache,
                ILogger<BikeStationService> logger,
                IMapper mapper,
                int number) =>
            {
                var svc = new BikeStationService(repo, cache, logger, mapper);
                var dto = await svc.GetByNumberAsync(number);
                return dto is not null ? Results.Ok(dto) : Results.NotFound();
            });

            // Summary aggregates
            v2.MapGet("/stations/summary", async (
                SimulatedCosmosBikeStationRepository repo,
                IMemoryCache cache,
                ILogger<BikeStationService> logger,
                IMapper mapper) =>
            {
                var svc = new BikeStationService(repo, cache, logger, mapper);
                var summary = await svc.GetSummaryAsync();
                return Results.Ok(summary);
            });

            // Add a new station
            v2.MapPost("/stations", async (
                SimulatedCosmosBikeStationRepository repo,
                IMemoryCache cache,
                ILogger<BikeStationService> logger,
                IMapper mapper,
                BikeStation station) =>
            {
                // Validate station does not already exist
                var svc = new BikeStationService(repo, cache, logger, mapper);
                await svc.AddAsync(station);
                return Results.Created($"/api/v2/stations/{station.number}", station);
            });

            // Update an existing station
            v2.MapPut("/stations/{number:int}", async (
                SimulatedCosmosBikeStationRepository repo,
                IMemoryCache cache,
                ILogger<BikeStationService> logger,
                IMapper mapper,
                int number, BikeStation station) =>
            {
                // Validate URL number matches station.number
                if (number != station.number) return Results.BadRequest(new { message = "URL number must match station.number" });
                var svc = new BikeStationService(repo, cache, logger, mapper);
                var ok = await svc.UpdateAsync(station);
                return ok ? Results.NoContent() : Results.NotFound();
            });
        }
    }
}
