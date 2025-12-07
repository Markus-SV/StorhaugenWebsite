namespace StorhaugenEats.API.DTOs;

public class HouseholdRecipeDto
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public Dictionary<string, int?> Ratings { get; set; } = new();
    public double AverageRating { get; set; }
    public DateTime DateAdded { get; set; }
    public Guid AddedByUserId { get; set; }
    public string? AddedByName { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedDate { get; set; }
    public Guid? ArchivedByUserId { get; set; }
    public string? ArchivedByName { get; set; }

    // If linked to global recipe
    public Guid? GlobalRecipeId { get; set; }
    public string? GlobalRecipeName { get; set; }
    public bool IsForked { get; set; }
    public string? PersonalNotes { get; set; }

    // Public sharing
    public bool IsPublic { get; set; }
    public string? HouseholdName { get; set; }
}

public class CreateHouseholdRecipeDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string>? ImageUrls { get; set; }
    public string? ImageUrl { get; set; }
    public string? PersonalNotes { get; set; }
    public bool IsPublic { get; set; } = false;

    // Optional: Link to global recipe
    public Guid? GlobalRecipeId { get; set; }
    public bool Fork { get; set; } = false; // If true, copy recipe; if false, link to it

    // Additional fields for forking
    public int? PrepTimeMinutes { get; set; }
    public int? Servings { get; set; }
    public string? Difficulty { get; set; }
    public string? Cuisine { get; set; }
    public List<string>? Tags { get; set; }
    public object? Ingredients { get; set; }
    public object? Instructions { get; set; }
}

public class UpdateHouseholdRecipeDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string>? ImageUrls { get; set; }
    public string? PersonalNotes { get; set; }
    public bool? IsPublic { get; set; }
}

public class RateRecipeDto
{
    public int Rating { get; set; } // 0-10
}

// Public recipes (community)
public class PublicRecipeDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
    public DateTime DateAdded { get; set; }
    public string HouseholdName { get; set; } = string.Empty;
    public string? AddedByName { get; set; }
    public Guid HouseholdId { get; set; }
    public Guid? GlobalRecipeId { get; set; }
}

public class BrowsePublicRecipesQuery
{
    public string? Search { get; set; }
    public string SortBy { get; set; } = "newest";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PublicRecipePagedResult
{
    public List<PublicRecipeDto> Recipes { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
