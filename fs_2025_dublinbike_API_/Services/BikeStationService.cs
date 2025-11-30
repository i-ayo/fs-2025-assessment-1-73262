using AutoMapper;
using fs_2025_dublinbike_API_.DTOs;
using fs_2025_dublinbike_API_.Models;
using fs_2025_dublinbike_API_.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Primitives;

namespace fs_2025_dublinbike_API_.Services
{
    // Business logic for bike stations
    public class BikeStationService
    {
        private readonly IBikeStationRepository _repo;
        private readonly IMemoryCache _cache;
        private readonly ILogger<BikeStationService> _logger;
        private readonly IMapper _mapper;
        private static readonly TimeSpan CACHE_DURATION = TimeSpan.FromMinutes(5);

        private const string CACHE_TOKEN_KEY = "stations::cache::token";
        private CancellationTokenSource _cacheTokenSource;

        // Constructor
        public BikeStationService(IBikeStationRepository repo, IMemoryCache cache, ILogger<BikeStationService> logger, IMapper mapper)
        {
            _repo = repo ?? throw new ArgumentNullException(nameof(repo));
            _cache = cache;
            _logger = logger;
            _mapper = mapper;
        

            // Ensure cache token exists
            if (!_cache.TryGetValue(CACHE_TOKEN_KEY, out CancellationTokenSource? token))
            {
                token = new CancellationTokenSource();
                _cache.Set(CACHE_TOKEN_KEY, token);
            }
            _cacheTokenSource = token!;
        }

        // Query parameters for filtering, sorting, and paging
        public class QueryParams
        {
            public string? Status { get; set; }
            public int? MinBikes { get; set; }
            public string? Q { get; set; }
            public string? Sort { get; set; }
            public string? Dir { get; set; }
            public int Page { get; set; } = 1;
            public int PageSize { get; set; } = 20;
        }

        // Query bike stations with filtering, sorting, and paging     
        public async Task<(IEnumerable<BikeStationDto> Items, int TotalCount)> QueryAsync(QueryParams p)
        {
            // Check cache first
            var cacheKey = $"stations::{p.Status}::{p.MinBikes}::{p.Q}::{p.Sort}::{p.Dir}::{p.Page}::{p.PageSize}";
            if (_cache.TryGetValue(cacheKey, out (IEnumerable<BikeStationDto>, int) cached))
                return cached;

            var all = await _repo.GetAllAsync();

            // Filtering
            if (!string.IsNullOrWhiteSpace(p.Status))
                all = all.Where(x => x.status?.Equals(p.Status, StringComparison.OrdinalIgnoreCase) ?? false).ToList();

            if (p.MinBikes.HasValue)
                all = all.Where(x => x.available_bikes >= p.MinBikes.Value).ToList();

            if (!string.IsNullOrWhiteSpace(p.Q))
            {
                var q = p.Q.Trim();
                all = all.Where(x => (x.name?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                                     (x.address?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)).ToList();
            }

            // Mapping to DTOs
            var dtos = _mapper.Map<List<BikeStationDto>>(all);

            var sort = p.Sort?.ToLowerInvariant();
            var asc = string.IsNullOrWhiteSpace(p.Dir) || p.Dir.Equals("asc", StringComparison.OrdinalIgnoreCase);

            // Sorting
            dtos = sort switch
            {
                "name" => asc ? dtos.OrderBy(d => d.Name).ToList() : dtos.OrderByDescending(d => d.Name).ToList(),
                "availablebikes" => asc ? dtos.OrderBy(d => d.AvailableBikes).ToList() : dtos.OrderByDescending(d => d.AvailableBikes).ToList(),
                "occupancy" => asc ? dtos.OrderBy(d => d.Occupancy).ToList() : dtos.OrderByDescending(d => d.Occupancy).ToList(),
                _ => dtos.OrderBy(d => d.Number).ToList()
            };

            // Paging
            var total = dtos.Count;
            var page = Math.Max(1, p.Page);
            var pageSize = Math.Clamp(p.PageSize, 1, 200);
            var items = dtos.Skip((page - 1) * pageSize).Take(pageSize).ToList();

            var result = (Items: (IEnumerable<BikeStationDto>)items, TotalCount: total);

            // Use token for cache invalidation
            // Set cache with expiration token
            var cacheOptions = new MemoryCacheEntryOptions();
            cacheOptions.AddExpirationToken(new CancellationChangeToken(_cacheTokenSource.Token));
            _cache.Set(cacheKey, result, cacheOptions);

            return result;
        }

        // Get bike station by number
        public async Task<BikeStationDto?> GetByNumberAsync(int number)
        {
            var s = await _repo.GetByNumberAsync(number);
            if (s == null) return null;
            return _mapper.Map<BikeStationDto>(s);
        }

        // Get bike station by id
        public async Task<BikeStationDto?> GetByIdAsync(string id)
        {
            var s = await _repo.GetByIdAsync(id);
            if (s == null) return null;
            return _mapper.Map<BikeStationDto>(s);
        }

        // Add new bike station
        public async Task<bool> AddAsync(BikeStation station)
        {   // Validate
            if (station.bike_stands < 0)
                throw new ArgumentException("bike_stands must be >= 0");

            if (string.IsNullOrWhiteSpace(station.id))
                station.id = station.number.ToString();
            // Set timestamps
            station.last_update_utc = DateTimeOffset.UtcNow;
            station.last_update_local = station.last_update_utc.ToLocalTime();
            
            // Add to repository
            await _repo.AddAsync(station);
            InvalidateCache();
            return true;
        }

        // Update existing bike station
        public async Task<bool> UpdateAsync(BikeStation station)
        {
            station.last_update_utc = DateTimeOffset.UtcNow;
            station.last_update_local = station.last_update_utc.ToLocalTime();

            // Update in repository
            var ok = await _repo.UpdateAsync(station);
            InvalidateCache();
            return ok;
        }

        // Get summary statistics
        public async Task<object> GetSummaryAsync()
        {
            // Get all stations
            var all = await _repo.GetAllAsync();
            var totalStations = all.Count;
            var totalBikeStands = all.Sum(s => s.bike_stands);
            var totalAvailableBikes = all.Sum(s => s.available_bikes);
            var countsByStatus = all.GroupBy(s => (s.status ?? "UNKNOWN").ToUpperInvariant()).ToDictionary(g => g.Key, g => g.Count());

            // Return summary object
            return new
            {
                totalStations,
                totalBikeStands,
                totalAvailableBikes,
                countsByStatus
            };
        }

        // Token-based cache invalidation
        // Cancels the current token and creates a new one
        // causing all cache entries using the old token to expire
        public void InvalidateCache()
        {
            // Cancel existing token and create a new one
            if (_cache.TryGetValue(CACHE_TOKEN_KEY, out CancellationTokenSource? token))
            {
                token.Cancel();
                var newToken = new CancellationTokenSource();
                _cache.Set(CACHE_TOKEN_KEY, newToken);
                _cacheTokenSource = newToken;
            }
        }


    }
}
