using HtmlAgilityPack;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using StorhaugenEats.API.Data;
using StorhaugenEats.API.Models;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace StorhaugenEats.API.Services;

public class HelloFreshScraperService : IHelloFreshScraperService
{
    private readonly HttpClient _httpClient;
    private readonly AppDbContext _context;
    private readonly IGlobalRecipeService _globalRecipeService;
    private readonly IStorageService _storageService;
    private readonly IConfiguration _configuration;

    private const string BaseUrl = "https://www.hellofresh.no";

    public HelloFreshScraperService(
        HttpClient httpClient,
        AppDbContext context,
        IGlobalRecipeService globalRecipeService,
        IStorageService storageService,
        IConfiguration configuration)
    {
        _httpClient = httpClient;
        _context = context;
        _globalRecipeService = globalRecipeService;
        _storageService = storageService;
        _configuration = configuration;
    }

    public async Task<bool> ShouldRunSyncAsync()
    {
        var syncIntervalHours = _configuration.GetValue<int>("HelloFresh:SyncIntervalHours", 24);

        var lastSync = await _context.EtlSyncLogs
            .Where(log => log.SyncType == "hellofresh" && log.Status == "success")
            .OrderByDescending(log => log.StartedAt)
            .FirstOrDefaultAsync();

        if (lastSync == null) return true;

        var hoursSinceLastSync = (DateTime.UtcNow - lastSync.StartedAt).TotalHours;
        return hoursSinceLastSync >= syncIntervalHours;
    }

    public async Task<string> GetBuildIdAsync()
    {
        var response = await _httpClient.GetStringAsync(BaseUrl);

        // Extract build ID from HTML
        // Pattern: /_next/static/[BUILD_ID]/_buildManifest.js
        var buildIdPattern = @"/_next/static/([^/]+)/_buildManifest\.js";
        var match = Regex.Match(response, buildIdPattern);

        if (!match.Success)
            throw new Exception("Failed to extract HelloFresh build ID");

        return match.Groups[1].Value;
    }

    public async Task<(int added, int updated)> SyncRecipesAsync()
    {
        var syncLog = new EtlSyncLog
        {
            Id = Guid.NewGuid(),
            SyncType = "hellofresh",
            StartedAt = DateTime.UtcNow
        };

        try
        {
            // Step 1: Get Build ID
            var buildId = await GetBuildIdAsync();
            syncLog.BuildId = buildId;

            // Step 2: Generate weeks to fetch
            var weeksToFetch = _configuration.GetValue<int>("HelloFresh:WeeksToFetch", 4);
            var weeks = GenerateWeeks(weeksToFetch);
            syncLog.WeeksSynced = string.Join(",", weeks);

            int recipesAdded = 0;
            int recipesUpdated = 0;

            // Step 3: Fetch data for each week
            foreach (var week in weeks)
            {
                var url = $"{BaseUrl}/_next/data/{buildId}/menus/{week}.json";

                try
                {
                    var json = await _httpClient.GetStringAsync(url);
                    var (added, updated) = await ProcessWeekDataAsync(json, week);

                    recipesAdded += added;
                    recipesUpdated += updated;
                }
                catch (Exception ex)
                {
                    // Log error but continue with other weeks
                    Console.WriteLine($"Error fetching week {week}: {ex.Message}");
                }
            }

            syncLog.RecipesAdded = recipesAdded;
            syncLog.RecipesUpdated = recipesUpdated;
            syncLog.Status = "success";
            syncLog.CompletedAt = DateTime.UtcNow;

            _context.EtlSyncLogs.Add(syncLog);
            await _context.SaveChangesAsync();

            return (recipesAdded, recipesUpdated);
        }
        catch (Exception ex)
        {
            syncLog.Status = "failed";
            syncLog.ErrorMessage = ex.Message;
            syncLog.CompletedAt = DateTime.UtcNow;

            _context.EtlSyncLogs.Add(syncLog);
            await _context.SaveChangesAsync();

            throw;
        }
    }

