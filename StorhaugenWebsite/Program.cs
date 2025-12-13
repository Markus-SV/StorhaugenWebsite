using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using StorhaugenWebsite;
using StorhaugenWebsite.Services;
using StorhaugenWebsite.ApiClient;
using Supabase;
using Microsoft.AspNetCore.Components.Authorization;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// API HttpClient - Configure base URL for API calls
#if DEBUG
var apiBaseUrl = "https://localhost:64797"; // Local API for development
#else
var apiBaseUrl = "https://storhaugen-eats-api-a7ckh4hwdvcagcb7.westeurope-01.azurewebsites.net"; // Azure API for production
#endif

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(apiBaseUrl) });

// MudBlazor
builder.Services.AddMudServices();

// Supabase Client
var supabaseUrl = "https://ithuvxvsoozmvdicxedx.supabase.co";
var supabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Iml0aHV2eHZzb296bXZkaWN4ZWR4Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjQ5NjA1NzIsImV4cCI6MjA4MDUzNjU3Mn0._CnQGm26PbG_8HxoLy5m1lQIfFT6P1RNhLNnbQ4DDy0";

var options = new SupabaseOptions
{
    AutoConnectRealtime = false // We don't need realtime in the frontend
};

builder.Services.AddScoped(sp => new Supabase.Client(supabaseUrl, supabaseAnonKey, options));

// Authentication & API Services
builder.Services.AddScoped<IAuthService, SupabaseAuthService>();
builder.Services.AddScoped<IApiClient, ApiClient>();
builder.Services.AddScoped<IHouseholdStateService, HouseholdStateService>();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, SupabaseAuthStateProvider>();
// Other Services
builder.Services.AddScoped<IDeviceStateService, DeviceStateService>();
builder.Services.AddScoped<IThemeService, ThemeService>();
builder.Services.AddScoped<IOcrService, TesseractOcrService>();

// Food Service - Uses ApiClient now for backward compatibility with existing pages
builder.Services.AddScoped<IFoodService, FoodService>();

// User-centric services (new architecture)
builder.Services.AddScoped<IUserRecipeService, UserRecipeService>();
builder.Services.AddScoped<IUserFriendshipService, UserFriendshipService>();
builder.Services.AddScoped<IActivityFeedService, ActivityFeedService>();

// HelloFresh sync service (triggers background sync on login)
builder.Services.AddScoped<IHelloFreshSyncService, HelloFreshSyncService>();

var host = builder.Build();
var authService = host.Services.GetRequiredService<IAuthService>();
await authService.InitializeAsync();

// Initialize household state (will auto-load after auth)
var householdStateService = host.Services.GetRequiredService<IHouseholdStateService>();
await householdStateService.InitializeAsync();

await host.RunAsync();
