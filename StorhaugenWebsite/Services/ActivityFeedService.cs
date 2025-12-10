using StorhaugenWebsite.ApiClient;
using StorhaugenWebsite.DTOs;

namespace StorhaugenWebsite.Services
{
    public class ActivityFeedService : IActivityFeedService
    {
        private readonly IApiClient _apiClient;
        private readonly IAuthService _authService;

        public List<ActivityFeedItemDto> CachedFeedItems { get; private set; } = new();
        public ActivitySummaryDto? CachedSummary { get; private set; }

        public ActivityFeedService(IApiClient apiClient, IAuthService authService)
        {
            _apiClient = apiClient;
            _authService = authService;
        }

        private void ValidateAuthentication()
        {
            if (!_authService.IsAuthenticated)
            {
                throw new UnauthorizedAccessException("You must be logged in to access the activity feed.");
            }
        }

        public async Task<ActivityFeedPagedResult> GetFeedAsync(ActivityFeedQuery? query = null)
        {
            ValidateAuthentication();

            query ??= new ActivityFeedQuery();
            var result = await _apiClient.GetFeedAsync(query);

            // Cache first page of results
            if (query.Page == 1)
            {
                CachedFeedItems = result.Items;
            }

            return result;
        }

        public async Task<ActivityFeedPagedResult> GetMyActivityAsync(int page = 1, int pageSize = 20)
        {
            ValidateAuthentication();
            return await _apiClient.GetMyActivityAsync(page, pageSize);
        }

        public async Task<ActivitySummaryDto> GetActivitySummaryAsync()
        {
            ValidateAuthentication();

            var summary = await _apiClient.GetActivitySummaryAsync();
            CachedSummary = summary;
            return summary;
        }

        public void InvalidateCache()
        {
            CachedFeedItems = new List<ActivityFeedItemDto>();
            CachedSummary = null;
        }
    }
}
