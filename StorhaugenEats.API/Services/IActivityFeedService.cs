using StorhaugenWebsite.Shared.DTOs;

namespace StorhaugenEats.API.Services;

/// <summary>
/// Service for managing the social activity feed.
/// </summary>
public interface IActivityFeedService
{
    // Feed queries
    Task<ActivityFeedPagedResult> GetFeedAsync(Guid userId, ActivityFeedQuery query);
    Task<ActivityFeedPagedResult> GetUserActivityAsync(Guid userId, int page = 1, int pageSize = 20);
    Task<ActivitySummaryDto> GetActivitySummaryAsync(Guid userId);

    // Activity creation (called by other services)
    Task RecordRatingActivityAsync(Guid userId, Guid recipeId, string recipeName, int rating, string? imageUrl = null);
    Task RecordAddedRecipeActivityAsync(Guid userId, Guid recipeId, string recipeName, string? imageUrl = null);
    Task RecordPublishedActivityAsync(Guid userId, Guid globalRecipeId, string recipeName, string? imageUrl = null);
    Task RecordJoinedHouseholdActivityAsync(Guid userId, Guid householdId, string householdName);

    // Cleanup
    Task CleanupOldActivitiesAsync(int daysToKeep = 90);
}
