using fs_2025_dublinbike_api.Models;
using Microsoft.Azure.Cosmos;
using System.Net;
using System.Text.Json;


namespace fs_2025_dublinbike_api.Repositories
{
    // Simulated Cosmos DB repository for V2:
    // - Reads from Data/dublinbike_cosmos.json at startup (so V2 can be tested locally).
    // - Comments show how to replace this with a real Cosmos DB SDK implementation.

    // Cosmos DB repository for V2.Works with Azure Cosmos DB or the Cosmos DB emulator.
    // For local testing, if container is empty, it will seed data from Data/dublinbike_cosmos.json (if present).
    public class SimulatedCosmosBikeStationRepository : IBikeStationRepository, IAsyncDisposable
    {
        private readonly CosmosClient _client;
        private Container _container;
        private readonly string _databaseId;
        private readonly string _containerId;

        public SimulatedCosmosBikeStationRepository(IConfiguration configuration)
        {
            var conn = configuration.GetValue<string>("Cosmos:ConnectionString");
            _databaseId = configuration.GetValue<string>("Cosmos:DatabaseId", "DublinBikesDb");
            _containerId = configuration.GetValue<string>("Cosmos:ContainerId", "BikeStations");

            if (string.IsNullOrWhiteSpace(conn))
                throw new InvalidOperationException("Cosmos connection string not configured (Cosmos:ConnectionString).");

            _client = new CosmosClient(conn, new CosmosClientOptions
            {
                AllowBulkExecution = false,
                RequestTimeout = TimeSpan.FromSeconds(5)
            });
        }

        public async Task InitializeAsync() 
        {
            // Ensure DB + container exist
            var dbResponse = await _client.CreateDatabaseIfNotExistsAsync(_databaseId);
            var db = dbResponse.Database;
            var containerResponse = await db.CreateContainerIfNotExistsAsync(
                new ContainerProperties(_containerId, "/number"));
            _container = containerResponse.Container;

            // Seed if empty
            await SeedIfEmpty();

        }

        private async Task SeedIfEmpty()
        {
            var iterator = _container.GetItemQueryIterator<BikeStation>("SELECT TOP 1 * FROM c");
            if (iterator.HasMoreResults)
            {
                var resp = await iterator.ReadNextAsync();
                if (resp.Count > 0) return; // already has data
            }

            var baseDir = AppContext.BaseDirectory;
            var file = Path.Combine(baseDir, "Data", "dublinbike_cosmos.json");
            if (!File.Exists(file)) return;

            var json = File.ReadAllText(file);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            var items = JsonSerializer.Deserialize<List<BikeStation>>(json, opts) ?? new List<BikeStation>();

            foreach (var item in items)
            {
                try
                {
                    item.id = item.number.ToString();
                    await _container.CreateItemAsync(item, new PartitionKey(item.number));
                }
                catch
                {
                    // ignore duplicates
                }
            }
        }

        public async Task AddAsync(BikeStation station)
        {
            station.id = station.number.ToString();
            await _container.CreateItemAsync(station, new PartitionKey(station.number));
        }

        public async Task<BikeStation?> GetByNumberAsync(int number)
        {
            Console.WriteLine($"🔍 Reading station {number}...");

            try
            {
                var pk = new PartitionKey(number.ToString()); // 🔧 force string
                var response = await _container.ReadItemAsync<BikeStation>(
                    number.ToString(), pk);

                Console.WriteLine($"✅ Found station: {response.Resource.name}");
                return response.Resource;
            }
            catch (CosmosException cex) when (cex.StatusCode == HttpStatusCode.NotFound)
            {
                Console.WriteLine($"❌ Station {number} not found.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"🔥 Error reading station {number}: {ex.Message}");
                return null;
            }
        }

        public async Task<List<BikeStation>> GetAllAsync()
        {
            Console.WriteLine("📦 Fetching all stations...");

            var list = new List<BikeStation>();
            var query = _container.GetItemQueryIterator<BikeStation>("SELECT * FROM c");

            while (query.HasMoreResults)
            {
                var res = await query.ReadNextAsync();
                Console.WriteLine($"🔍 Retrieved {res.Count} stations...");
                list.AddRange(res.Resource);
            }

            return list;
        }

        public async Task ReplaceAllAsync(IEnumerable<BikeStation> stations)
        {
            foreach (var s in stations)
            {
                s.id = s.number.ToString();
                await _container.UpsertItemAsync(s, new PartitionKey(s.number));
            }
        }

        public async Task<bool> UpdateAsync(BikeStation station)
        {
            try
            {
                station.id = station.number.ToString();
                var resp = await _container.ReplaceItemAsync(
                    station,
                    station.number.ToString(),
                    new PartitionKey(station.number)
                );

                return resp.StatusCode == HttpStatusCode.OK ||
                       resp.StatusCode == HttpStatusCode.Created;
            }
            catch (CosmosException cex) when (cex.StatusCode == HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public async ValueTask DisposeAsync()
        {
            _client.Dispose();
            await Task.CompletedTask;
        }
    }
}