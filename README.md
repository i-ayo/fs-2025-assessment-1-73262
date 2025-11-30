# DublinBike API

A minimal REST API for Dublin Bike Stations with multiple data sources, background simulation, and caching.

## Table of Contents

- [Project Overview](#project-overview)
- [Assignment Objectives](#assignment-objectives)
- [Technologies](#technologies)
- [Architecture](#architecture)
- [Setup & Running the Application](#setup--running-the-application)
- [API Endpoints](#api-endpoints)
- [Example Requests & Responses](#example-requests--responses)
- [Testing](#testing)
- [Project Structure](#project-structure)
- [Notes](#notes)
- [Postman Collection](#postman-collection)

## Project Overview

The DublinBike API provides real-time information about Dublin bike stations, including availability and location, with filtering, sorting, and paging support.

It supports two storage backends:

- **V1**: File-based JSON repository (`Data/dublinbike.json`)
- **V2**: Simulated Cosmos DB repository (`Data/dublinbike_cosmos.json`)

The API also includes:

- A background service that randomly updates bike availability to simulate live changes
- In-memory caching with automatic invalidation after updates
- Filtering, sorting, and paging for station listings
- Unit and integration tests to ensure correctness

## Assignment Objectives

### Data Models & DTOs

- `BikeStation` model with `Position` type for latitude/longitude
- `BikeStationDto` for API responses with computed fields like `Occupancy`

### Repository Pattern

- `IBikeStationRepository` interface defines a contract for storage operations
- `FileJsonBikeStationRepository` implements file-based storage
- `SimulatedCosmosBikeStationRepository` implements a simulated Cosmos DB repository

### Business Logic

- `BikeStationService` provides filtering, sorting, paging, caching, add/update, and summary methods

### API Endpoints

- CRUD operations (GET, POST, PUT) for bike stations
- Summary endpoint for aggregate statistics
- Versioned endpoints for V1 (JSON) and V2 (Cosmos simulation)

### Background Processing

- `RandomBikeUpdater` hosted service updates bike stations periodically (~12s interval)

### Caching

- In-memory caching with token-based invalidation after POST, PUT, or background updates

### Testing

- Unit tests (`BikeStationServiceTests`) for service logic
- Integration tests (`BikeStationsEndpointTests`) for API endpoints

## Technologies

- .NET 6 (Minimal API)
- C# 11
- AutoMapper for DTO mapping
- Microsoft.Extensions.Caching.Memory for caching
- Cosmos DB SDK (simulated)
- xUnit for testing
- Swagger for API documentation

## Architecture

```
+--------------------+
|       Program      | <-- Sets up DI, Swagger, Endpoints, Hosted Service
+--------------------+
          |
          v
+--------------------+
|   BikeStations     | <-- API endpoints (V1, V2)
+--------------------+
          |
          v
+--------------------+
| BikeStationService | <-- Business logic: filtering, caching, add/update, summary
+--------------------+
          |
          v
+--------------------+
| Repositories       | <-- FileJson / Simulated Cosmos
+--------------------+
          |
          v
+--------------------+
| Data Source        | <-- JSON file or Cosmos DB
+--------------------+
```

## Setup & Running the Application

### Clone the repository

```bash
git clone https://github.com/yourusername/fs_2025_dublinbike_API_.git
cd fs_2025_dublinbike_API_
```

### Install dependencies

```bash
dotnet restore
```

### Run the API

```bash
dotnet run --project fs_2025_dublinbike_API_
```

### Access Swagger UI

- HTTPS: `https://localhost:5001/swagger`
- HTTP: `http://localhost:5000/swagger`

### Configuration (appsettings.json)

```json
{
  "Updater": {
    "IntervalMs": 12000
  },
  "Cosmos": {
    "ConnectionString": "YourCosmosConnectionString",
    "DatabaseId": "DublinBikesVDb",
    "ContainerId": "BikeStationsV"
  }
}
```

**Note:**
- V1 uses `Data/dublinbike.json`
- V2 uses Cosmos DB or a simulated JSON-based container

### Cosmos DB Setup

To create a new Cosmos DB container:

1. Go to Azure Portal → Cosmos DB account → Data Explorer
2. Create Database `DublinBikesVDb`
3. Create Container `BikeStationsV`
4. Update `ConnectionString` in `appsettings.json`

To remove the old container, simply delete it from Cosmos Data Explorer.

## API Endpoints

### Version 1 (File-based)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v1/stations` | List all stations (filter, sort, page) |
| GET | `/api/v1/stations/{number}` | Get station by number |
| GET | `/api/v1/stations/summary` | Get summary statistics |
| POST | `/api/v1/stations` | Add a new station |
| PUT | `/api/v1/stations/{number}` | Update existing station |

### Version 2 (Simulated Cosmos DB)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/v2/stations` | List all stations (filter, sort, page) |
| GET | `/api/v2/stations/{number}` | Get station by number |
| GET | `/api/v2/stations/summary` | Get summary statistics |
| POST | `/api/v2/stations` | Add a new station |
| PUT | `/api/v2/stations/{number}` | Update existing station |

## Example Requests & Responses

### GET V1 /stations

**Request:**
```http
GET http://localhost:5000/api/v1/stations?status=OPEN&sort=availableBikes&dir=desc
```

**Response:**
```json
{
  "total": 125,
  "items": [
    {
      "id": "42",
      "Number": 42,
      "Name": "Smithfield",
      "Address": "Smithfield, Dublin",
      "Lat": 53.350,
      "Lng": -6.280,
      "BikeStands": 20,
      "AvailableBikeStands": 5,
      "AvailableBikes": 15,
      "Status": "OPEN",
      "LastUpdateUtc": "2025-11-30T12:00:00Z",
      "LastUpdateLocal": "2025-11-30T12:00:00+00:00",
      "Occupancy": 0.75
    }
  ]
}
```

### POST /api/v2/stations

**Request:**
```http
POST http://localhost:5000/api/v2/stations
Content-Type: application/json

{
  "id": "999",
  "partitionKey": "999",
  "number": 999,
  "contract_name": "dublin",
  "name": "TEST STATION COSMOS",
  "address": "123 Test St",
  "position": {
    "lat": 53.345,
    "lng": -6.26
  },
  "banking": true,
  "bonus": true,
  "bike_stands": 10,
  "available_bike_stands": 10,
  "available_bikes": 0,
  "status": "OPEN",
  "last_update": 1700000000
}
```

**Response: 201 Created**
```json
{
  "id": "999",
  "Number": 999,
  "Name": "TEST STATION COSMOS",
  "Address": "123 Test St",
  "Lat": 53.345,
  "Lng": -6.26,
  "BikeStands": 10,
  "AvailableBikes": 0,
  "AvailableBikeStands": 10,
  "Status": "OPEN",
  "LastUpdateUtc": "...",
  "LastUpdateLocal": "...",
  "Occupancy": 0.0
}
```

## Testing

### Unit Tests

`BikeStationServiceTests` — tests filtering, paging, caching, timestamp logic, add/update behavior.

### Integration Tests

`BikeStationsEndpointTests` — tests API endpoints using `WebApplicationFactory<Program>`.

### Run all tests

```bash
dotnet test
```

## Project Structure

```
fs_2025_dublinbike_API_/
├─ Data/
│  ├─ dublinbike.json
│  └─ dublinbike_cosmos.json
├─ Endpoints/
│  └─ BikeStationsEndpoints.cs
├─ Hosted/
│  └─ RandomBikeUpdater.cs
├─ Models/
│  ├─ BikeStation.cs
│  └─ Position.cs
├─ DTOs/
│  └─ BikeStationDto.cs
├─ Profiles/
│  └─ BikeStationProfile.cs
├─ Repositories/
│  ├─ IBikeStationRepository.cs
│  ├─ FileJsonBikeStationRepository.cs
│  └─ SimulatedCosmosBikeStationRepository.cs
├─ Services/
│  └─ BikeStationService.cs
├─ Program.cs
├─ Tests/
│  ├─ BikeStationServiceTests.cs
│  └─ BikeStationsEndpointTests.cs
├─ Postman/
│  └─ DublinBikes.postman_collection.json
└─ README.md
```

## Notes

- Background service `RandomBikeUpdater` simulates real-time bike availability changes
- DTO mapping with AutoMapper ensures clean separation between internal models and API responses
- Caching improves response times for list endpoints, invalidated after updates
- JSON files and Cosmos DB connection are configurable via `appsettings.json`

## Postman Collection

Import the file `Postman/DublinBikes.postman_collection.json` into Postman to test all endpoints for both V1 and V2. Use Collection Runner for batch testing.

---

## License

This project is licensed under the MIT License.

## Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.
