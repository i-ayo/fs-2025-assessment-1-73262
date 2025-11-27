using fs_2025_dublinbike_api.Models;
using fs_2025_dublinbike_api.Repositories;

namespace fs_2025_dublinbike_api.Hosted
{
    // BackgroundService to simulate a live feed.
    public class RandomBikeUpdater : BackgroundService
    {
        private readonly IBikeStationRepository _repo;
        private readonly ILogger<RandomBikeUpdater> _logger;
        private readonly Random _rng = new();
        private readonly int _intervalMs;

        // intervalSeconds default 12
        public RandomBikeUpdater(IBikeStationRepository repo, ILogger<RandomBikeUpdater> logger, IConfiguration config)
        {
            _repo = repo;
            _logger = logger;
            _intervalMs = config.GetValue<int?>("Updater:IntervalMs") ?? 12000;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RandomBikeUpdater started, interval {ms}ms", _intervalMs);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var all = await _repo.GetAllAsync();

                    var updated = all.Select(s =>
                    {
                        // small wiggle for capacity
                        var newCapacity = Math.Max(1, s.bike_stands + _rng.Next(-3, 4));
                        var availableBikes = _rng.Next(0, newCapacity + 1);
                        var availableStands = Math.Max(0, newCapacity - availableBikes);

                        return new BikeStation
                        {
                            number = s.number,
                            contract_name = s.contract_name,
                            name = s.name,
                            address = s.address,
                            position = s.position,
                            banking = s.banking,
                            bonus = s.bonus,
                            bike_stands = newCapacity,
                            available_bike_stands = availableStands,
                            available_bikes = availableBikes,
                            status = s.status,
                            last_update = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                        };
                    }).ToList();

                    await _repo.ReplaceAllAsync(updated);
                    _logger.LogDebug("RandomBikeUpdater updated {count} stations", updated.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "RandomBikeUpdater error");
                }

                await Task.Delay(_intervalMs, stoppingToken);
            }
        }
    }
}

