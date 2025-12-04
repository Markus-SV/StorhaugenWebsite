using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using StorhaugenWebsite;
using StorhaugenWebsite.Brokers;
using StorhaugenWebsite.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// MudBlazor
builder.Services.AddMudServices();

// Custom Services
builder.Services.AddScoped<IFirebaseBroker, FirebaseBroker>();
builder.Services.AddScoped<IFoodService, FoodService>();
builder.Services.AddScoped<IAuthService, AuthService>();

await builder.Build().RunAsync();