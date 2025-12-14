using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;
using StorhaugenWebsite.Shared.DTOs;
using StorhaugenEats.API.Models;
using System.Text.Json;

namespace StorhaugenEats.API.Services;

public class ActivityFeedService : IActivityFeedService
{
    private readonly AppDbContext _context;
    private readonly IUserFriendshipService _friendshipService;

    public ActivityFeedService(AppDbContext context, IUserFriendshipService friendshipService)
    {
        _context = context;
        _friendshipService = friendshipService;
    }

    public async Task<ActivityFeedPagedResult> GetFeedAsync(Guid userId, ActivityFeedQuery query)
    {
        // Get friend IDs
        var friendIds = await _friendshipService.GetFriendIdsAsync(userId);

        // If no friends, return empty feed
        if (!friendIds.Any())
        {
            return new ActivityFeedPagedResult
            {
                Items = new List<ActivityFeedItemDto>(),
                TotalCount = 0,
                Page = query.Page,
                PageSize = query.PageSize
            };
        }

        // Filter by specific users if provided, otherwise show all friends
        var targetUserIds = query.UserIds?.Any() == true
            ? query.UserIds.Intersect(friendIds).ToList()
            : friendIds;

        var queryable = _context.ActivityFeedItems
            .Include(a => a.User)
            .Where(a => targetUserIds.Contains(a.UserId));

        // Filter by activity types
        if (query.Types?.Any() == true)
        {
            queryable = queryable.Where(a => query.Types.Contains(a.ActivityType));
        }

        queryable = queryable.OrderByDescending(a => a.CreatedAt);

        var totalCount = await queryable.CountAsync();
        var items = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new ActivityFeedPagedResult
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<ActivityFeedPagedResult> GetUserActivityAsync(Guid userId, int page = 1, int pageSize = 20)
    {
        var queryable = _context.ActivityFeedItems
            .Include(a => a.User)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt);

        var totalCount = await queryable.CountAsync();
        var items = await queryable
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new ActivityFeedPagedResult
        {
            Items = items.Select(MapToDto).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ActivitySummaryDto> GetActivitySummaryAsync(Guid userId)
    {
        var activities = await _context.ActivityFeedItems
            .Where(a => a.UserId == userId)
            .ToListAsync();

        return new ActivitySummaryDto
        {
            TotalActivities = activities.Count,
            RecipesAdded = activities.Count(a => a.ActivityType == "added"),
            RecipesRated = activities.Count(a => a.ActivityType == "rated"),
            RecipesPublished = activities.Count(a => a.ActivityType == "published"),
            LastActivityDate = activities.OrderByDescending(a => a.CreatedAt).FirstOrDefault()?.CreatedAt
        };
    }

    public async Task RecordRatingActivityAsync(Guid userId, Guid recipeId, string recipeName, int rating, string? imageUrl = null)
    {
        var activity = ActivityFeedItem.CreateRatingActivity(userId, recipeId, recipeName, rating, imageUrl);
        _context.ActivityFeedItems.Add(activity);
        await _context.SaveChangesAsync();
    }

    public async Task RecordAddedRecipeActivityAsync(Guid userId, Guid recipeId, string recipeName, string? imageUrl = null)
    {
        var activity = ActivityFeedItem.CreateAddedRecipeActivity(userId, recipeId, recipeName, imageUrl);
        _context.ActivityFeedItems.Add(activity);
        await _context.SaveChangesAsync();
    }

    public async Task RecordPublishedActivityAsync(Guid userId, Guid globalRecipeId, string recipeName, string? imageUrl = null)
    {
        var activity = ActivityFeedItem.CreatePublishedActivity(userId, globalRecipeId, recipeName, imageUrl);
        _context.ActivityFeedItems.Add(activity);
        await _context.SaveChangesAsync();
    }

    public async Task CleanupOldActivitiesAsync(int daysToKeep = 90)
    {
        var cutoffDate = DateTime.UtcNow.AddDays(-daysToKeep);

        var oldActivities = await _context.ActivityFeedItems
            .Where(a => a.CreatedAt < cutoffDate)
            .ToListAsync();

        if (oldActivities.Any())
        {
            _context.ActivityFeedItems.RemoveRange(oldActivities);
            await _context.SaveChangesAsync();
        }
    }

    private ActivityFeedItemDto MapToDto(ActivityFeedItem activity)
    {
        var metadata = ParseMetadata(activity.Metadata);

        return new ActivityFeedItemDto
        {
            Id = activity.Id,
            UserId = activity.UserId,
            UserDisplayName = activity.User?.DisplayName ?? "Unknown",
            UserAvatarUrl = activity.User?.AvatarUrl,
            ActivityType = activity.ActivityType,
            TargetType = activity.TargetType,
            TargetId = activity.TargetId,
            RecipeName = metadata.GetValueOrDefault("recipeName")?.ToString(),
            RecipeImageUrl = metadata.GetValueOrDefault("imageUrl")?.ToString(),
            RatingScore = metadata.TryGetValue("rating", out var ratingObj) && ratingObj is JsonElement elem
                ? elem.TryGetInt32(out var rating) ? rating : null
                : null,
            CreatedAt = activity.CreatedAt
        };
    }

    private static Dictionary<string, object?> ParseMetadata(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(json) ?? new();
        }
        catch
        {
            return new Dictionary<string, object?>();
        }
    }
}
