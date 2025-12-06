namespace StorhaugenWebsite.DTOs;

// User DTOs
public class UserDto
{
    public int Id { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public int? CurrentHouseholdId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateUserDto
{
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public int? CurrentHouseholdId { get; set; }
}

// Household DTOs
public class HouseholdDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int CreatedById { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<HouseholdMemberDto> Members { get; set; } = new();
}

public class HouseholdMemberDto
{
    public int UserId { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class CreateHouseholdDto
{
    public required string Name { get; set; }
}

public class InviteToHouseholdDto
{
    public required string Email { get; set; }
}

public class HouseholdInviteDto
{
    public int Id { get; set; }
    public int HouseholdId { get; set; }
    public required string HouseholdName { get; set; }
    public int InvitedById { get; set; }
    public required string InvitedByName { get; set; }
    public required string InvitedEmail { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }
}

// Recipe DTOs
public class HouseholdRecipeDto
{
    public int Id { get; set; }
    public int HouseholdId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public Dictionary<string, int?> Ratings { get; set; } = new();
    public double AverageRating { get; set; }
    public DateTime DateAdded { get; set; }
    public int AddedByUserId { get; set; }
    public string? AddedByName { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedDate { get; set; }
    public int? ArchivedByUserId { get; set; }
    public string? ArchivedByName { get; set; }

    // Multi-tenant fields
    public int? GlobalRecipeId { get; set; }
    public string? GlobalRecipeName { get; set; }
    public bool IsForked { get; set; }
    public string? PersonalNotes { get; set; }
}

public class CreateHouseholdRecipeDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string>? ImageUrls { get; set; }
    public string? ImageUrl { get; set; }
    public string? PersonalNotes { get; set; }
    public int? GlobalRecipeId { get; set; }
    public bool Fork { get; set; } = false;

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
}

public class RateRecipeDto
{
    public int Rating { get; set; } // 0-10
}

// Global Recipe DTOs
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
    public bool IsHellofresh { get; set; }
    public string? HellofreshUuid { get; set; }
    public string? HellofreshSlug { get; set; }
    public int? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public double AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public int TotalTimesAdded { get; set; }
    public DateTime CreatedAt { get; set; }
    public object? Instructions { get; set; }

}

public class GlobalRecipePagedResult
{
    public List<GlobalRecipeDto> Recipes { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class BrowseGlobalRecipesQuery
{
    public string? Search { get; set; }
    public string? Cuisine { get; set; }
    public string? Difficulty { get; set; }
    public int? MaxPrepTime { get; set; }
    public List<string>? Tags { get; set; }
    public bool HellofreshOnly { get; set; } = false;
    public string SortBy { get; set; } = "popular";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// Storage DTOs
public class UploadImageDto
{
    public required string FileName { get; set; }
    public required string Base64Data { get; set; }
    public string Bucket { get; set; } = "recipe-images";
}

public class UploadImageResultDto
{
    public required string Url { get; set; }
    public required string FileName { get; set; }
}
