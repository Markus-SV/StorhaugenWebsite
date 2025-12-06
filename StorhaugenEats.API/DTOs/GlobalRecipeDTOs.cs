namespace StorhaugenEats.API.DTOs;

public class GlobalRecipeDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public object? Ingredients { get; set; }
    public object? NutritionData { get; set; }
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public int? TotalTimeMinutes { get; set; }
    public int? Servings { get; set; }
    public string? Difficulty { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? Cuisine { get; set; }

    // HelloFresh specific
    public bool IsHellofresh { get; set; }
    public string? HellofreshUuid { get; set; }
    public string? HellofreshSlug { get; set; }

    // User-created
    public int? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }

    // Aggregated ratings
    public double AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public int TotalTimesAdded { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class CreateGlobalRecipeDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public object? Ingredients { get; set; }
    public object? NutritionData { get; set; }
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public int? Servings { get; set; }
    public string? Difficulty { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? Cuisine { get; set; }
}

public class BrowseGlobalRecipesQuery
{
    public string? Search { get; set; }
    public string? Cuisine { get; set; }
    public string? Difficulty { get; set; }
    public int? MaxPrepTime { get; set; }
    public List<string>? Tags { get; set; }
    public bool HellofreshOnly { get; set; } = false;
    public string SortBy { get; set; } = "popular"; // popular, newest, rating, name
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class GlobalRecipePagedResult
{
    public List<GlobalRecipeDto> Recipes { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
