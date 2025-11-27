using AutoMapper;
using fs_2025_dublinbike_api.Models;
using fs_2025_dublinbike_api.Repositories;
using fs_2025_dublinbike_api.Services;
using Microsoft.Extensions.Caching.Memory;

namespace fs_2025_dublinbike_api.Endpoints
{
    // Minimal API endpoints for both V1 (file) and V2 (cosmos).
    public static class BikeStationsEndpoints
    {
        // Register endpoints for both V1 and V2
        public static void MapBikeStations(this WebApplication app)
        {
            var api = app.MapGroup("/api");

            // V1 - file repo
            var v1 = api.MapGroup("/v1");
            v1.MapGet("/stations", async (
                FileJsonBikeStationRepository repo, 
                IMemoryCache cache, 
                ILogger<BikeStationService> logger, 
                IMapper mapper,                    
                string? status, int? minBikes, string? q, string? sort, string? dir, int? page, int? pageSize) =>
            {
                // create service instance wired to File repo
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

            v1.MapPost("/stations", async (
                FileJsonBikeStationRepository repo, 
                IMemoryCache cache, 
                ILogger<BikeStationService> logger, 
                BikeStation station,
                IMapper mapper) =>
            {
                var svc = new BikeStationService(repo, cache, logger, mapper);
                await svc.AddAsync(station);
                return Results.Created($"/api/v1/stations/{station.number}", station);
            });

            v1.MapPut("/stations/{number:int}", async (
                FileJsonBikeStationRepository repo, 
                IMemoryCache cache, 
                ILogger<BikeStationService> logger,
                IMapper mapper,
                int number, BikeStation station) =>
            {
                if (number != station.number) return Results.BadRequest(new { message = "URL number must match station.number" });
                var svc = new BikeStationService(repo, cache, logger, mapper);
                var ok = await svc.UpdateAsync(station);
                return ok ? Results.NoContent() : Results.NotFound();
            });

            // V2 - cosmos repo
            var v2 = api.MapGroup("/v2");
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

            v2.MapGet("/stations/summary", async (
                SimulatedCosmosBikeStationRepository repo, 
                IMemoryCache cache, 
                ILogger<BikeStationService> logger,
                IMapper mapper
                ) =>
            {
                var svc = new BikeStationService(repo, cache, logger, mapper);
                var summary = await svc.GetSummaryAsync();
                return Results.Ok(summary);
            });

            v2.MapPost("/stations", async (
                SimulatedCosmosBikeStationRepository repo, 
                IMemoryCache cache, 
                ILogger<BikeStationService> logger,
                IMapper mapper,
                BikeStation station) =>
            {
                var svc = new BikeStationService(repo, cache, logger, mapper);
                await svc.AddAsync(station);
                return Results.Created($"/api/v2/stations/{station.number}", station);
            });

            v2.MapPut("/stations/{number:int}", async (
                SimulatedCosmosBikeStationRepository repo, 
                IMemoryCache cache, 
                ILogger<BikeStationService> logger,
                IMapper mapper,

                int number, BikeStation station) =>
            {
                //if (number != station.number) return Results.BadRequest(new { message = "URL number must match station.number" });
                //var svc = new BikeStationService(repo, cache, logger, mapper);
                //var ok = await svc.UpdateAsync(station);
                //return ok ? Results.NoContent() : Results.NotFound();

                Console.WriteLine($"GET /api/v2/stations/{number}");
                var svc = new BikeStationService(repo, cache, logger, mapper);
                var dto = await svc.GetByNumberAsync(number);
                return dto is not null ? Results.Ok(dto) : Results.NotFound();
            });
        }
    }
}

