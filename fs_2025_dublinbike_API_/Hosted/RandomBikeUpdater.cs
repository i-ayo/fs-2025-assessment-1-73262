using fs_2025_dublinbike_API_.Models;
using fs_2025_dublinbike_API_.Repositories;

namespace fs_2025_dublinbike_API_.Hosted
{
    // Background task that periodically randomizes station availability (simulated live data)
    public class RandomBikeUpdater : BackgroundService
    {
        // Dependencies
        private readonly IBikeStationRepository _repo;
        private readonly ILogger<RandomBikeUpdater> _logger;
        private readonly Random _rng = new();
        private readonly int _intervalMs;

        // Constructor
        public RandomBikeUpdater(IBikeStationRepository repo, ILogger<RandomBikeUpdater> logger, IConfiguration config)
        {
            _repo = repo;
            _logger = logger;
            // Update interval (ms) from config or default
            _intervalMs = config.GetValue<int?>("Updater:IntervalMs") ?? 12000;
        }

        // Main execution loop
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Log start
            _logger.LogInformation("RandomBikeUpdater started, interval {ms}ms", _intervalMs);

            // Loop until cancellation requested
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Get current dataset
                    var all = await _repo.GetAllAsync();

                    // Produce updated list with random availability
                    var updated = all.Select(s =>
                    {
                        var newCapacity = Math.Max(1, s.bike_stands + _rng.Next(-3, 4));
                        var availableBikes = _rng.Next(0, newCapacity + 1);
                        var availableStands = Math.Max(0, newCapacity - availableBikes);

                        // Return updated station
                        return new BikeStation
                        {
                            id = s.id,
                            partitionKey = s.partitionKey,
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
                            last_update_utc = DateTimeOffset.UtcNow,
                            last_update_local = DateTimeOffset.UtcNow.ToLocalTime()
                        };
                    }).ToList();

                    // Save updated dataset
                    await _repo.ReplaceAllAsync(updated);
                    _logger.LogDebug("RandomBikeUpdater updated {count} stations", updated.Count);

                }
                // Error handling
                catch (Exception ex)
                {
                    _logger.LogError(ex, "RandomBikeUpdater error");
                }

                // Wait until next update
                await Task.Delay(_intervalMs, stoppingToken);
            }
        }
    }
}
