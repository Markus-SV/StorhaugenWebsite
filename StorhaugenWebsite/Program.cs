using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using StorhaugenWebsite;
using StorhaugenWebsite.Services;
using Supabase;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Load configuration
var http = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
var config = await http.GetFromJsonAsync<Dictionary<string, Dictionary<string, string>>>("appsettings.json");

var supabaseUrl = config?["Supabase"]?["Url"] ?? throw new InvalidOperationException("Supabase URL not configured");
var supabaseKey = config?["Supabase"]?["AnonKey"] ?? throw new InvalidOperationException("Supabase AnonKey not configured");

// Configure Supabase
var supabaseOptions = new SupabaseOptions
{
    AutoRefreshToken = true,
    AutoConnectRealtime = false
};

builder.Services.AddScoped(sp => new Client(supabaseUrl, supabaseKey, supabaseOptions));

// HttpClient for API calls
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// MudBlazor
builder.Services.AddMudServices();

// Custom Services
builder.Services.AddScoped<IFoodService, FoodService>();
builder.Services.AddScoped<IAuthService, SupabaseAuthService>();
builder.Services.AddScoped<IApiClient, ApiClient>();
builder.Services.AddScoped<IDeviceStateService, DeviceStateService>();
builder.Services.AddScoped<IThemeService, ThemeService>();
builder.Services.AddScoped<IOcrService, TesseractOcrService>();

await builder.Build().RunAsync();