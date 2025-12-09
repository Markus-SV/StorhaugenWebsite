namespace StorhaugenEats.API.DTOs;

// ==========================================
// USER RECIPE DTOs
// ==========================================

/// <summary>
/// DTO for returning user recipe data.
/// </summary>
public class UserRecipeDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public string? UserAvatarUrl { get; set; }

    // Recipe data (resolved from local or global)
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public object? Ingredients { get; set; }

    // Link status
    public Guid? GlobalRecipeId { get; set; }
    public string? GlobalRecipeName { get; set; }
    public bool IsLinkedToGlobal => GlobalRecipeId.HasValue;
    public bool IsPublished { get; set; }

    // Metadata
    public string Visibility { get; set; } = "private";
    public string? PersonalNotes { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Ratings
    public int? MyRating { get; set; }
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
    public Dictionary<string, int?> HouseholdRatings { get; set; } = new();
}

/// <summary>
/// DTO for creating a new user recipe.
/// </summary>
public class CreateUserRecipeDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string>? ImageUrls { get; set; }
    public object? Ingredients { get; set; }
    public string? PersonalNotes { get; set; }
    public Guid? GlobalRecipeId { get; set; }
    public string Visibility { get; set; } = "private";
}

/// <summary>
/// DTO for updating a user recipe.
/// </summary>
public class UpdateUserRecipeDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string>? ImageUrls { get; set; }
    public object? Ingredients { get; set; }
    public string? PersonalNotes { get; set; }
    public string? Visibility { get; set; }
}

/// <summary>
/// DTO for publishing a recipe result.
/// </summary>
public class PublishRecipeResultDto
{
    public UserRecipeDto UserRecipe { get; set; } = null!;
    public Guid GlobalRecipeId { get; set; }
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Query parameters for getting user recipes.
/// </summary>
public class GetUserRecipesQuery
{
    public string? Visibility { get; set; }
    public bool IncludeArchived { get; set; } = false;
    public string SortBy { get; set; } = "date";
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Paged result for user recipes.
/// </summary>
public class UserRecipePagedResult
{
    public List<UserRecipeDto> Recipes { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
