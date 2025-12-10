using StorhaugenWebsite.DTOs;

namespace StorhaugenWebsite.Services
{
    public interface IActivityFeedService
    {
        // Feed queries
        Task<ActivityFeedPagedResult> GetFeedAsync(ActivityFeedQuery? query = null);
        Task<ActivityFeedPagedResult> GetMyActivityAsync(int page = 1, int pageSize = 20);
        Task<ActivitySummaryDto> GetActivitySummaryAsync();

        // Cache
        List<ActivityFeedItemDto> CachedFeedItems { get; }
        ActivitySummaryDto? CachedSummary { get; }
        void InvalidateCache();
    }
}
