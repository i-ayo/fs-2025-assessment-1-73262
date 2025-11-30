using fs_2025_dublinbike_API_.Models;
using Microsoft.Azure.Cosmos;
using System.Net;
using System.Text.Json;

namespace fs_2025_dublinbike_API_.Repositories
{
    // Simulated Cosmos DB repository for BikeStation
    public class SimulatedCosmosBikeStationRepository : IBikeStationRepository, IAsyncDisposable
    {
        // Dependencies
        private readonly CosmosClient _client;
        private Container _container;
        private readonly string _databaseId;
        private readonly string _containerId;
        private readonly IConfiguration _config;

        // Constructor
        public SimulatedCosmosBikeStationRepository(IConfiguration configuration)
        {
            // Read config
            _config = configuration;
            var conn = configuration.GetValue<string>("Cosmos:ConnectionString");
            _databaseId = configuration.GetValue<string>("Cosmos:DatabaseId", "DublinBikesDb");
            _containerId = configuration.GetValue<string>("Cosmos:ContainerId", "BikeStations");

            // Validate connection string
            if (string.IsNullOrWhiteSpace(conn))
                throw new InvalidOperationException("Cosmos connection string not configured (Cosmos:ConnectionString).");

            _client = new CosmosClient(conn, new CosmosClientOptions
            {
                AllowBulkExecution = false,
                RequestTimeout = TimeSpan.FromSeconds(10)
            });
        }

        // Init & seed (call and await before app.Run)
        // Creates DB/container if not exists, seeds data if empty
        public async Task InitializeAsync()
        {
            var dbResp = await _client.CreateDatabaseIfNotExistsAsync(_databaseId);
            // Use string id as partition key to avoid type mismatches
            var containerResp = await dbResp.Database.CreateContainerIfNotExistsAsync(new ContainerProperties
            {
                Id = _containerId,
                PartitionKeyPath = "/id"
            });
            _container = containerResp.Container;

            // Check if container empty
            var iter = _container.GetItemQueryIterator<BikeStation>("SELECT TOP 1 c.id FROM c");
            if (iter.HasMoreResults)
            {
                var resp = await iter.ReadNextAsync();
                if (resp.Count > 0) return; // has data
            }

            // Seed from Data/dublinbike_cosmos.json if available
            var baseDir = AppContext.BaseDirectory;
            var file = Path.Combine(baseDir, "Data", "dublinbike_cosmos.json");
            if (!File.Exists(file)) return;
            var json = await File.ReadAllTextAsync(file);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var items = JsonSerializer.Deserialize<List<BikeStation>>(json, opts) ?? new List<BikeStation>();

            // Insert each item
            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.id)) item.id = item.number.ToString();
                // Partition is /id (string)
                try
                {
                    await _container.CreateItemAsync(item, new PartitionKey(item.id));
                }
                catch (CosmosException cex) when (cex.StatusCode == HttpStatusCode.Conflict)
                {
                    // ignore duplicates
                }
            }
        }

        // Add new station
        public async Task AddAsync(BikeStation station)
        {
            if (string.IsNullOrWhiteSpace(station.id)) station.id = station.number.ToString();
            await _container.CreateItemAsync(station, new PartitionKey(station.id));
        }

        // Get by number
        public async Task<BikeStation?> GetByNumberAsync(int number)
        {
            // Query by number
            // Note: number is not the partition key, so we have to query
            var query = _container.GetItemQueryIterator<BikeStation>(
                new QueryDefinition("SELECT * FROM c WHERE c.number = @num").WithParameter("@num", number));
            var list = new List<BikeStation>();

            // Read results
            while (query.HasMoreResults)
            {
                var res = await query.ReadNextAsync();
                list.AddRange(res.Resource);
            }
            return list.FirstOrDefault();
        }

        // Get by id
        public async Task<BikeStation?> GetByIdAsync(string id)
        {
            // Read item
            try
            {
                var resp = await _container.ReadItemAsync<BikeStation>(id, new PartitionKey(id));
                return resp.Resource;
            }
            // Not found
            catch (CosmosException cex) when (cex.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }
        }
        // Get all stations
        public async Task<List<BikeStation>> GetAllAsync()
        {
            // Query all
            var query = _container.GetItemQueryIterator<BikeStation>("SELECT * FROM c");
            var list = new List<BikeStation>();
            while (query.HasMoreResults)
            {
                var res = await query.ReadNextAsync();
                list.AddRange(res.Resource);
            }
            return list;
        }

        // Replace all stations
        public async Task ReplaceAllAsync(IEnumerable<BikeStation> stations)
        {
            // upsert each one (keeps id)
            foreach (var s in stations)
            {
                if (string.IsNullOrWhiteSpace(s.id)) s.id = s.number.ToString();
                await _container.UpsertItemAsync(s, new PartitionKey(s.id));
            }
        }

        // Update existing station
        public async Task<bool> UpdateAsync(BikeStation station)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(station.id)) station.id = station.number.ToString();
                var resp = await _container.ReplaceItemAsync(station, station.id, new PartitionKey(station.id));
                return resp.StatusCode == HttpStatusCode.OK || resp.StatusCode == HttpStatusCode.Created;
            }
            catch (CosmosException cex) when (cex.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        // Dispose
        public async ValueTask DisposeAsync()
        {
            // Dispose Cosmos client
            _client.Dispose();
            await Task.CompletedTask;
        }
    }
}
