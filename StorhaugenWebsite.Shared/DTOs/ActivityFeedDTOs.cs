namespace StorhaugenWebsite.Shared.DTOs;

// ==========================================
// ACTIVITY FEED DTOs
// ==========================================

/// <summary>
/// DTO for a single activity feed item.
/// </summary>
public class ActivityFeedItemDto
{
    public Guid Id { get; set; }

    // User who performed the action
    public Guid UserId { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public string? UserAvatarUrl { get; set; }

    /// <summary>
    /// Type of activity: "rated", "added", "published", "joined_household".
    /// </summary>
    public string ActivityType { get; set; } = string.Empty;

    /// <summary>
    /// Type of target: "user_recipe", "global_recipe", "household".
    /// </summary>
    public string TargetType { get; set; } = string.Empty;

    public Guid TargetId { get; set; }

    // Denormalized data for display
    public string? RecipeName { get; set; }
    public string? RecipeImageUrl { get; set; }
    public int? RatingScore { get; set; }
    public string? HouseholdName { get; set; }

    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Human-readable activity description.
    /// Example: "Sarah rated Spaghetti Carbonara 9/10"
    /// </summary>
    public string Description => GenerateDescription();

    private string GenerateDescription()
    {
        return ActivityType switch
        {
            "rated" => $"{UserDisplayName} rated {RecipeName ?? "a recipe"} {RatingScore}/10",
            "added" => $"{UserDisplayName} added {RecipeName ?? "a new recipe"}",
            "published" => $"{UserDisplayName} published {RecipeName ?? "a recipe"} to the community",
            "joined_household" => $"{UserDisplayName} joined {HouseholdName ?? "a household"}",
            _ => $"{UserDisplayName} did something"
        };
    }
}

/// <summary>
/// Query parameters for the activity feed.
/// </summary>
public class ActivityFeedQuery
{
    /// <summary>
    /// Filter by activity types. If empty, all types are included.
    /// </summary>
    public List<string>? Types { get; set; }

    /// <summary>
    /// Only show activities from specific users. If empty, shows all friends.
    /// </summary>
    public List<Guid>? UserIds { get; set; }

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

/// <summary>
/// Paged result for activity feed.
/// </summary>
public class ActivityFeedPagedResult
{
    public List<ActivityFeedItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasMore => Page < TotalPages;
}

/// <summary>
/// Summary of recent activity for a user.
/// </summary>
public class ActivitySummaryDto
{
    public int TotalActivities { get; set; }
    public int RecipesAdded { get; set; }
    public int RecipesRated { get; set; }
    public int RecipesPublished { get; set; }
    public DateTime? LastActivityDate { get; set; }
}
