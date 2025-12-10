namespace StorhaugenWebsite.Shared.DTOs;

// ==========================================
// HOUSEHOLD AGGREGATION DTOs
// ==========================================

/// <summary>
/// DTO for an aggregated recipe from household members.
/// This is used when viewing the "Family Cookbook" - an aggregation of all member recipes.
/// </summary>
public class AggregatedRecipeDto
{
    public Guid UserRecipeId { get; set; }

    // Owner info
    public Guid OwnerUserId { get; set; }
    public string OwnerDisplayName { get; set; } = string.Empty;
    public string? OwnerAvatarUrl { get; set; }

    // Recipe info
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> ImageUrls { get; set; } = new();

    // Link status
    public Guid? GlobalRecipeId { get; set; }
    public bool IsLinkedToGlobal => GlobalRecipeId.HasValue;

    // Ratings from household members
    public Dictionary<string, int?> HouseholdRatings { get; set; } = new();
    public double HouseholdAverageRating { get; set; }
    public int HouseholdRatingCount { get; set; }

    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for a "common favorite" - a recipe that multiple household members have rated highly.
/// </summary>
public class CommonFavoriteDto
{
    /// <summary>
    /// The GlobalRecipeId that multiple members have in common.
    /// </summary>
    public Guid GlobalRecipeId { get; set; }

    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }

    // Member ratings
    public List<MemberRatingDto> MemberRatings { get; set; } = new();
    public double AverageRating { get; set; }
    public int MembersWhoRated { get; set; }

    /// <summary>
    /// Whether this recipe is already in the current user's collection.
    /// </summary>
    public bool IsInMyCollection { get; set; }
}

/// <summary>
/// DTO for a single member's rating.
/// </summary>
public class MemberRatingDto
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime RatedAt { get; set; }
}

/// <summary>
/// Query parameters for getting combined household recipes.
/// </summary>
public class GetCombinedRecipesQuery
{
    /// <summary>
    /// Filter to only show recipes from specific members.
    /// </summary>
    public List<Guid>? FilterByMembers { get; set; }

    /// <summary>
    /// Minimum average rating (from household members).
    /// </summary>
    public double? MinRating { get; set; }

    /// <summary>
    /// Search term for recipe name.
    /// </summary>
    public string? Search { get; set; }

    public string SortBy { get; set; } = "date";
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

/// <summary>
/// Query parameters for getting common favorites.
/// </summary>
public class GetCommonFavoritesQuery
{
    /// <summary>
    /// Minimum number of members who must have rated the recipe.
    /// </summary>
    public int MinMembers { get; set; } = 2;

    /// <summary>
    /// Minimum average rating from those members.
    /// </summary>
    public double MinAverageRating { get; set; } = 4.0;

    public int Limit { get; set; } = 20;
}

/// <summary>
/// Paged result for aggregated recipes.
/// </summary>
public class AggregatedRecipePagedResult
{
    public List<AggregatedRecipeDto> Recipes { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
