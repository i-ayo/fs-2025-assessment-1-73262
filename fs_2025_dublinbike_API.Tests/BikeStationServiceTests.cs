using AutoMapper;
using fs_2025_dublinbike_API_.Models;
using fs_2025_dublinbike_API_.Profiles;
using fs_2025_dublinbike_API_.Repositories;
using fs_2025_dublinbike_API_.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

public class BikeStationServiceTests
{
    // Dependencies
    private readonly IMapper _mapper;
    private readonly IMemoryCache _cache;

    // Constructor - setup AutoMapper and MemoryCache
    public BikeStationServiceTests()
    {
        var config = new MapperConfiguration(cfg =>
        {
            cfg.AddProfile(new BikeStationProfile());
        });

        _mapper = config.CreateMapper();
        _cache = new MemoryCache(new MemoryCacheOptions());
    }

    // ---------------------------
    // FILTERING + SEARCH TEST
    // ---------------------------
    [Fact]
    // Verify that filtering by MinBikes and searching by Q works correctly
    public async Task Query_MinBikesAndSearch_Works()
    {
        var repo = new InMemoryTestRepo();
        var svc = new BikeStationService(repo, _cache, new NullLogger<BikeStationService>(), _mapper);

        var (items, total) = await svc.QueryAsync(new BikeStationService.QueryParams
        {
            MinBikes = 5,
            Q = "Parnell"
        });

        Assert.Equal(1, total);
        Assert.Single(items);
        Assert.Contains("Parnell", items.First().Name, StringComparison.OrdinalIgnoreCase);
    }

    // ---------------------------
    // PAGING TEST
    // ---------------------------
    [Fact]
    // Verify that paging works correctly
    public async Task Query_Paging_Works()
    {
        var repo = new InMemoryTestRepo();
        var svc = new BikeStationService(repo, _cache, new NullLogger<BikeStationService>(), _mapper);

        var (items, total) = await svc.QueryAsync(new BikeStationService.QueryParams
        {
            Page = 2,
            PageSize = 1
        });

        Assert.Equal(2, total);
        Assert.Single(items);
        Assert.Equal(2, items.First().Number);
    }

    // ---------------------------
    // CACHE INVALIDATION TEST
    // ---------------------------
    [Fact]
    // Verify that cache is invalidated after adding a new bike station
    public async Task Cache_Invalidates_After_AddAsync()
    {
        var repo = new InMemoryTestRepo();
        var svc = new BikeStationService(repo, _cache, new NullLogger<BikeStationService>(), _mapper);

        await svc.QueryAsync(new BikeStationService.QueryParams()); // cache created

        await svc.AddAsync(new BikeStation
        {
            id = "3",
            number = 3,
            name = "New Station",
            address = "Nowhere",
            last_update_utc = DateTimeOffset.UtcNow,
            last_update_local = DateTimeOffset.Now
        });

        // cache should now be invalidated, query should reflect new item
        var (items, total) = await svc.QueryAsync(new BikeStationService.QueryParams());

        Assert.Equal(3, total);
    }

    // ---------------------------
    // TIMESTAMP TEST
    // ---------------------------

    // Verify that AddAsync sets last_update_utc and last_update_local correctly
    [Fact]
    public async Task AddAsync_SetsUtcAndLocalTime()
    {
        var repo = new InMemoryTestRepo();
        var svc = new BikeStationService(repo, _cache, new NullLogger<BikeStationService>(), _mapper);

        var station = new BikeStation
        {
            id = "10",
            number = 10,
            name = "Time Test",
            address = "Clock St"
        };

        await svc.AddAsync(station);

        var created = await repo.GetByIdAsync("10");
        Assert.NotNull(created);

        Assert.True(created!.last_update_utc.Offset == TimeSpan.Zero);
        Assert.True(created.last_update_local.Offset != TimeSpan.Zero);
    }

    // ---------------------------
    // IN-MEMORY TEST REPOSITORY
    // ---------------------------
    // Simple in-memory implementation of IBikeStationRepository for testing
    private class InMemoryTestRepo : IBikeStationRepository
    {
        // Pre-seeded bike stations
        private readonly List<BikeStation> _stations = new()
        {
            new BikeStation
            {
                id = "1",
                number = 1,
                name = "Parnell Square",
                address = "Parnell",
                bike_stands = 10,
                available_bikes = 6,
                available_bike_stands = 4,
                status = "OPEN",
                last_update_utc = DateTimeOffset.UtcNow,
                last_update_local = DateTimeOffset.Now
            },
            new BikeStation
            {
                id = "2",
                number = 2,
                name = "Other",
                address = "Else",
                bike_stands = 10,
                available_bikes = 2,
                available_bike_stands = 8,
                status = "OPEN",
                last_update_utc = DateTimeOffset.UtcNow,
                last_update_local = DateTimeOffset.Now
            }
        };
        // Add new bike station
        public Task AddAsync(BikeStation station)
        {
            _stations.Add(station);
            return Task.CompletedTask;
        }

        public Task<List<BikeStation>> GetAllAsync()
            => Task.FromResult(_stations.ToList());

        public Task<BikeStation?> GetByIdAsync(string id)
            => Task.FromResult(_stations.FirstOrDefault(s => s.id == id));

        public Task<BikeStation?> GetByNumberAsync(int number)
            => Task.FromResult(_stations.FirstOrDefault(s => s.number == number));

        // Replace all bike stations
        public Task ReplaceAllAsync(IEnumerable<BikeStation> stations)
        {
            _stations.Clear();
            _stations.AddRange(stations);
            return Task.CompletedTask;
        }

        // Update existing bike station
        public Task<bool> UpdateAsync(BikeStation station)
        {
            var i = _stations.FindIndex(s => s.id == station.id);
            if (i < 0) return Task.FromResult(false);

            _stations[i] = station;
            return Task.FromResult(true);
        }
    }
}
