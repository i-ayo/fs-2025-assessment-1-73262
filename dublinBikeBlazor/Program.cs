using dublinBikeBlazor.Components;
using dublinBikeBlazor.Services;


var builder = WebApplication.CreateBuilder(args);

// Configure CORS 
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorClient", policy =>
    {
        policy.WithOrigins(
            "https://localhost:7257",  // Blazor client port
            "https://localhost:5001"   // API port
        )
        .AllowAnyMethod()
        .AllowAnyHeader();
    });
});

// Add Razor components
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Register Stations API client with HttpClient and BaseAddress
builder.Services.AddHttpClient<IStationsApiClient, StationsApiClient>(client =>
{
    client.BaseAddress = new Uri("https://localhost:5001/");
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.UseCors("AllowBlazorClient");
app.UseAuthorization();

app.Run();