    private async Task<(int added, int updated)> ProcessWeekDataAsync(string jsonData, string week)
    {
        using var doc = JsonDocument.Parse(jsonData);
        var root = doc.RootElement;

        int added = 0;
        int updated = 0;

        // Navigate to courses: pageProps.ssrPayload.courses
        if (!root.TryGetProperty("pageProps", out var pageProps) ||
            !pageProps.TryGetProperty("ssrPayload", out var ssrPayload) ||
            !ssrPayload.TryGetProperty("courses", out var courses))
        {
            return (0, 0);
        }

        foreach (var course in courses.EnumerateArray())
        {
            try
            {
                // Extract recipe data
                var recipe = await ParseHelloFreshRecipeAsync(course);

                if (recipe != null)
                {
                    var existing = await _globalRecipeService.GetByHellofreshUuidAsync(recipe.HellofreshUuid!);

                    if (existing == null)
                    {
                        await _globalRecipeService.UpsertHellofreshRecipeAsync(recipe);
                        added++;
                    }
                    else
                    {
                        await _globalRecipeService.UpsertHellofreshRecipeAsync(recipe);
                        updated++;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing recipe: {ex.Message}");
            }
        }

        return (added, updated);
    }

    private async Task<GlobalRecipe?> ParseHelloFreshRecipeAsync(JsonElement course)
    {
        try
        {
            // Extract the recipe object from the course
            if (!course.TryGetProperty("recipe", out var recipeElement))
            {
                return null;
            }

            // Extract basic data from recipe object
            if (!recipeElement.TryGetProperty("uuid", out var uuidElement) ||
                !recipeElement.TryGetProperty("name", out var nameElement))
            {
                return null;
            }

            var uuid = uuidElement.GetString();
            var title = nameElement.GetString();

            if (string.IsNullOrEmpty(uuid) || string.IsNullOrEmpty(title))
                return null;

            // Extract description (use headline as description)
            string? description = null;
            if (recipeElement.TryGetProperty("headline", out var descElement))
            {
                description = descElement.GetString();
            }

            // Extract image URL and re-host it
            string? imageUrl = null;
            if (recipeElement.TryGetProperty("imageLink", out var imageElement))
            {
                var originalImageUrl = imageElement.GetString();
                if (!string.IsNullOrEmpty(originalImageUrl))
                {
                    try
                    {
                        // Re-host image to Supabase Storage
                        imageUrl = await _storageService.UploadImageFromUrlAsync(
                            originalImageUrl,
                            $"{uuid}.jpg",
                            "hellofresh"
                        );
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to upload image for {uuid}: {ex.Message}");
                        imageUrl = originalImageUrl; // Fallback to original URL
                    }
                }
            }

            // Extract ingredients
            var ingredients = new List<object>();
            if (recipeElement.TryGetProperty("ingredients", out var ingredientsElement))
            {
                foreach (var ing in ingredientsElement.EnumerateArray())
                {
                    var ingredient = new Dictionary<string, string>();

                    if (ing.TryGetProperty("name", out var ingName))
                        ingredient["name"] = ingName.GetString() ?? "";

                    if (ing.TryGetProperty("quantity", out var quantity))
                        ingredient["amount"] = quantity.GetString() ?? "";

                    if (ing.TryGetProperty("unit", out var unit))
                        ingredient["unit"] = unit.GetString() ?? "";

                    if (ing.TryGetProperty("imagePath", out var ingImage))
                        ingredient["image"] = ingImage.GetString() ?? "";

                    ingredients.Add(ingredient);
                }
            }

            // Extract nutrition data
            Dictionary<string, object>? nutritionData = null;
            if (recipeElement.TryGetProperty("nutrition", out var nutritionElement))
            {
                nutritionData = new Dictionary<string, object>();

                foreach (var prop in nutritionElement.EnumerateObject())
                {
                    nutritionData[prop.Name] = prop.Value.ToString();
                }
            }

            // Extract cook time
            int? cookTime = null;
            if (recipeElement.TryGetProperty("prepTime", out var prepTimeElement))
            {
                if (prepTimeElement.TryGetInt32(out var time))
                {
                    cookTime = time;
                }
                else
                {
                    // Sometimes it's a string like "PT20M"
                    var timeStr = prepTimeElement.GetString();
                    if (!string.IsNullOrEmpty(timeStr))
                    {
                        var match = Regex.Match(timeStr, @"\d+");
                        if (match.Success && int.TryParse(match.Value, out var parsedTime))
                        {
                            cookTime = parsedTime;
                        }
                    }
                }
            }

            // Extract difficulty
            string? difficulty = null;
            if (recipeElement.TryGetProperty("difficulty", out var difficultyElement))
            {
                if (difficultyElement.TryGetInt32(out var diffValue))
                {
                    difficulty = diffValue switch
                    {
                        1 => "Easy",
                        2 => "Medium",
                        3 => "Hard",
                        _ => "Easy"
                    };
                }
            }

            return new GlobalRecipe
            {
                Id = Guid.NewGuid(),
                Title = title,
                Description = description,
                ImageUrl = imageUrl,
                Ingredients = JsonSerializer.Serialize(ingredients),
                NutritionData = nutritionData != null ? JsonSerializer.Serialize(nutritionData) : null,
                CookTimeMinutes = cookTime,
                Difficulty = difficulty,
                IsHellofresh = true,
                HellofreshUuid = uuid,
                IsPublic = false,
                CreatedByUserId = null
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error parsing HelloFresh recipe: {ex.Message}");
            return null;
        }
    }

    private List<string> GenerateWeeks(int count)
    {
        var weeks = new List<string>();
        var currentDate = DateTime.UtcNow;

        for (int i = 0; i < count; i++)
        {
            var targetDate = currentDate.AddDays(i * 7);
            var calendar = CultureInfo.CurrentCulture.Calendar;
            var weekNumber = calendar.GetWeekOfYear(
                targetDate,
                CalendarWeekRule.FirstFourDayWeek,
                DayOfWeek.Monday
            );

            var weekString = $"{targetDate.Year}-W{weekNumber:D2}";
            weeks.Add(weekString);
        }

        return weeks;
    }
}
