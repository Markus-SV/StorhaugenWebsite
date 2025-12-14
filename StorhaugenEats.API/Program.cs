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

// HTTP Context Accessor (needed for CurrentUserService)
builder.Services.AddHttpContextAccessor();

// Application Services
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IHouseholdService, HouseholdService>();
builder.Services.AddScoped<IGlobalRecipeService, GlobalRecipeService>();
builder.Services.AddScoped<IRatingService, RatingService>();
builder.Services.AddScoped<IStorageService, SupabaseStorageService>();
builder.Services.AddScoped<IHelloFreshScraperService, HelloFreshScraperService>();
// HTTP Client for scraper
builder.Services.AddHttpClient<IHelloFreshScraperService, HelloFreshScraperService>();

// New User-Centric Services
builder.Services.AddScoped<IUserFriendshipService, UserFriendshipService>();
builder.Services.AddScoped<IActivityFeedService, ActivityFeedService>();
builder.Services.AddScoped<IUserRecipeService, UserRecipeService>();
builder.Services.AddScoped<ITagService, TagService>();
builder.Services.AddScoped<ICollectionService, CollectionService>();

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
        ValidIssuer = $"{builder.Configuration["Supabase:Url"]}/auth/v1",
        ValidateAudience = false,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        NameClaimType = "email", // Map email claim to Name for easier access
        RoleClaimType = "role"
    };

    // Enable detailed logging for JWT validation failures (development only)
    options.Events = new JwtBearerEvents
    {
        OnAuthenticationFailed = context =>
        {
            if (builder.Environment.IsDevelopment())
            {
                Console.WriteLine($"JWT Authentication failed: {context.Exception.Message}");
            }
            return Task.CompletedTask;
        },
        OnTokenValidated = context =>
        {
            if (builder.Environment.IsDevelopment())
            {
                var email = context.Principal?.FindFirst("email")?.Value;
                Console.WriteLine($"JWT Token validated for user: {email}");
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// CORS (for Blazor WASM)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowBlazorWasm", policy =>
    {
        var allowedOrigins = new List<string>
        {
            // Development - Frontend
            "https://localhost:7280",  // Frontend HTTPS
            "http://localhost:5055",   // Frontend HTTP
            "https://localhost:7000",
            "https://localhost:7001",
            "https://localhost:5001",
            "http://localhost:5000",
            "https://127.0.0.1:7280",
            "https://127.0.0.1:7000",
            "https://127.0.0.1:7001",

            // Production (GitHub Pages)
            "https://markus-sv.github.io"
        };

        // Add additional production URL from configuration if specified
        var productionUrl = builder.Configuration["Frontend:ProductionUrl"];
        if (!string.IsNullOrEmpty(productionUrl))
        {
            allowedOrigins.Add(productionUrl);
        }

        policy.WithOrigins(allowedOrigins.ToArray())
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

var app = builder.Build();

// Run database migrations on startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        context.Database.Migrate();
        Console.WriteLine("✅ Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while migrating the database");
        // Don't fail startup, just log the error
    }
}

// Configure the HTTP request pipeline
// Enable Swagger in all environments for easier testing
app.UseSwagger();
app.UseSwaggerUI();

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
