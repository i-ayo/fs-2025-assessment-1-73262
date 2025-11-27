using AutoMapper;
using fs_2025_dublinbike_api.Endpoints;
using fs_2025_dublinbike_api.Hosted;
using fs_2025_dublinbike_api.Profiles;
using fs_2025_dublinbike_api.Repositories;
using fs_2025_dublinbike_api.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Microsoft.Win32;

var builder = WebApplication.CreateBuilder(args);

// Add configuration and services
builder.Services.AddMemoryCache();
builder.Services.AddLogging();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "DublinBike API", Version = "v1" });
});

//Register AutoMapper
builder.Services.AddAutoMapper(cfg => { }, typeof(BikeStationProfile).Assembly);

// Register repositories
builder.Services.AddSingleton<FileJsonBikeStationRepository>();
builder.Services.AddSingleton<SimulatedCosmosBikeStationRepository>();

// Register MemoryCache as IMemoryCache (already handled by AddMemoryCache, but this is safe)
builder.Services.AddSingleton<IMemoryCache>(sp => sp.GetRequiredService<IMemoryCache>());

// Register hosted background updater for V1 (file repo)
builder.Services.AddHostedService<RandomBikeUpdater>(sp =>
{
    var repo = sp.GetRequiredService<FileJsonBikeStationRepository>() as IBikeStationRepository;
    var logger = sp.GetRequiredService<ILogger<RandomBikeUpdater>>();
    var configuration = sp.GetRequiredService<IConfiguration>();
    return new RandomBikeUpdater(repo!, logger, configuration);
});

var cosmosRepo = new SimulatedCosmosBikeStationRepository(builder.Configuration);
await cosmosRepo.InitializeAsync();
builder.Services.AddSingleton(cosmosRepo);

// Build app
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// Map endpoints
app.MapBikeStations();


// Basic root redirect to swagger
app.MapGet("/", () => Results.Redirect("/swagger"));

app.Run();