using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StorhaugenEats.API.Data;
using StorhaugenEats.API.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add configuration
builder.Configuration.AddEnvironmentVariables();
builder.Configuration.AddJsonFile("appsettings.json", optional: false);
builder.Configuration.AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Database Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Supabase Client
builder.Services.AddSingleton(sp =>
{
    var url = builder.Configuration["Supabase:Url"] ?? throw new InvalidOperationException("Supabase URL not configured");
    var key = builder.Configuration["Supabase:ServiceRoleKey"] ?? throw new InvalidOperationException("Supabase Service Role Key not configured");

    var options = new Supabase.SupabaseOptions
    {
        AutoConnectRealtime = false // We don't need realtime in the API
    };

    return new Supabase.Client(url, key, options);
});

// Application Services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IHouseholdService, HouseholdService>();
builder.Services.AddScoped<IGlobalRecipeService, GlobalRecipeService>();
builder.Services.AddScoped<IHouseholdRecipeService, HouseholdRecipeService>();
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<IStorageService, SupabaseStorageService>();
builder.Services.AddScoped<IHelloFreshScraperService, HelloFreshScraperService>();
// HTTP Client for scraper
builder.Services.AddHttpClient<IHelloFreshScraperService, HelloFreshScraperService>();

// JWT Authentication (Supabase JWT)
var jwtSecret = builder.Configuration["Supabase:JwtSecret"] ?? throw new InvalidOperationException("JWT Secret not configured");
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Supabase:Url"],
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// CORS (for Blazor WASM)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorWasm", policy =>
    {
        policy.WithOrigins(
            "https://localhost:7000", // Development
            "https://yourdomain.com" // Production - UPDATE THIS
        )
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowBlazorWasm");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// Health check endpoint
app.MapGet("/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));

// Database connection test endpoint
app.MapGet("/test-connections", async (IConfiguration configuration) =>
{
    await StorhaugenEats.API.ConnectionTester.TestAllConnectionsAsync(configuration);
    return Results.Ok(new { message = "Check console output for connection test results" });
});

app.Run();
