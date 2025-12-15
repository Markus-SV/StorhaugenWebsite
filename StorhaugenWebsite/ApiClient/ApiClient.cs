using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using StorhaugenWebsite.Shared.DTOs;
using StorhaugenWebsite.Services;

namespace StorhaugenWebsite.ApiClient;

public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;
    private readonly JsonSerializerOptions _jsonOptions;

    public ApiClient(HttpClient httpClient, IAuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;

        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
    }

    private async Task AddAuthHeaderAsync()
    {
        var token = await _authService.GetAccessTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    // User Methods
    public async Task<UserDto?> GetMyProfileAsync()
    {
        await AddAuthHeaderAsync();
        return await _httpClient.GetFromJsonAsync<UserDto>("/api/users/me", _jsonOptions);
    }

    public async Task<UserDto> UpdateMyProfileAsync(UpdateUserDto dto)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PutAsJsonAsync("/api/users/me", dto, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserDto>(_jsonOptions))!;
    }

    // Collection Methods
    public async Task<List<CollectionDto>> GetMyCollectionsAsync()
    {
        await AddAuthHeaderAsync();
        return (await _httpClient.GetFromJsonAsync<List<CollectionDto>>("/api/collections", _jsonOptions)) ?? new();
    }

    public async Task<CollectionDto?> GetCollectionAsync(Guid id)
    {
        await AddAuthHeaderAsync();
        return await _httpClient.GetFromJsonAsync<CollectionDto>($"/api/collections/{id}", _jsonOptions);
    }

    public async Task<CollectionDto> CreateCollectionAsync(CreateCollectionDto dto)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("/api/collections", dto, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CollectionDto>(_jsonOptions))!;
    }

    public async Task<CollectionDto> UpdateCollectionAsync(Guid id, UpdateCollectionDto dto)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PutAsJsonAsync($"/api/collections/{id}", dto, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CollectionDto>(_jsonOptions))!;
    }

    public async Task DeleteCollectionAsync(Guid id)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.DeleteAsync($"/api/collections/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<CollectionRecipesResult> GetCollectionRecipesAsync(Guid id, GetCollectionRecipesQuery? query = null)
    {
        await AddAuthHeaderAsync();
        var queryParams = new List<string>();

        if (query != null)
        {
            if (!string.IsNullOrWhiteSpace(query.Search))
                queryParams.Add($"search={Uri.EscapeDataString(query.Search)}");

            queryParams.Add($"sortBy={Uri.EscapeDataString(query.SortBy)}");
            queryParams.Add($"sortDescending={query.SortDescending}");
            queryParams.Add($"page={query.Page}");
            queryParams.Add($"pageSize={query.PageSize}");
        }

        var url = queryParams.Count > 0
            ? $"/api/collections/{id}/recipes?{string.Join("&", queryParams)}"
            : $"/api/collections/{id}/recipes";

        return (await _httpClient.GetFromJsonAsync<CollectionRecipesResult>(url, _jsonOptions))
            ?? new CollectionRecipesResult();
    }

    public async Task AddRecipeToCollectionAsync(Guid collectionId, AddRecipeToCollectionDto dto)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync($"/api/collections/{collectionId}/recipes", dto, _jsonOptions);
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveRecipeFromCollectionAsync(Guid collectionId, Guid recipeId)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.DeleteAsync($"/api/collections/{collectionId}/recipes/{recipeId}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<CollectionMemberDto>> GetCollectionMembersAsync(Guid collectionId)
    {
        await AddAuthHeaderAsync();
        return (await _httpClient.GetFromJsonAsync<List<CollectionMemberDto>>($"/api/collections/{collectionId}/members", _jsonOptions))
            ?? new List<CollectionMemberDto>();
    }

    public async Task AddCollectionMemberAsync(Guid collectionId, AddCollectionMemberDto dto)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync($"/api/collections/{collectionId}/members", dto, _jsonOptions);
        response.EnsureSuccessStatusCode();
    }

    public async Task RemoveCollectionMemberAsync(Guid collectionId, Guid memberId)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.DeleteAsync($"/api/collections/{collectionId}/members/{memberId}");
        response.EnsureSuccessStatusCode();
    }

    public async Task LeaveCollectionAsync(Guid collectionId)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsync($"/api/collections/{collectionId}/leave", null);
        response.EnsureSuccessStatusCode();
    }

    // Global Recipe Methods
    public async Task<GlobalRecipePagedResult> BrowseGlobalRecipesAsync(BrowseGlobalRecipesQuery query)
    {
        var queryParams = new List<string>();

        if (!string.IsNullOrWhiteSpace(query.Search))
            queryParams.Add($"search={Uri.EscapeDataString(query.Search)}");

        if (!string.IsNullOrWhiteSpace(query.Cuisine))
            queryParams.Add($"cuisine={Uri.EscapeDataString(query.Cuisine)}");

        if (!string.IsNullOrWhiteSpace(query.Difficulty))
            queryParams.Add($"difficulty={Uri.EscapeDataString(query.Difficulty)}");

        if (query.MaxPrepTime.HasValue)
            queryParams.Add($"maxPrepTime={query.MaxPrepTime.Value}");

        if (query.Tags != null && query.Tags.Count > 0)
        {
            foreach (var tag in query.Tags)
                queryParams.Add($"tags={Uri.EscapeDataString(tag)}");
        }

        if (query.HellofreshOnly)
            queryParams.Add("hellofreshOnly=true");

        if (!string.IsNullOrWhiteSpace(query.HellofreshWeek))
            queryParams.Add($"hellofreshWeek={Uri.EscapeDataString(query.HellofreshWeek)}");

        queryParams.Add($"sortBy={Uri.EscapeDataString(query.SortBy)}");
        queryParams.Add($"page={query.Page}");
        queryParams.Add($"pageSize={query.PageSize}");

        var url = $"/api/global-recipes?{string.Join("&", queryParams)}";

        return (await _httpClient.GetFromJsonAsync<GlobalRecipePagedResult>(url, _jsonOptions))
            ?? new GlobalRecipePagedResult();
    }

    public async Task<GlobalRecipeDto?> GetGlobalRecipeAsync(Guid id)
    {
        return await _httpClient.GetFromJsonAsync<GlobalRecipeDto>($"/api/global-recipes/{id}", _jsonOptions);
    }

    public async Task<List<GlobalRecipeDto>> SearchGlobalRecipesAsync(string query, int limit = 20)
    {
        var url = $"/api/global-recipes/search?q={Uri.EscapeDataString(query)}&limit={limit}";
        return (await _httpClient.GetFromJsonAsync<List<GlobalRecipeDto>>(url, _jsonOptions)) ?? new();
    }

    public async Task DeleteGlobalRecipeAsync(Guid id)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.DeleteAsync($"/api/global-recipes/{id}");
        response.EnsureSuccessStatusCode();
    }

    // Storage Methods
    public async Task<UploadImageResultDto> UploadImageAsync(byte[] imageData, string fileName)
    {
        await AddAuthHeaderAsync();

        var base64 = Convert.ToBase64String(imageData);
        var dto = new UploadImageDto
        {
            FileName = fileName,
            Base64Data = base64
        };

        var response = await _httpClient.PostAsJsonAsync("/api/storage/upload", dto, _jsonOptions);
        response.EnsureSuccessStatusCode();

        return (await response.Content.ReadFromJsonAsync<UploadImageResultDto>(_jsonOptions))!;
    }

    public async Task DeleteImageAsync(string fileName)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.DeleteAsync($"/api/storage/{Uri.EscapeDataString(fileName)}");
        response.EnsureSuccessStatusCode();
    }

    // User Recipes (user-centric recipe management)
    public async Task<UserRecipePagedResult> GetMyUserRecipesAsync(GetUserRecipesQuery query)
    {
        await AddAuthHeaderAsync();
        var queryParams = new List<string>();

        if (!string.IsNullOrWhiteSpace(query.Visibility))
            queryParams.Add($"visibility={Uri.EscapeDataString(query.Visibility)}");

        if (query.IncludeArchived)
            queryParams.Add("includeArchived=true");

        queryParams.Add($"sortBy={Uri.EscapeDataString(query.SortBy)}");
        queryParams.Add($"page={query.Page}");
        queryParams.Add($"pageSize={query.PageSize}");

        var url = $"/api/user-recipes?{string.Join("&", queryParams)}";
        return (await _httpClient.GetFromJsonAsync<UserRecipePagedResult>(url, _jsonOptions))
            ?? new UserRecipePagedResult();
    }

    public async Task<UserRecipeDto?> GetUserRecipeAsync(Guid id)
    {
        await AddAuthHeaderAsync();
        return await _httpClient.GetFromJsonAsync<UserRecipeDto>($"/api/user-recipes/{id}", _jsonOptions);
    }

    public async Task<UserRecipeDto> CreateUserRecipeAsync(CreateUserRecipeDto dto)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("/api/user-recipes", dto, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserRecipeDto>(_jsonOptions))!;
    }

    public async Task<UserRecipeDto> UpdateUserRecipeAsync(Guid id, UpdateUserRecipeDto dto)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PutAsJsonAsync($"/api/user-recipes/{id}", dto, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserRecipeDto>(_jsonOptions))!;
    }

    public async Task DeleteUserRecipeAsync(Guid id)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.DeleteAsync($"/api/user-recipes/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<PublishRecipeResultDto> PublishUserRecipeAsync(Guid id)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsync($"/api/user-recipes/{id}/publish", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<PublishRecipeResultDto>(_jsonOptions))!;
    }

    public async Task<UserRecipeDto> DetachUserRecipeAsync(Guid id)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsync($"/api/user-recipes/{id}/detach", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserRecipeDto>(_jsonOptions))!;
    }

    public async Task<UserRecipeDto> RateUserRecipeAsync(Guid id, int rating, string? comment = null)
    {
        await AddAuthHeaderAsync();
        var dto = new { Rating = rating, Comment = comment };
        var response = await _httpClient.PostAsJsonAsync($"/api/user-recipes/{id}/rate", dto, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserRecipeDto>(_jsonOptions))!;
    }

    public async Task RemoveUserRecipeRatingAsync(Guid id)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.DeleteAsync($"/api/user-recipes/{id}/rate");
        response.EnsureSuccessStatusCode();
    }

    public async Task<UserRecipeDto> ArchiveUserRecipeAsync(Guid id)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsync($"/api/user-recipes/{id}/archive", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserRecipeDto>(_jsonOptions))!;
    }

    public async Task<UserRecipeDto> RestoreUserRecipeAsync(Guid id)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsync($"/api/user-recipes/{id}/restore", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserRecipeDto>(_jsonOptions))!;
    }

    public async Task<UserRecipePagedResult> GetFriendsRecipesAsync(GetUserRecipesQuery query)
    {
        await AddAuthHeaderAsync();
        var queryParams = new List<string>();
        if (query.Page > 0) queryParams.Add($"page={query.Page}");
        if (query.PageSize > 0) queryParams.Add($"pageSize={query.PageSize}");

        var url = "/api/user-recipes/friends";
        if (queryParams.Any()) url += "?" + string.Join("&", queryParams);

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserRecipePagedResult>(_jsonOptions))!;
    }

    // User Friendships
    public async Task<FriendshipListDto> GetFriendshipsAsync()
    {
        await AddAuthHeaderAsync();
        return (await _httpClient.GetFromJsonAsync<FriendshipListDto>("/api/friendships", _jsonOptions))
            ?? new FriendshipListDto();
    }

    public async Task<List<FriendProfileDto>> GetFriendsAsync()
    {
        await AddAuthHeaderAsync();
        return (await _httpClient.GetFromJsonAsync<List<FriendProfileDto>>("/api/friendships/friends", _jsonOptions))
            ?? new List<FriendProfileDto>();
    }

    public async Task<UserFriendshipDto?> GetFriendshipAsync(Guid id)
    {
        await AddAuthHeaderAsync();
        return await _httpClient.GetFromJsonAsync<UserFriendshipDto>($"/api/friendships/{id}", _jsonOptions);
    }

    public async Task<UserFriendshipDto> SendFriendRequestAsync(SendFriendRequestDto dto)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("/api/friendships/request", dto, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserFriendshipDto>(_jsonOptions))!;
    }

    public async Task<UserFriendshipDto> RespondToFriendRequestAsync(Guid id, RespondFriendRequestDto dto)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync($"/api/friendships/{id}/respond", dto, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<UserFriendshipDto>(_jsonOptions))!;
    }

    public async Task RemoveFriendshipAsync(Guid id)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.DeleteAsync($"/api/friendships/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<UserSearchResultDto>> SearchUsersAsync(string query, int limit = 20)
    {
        await AddAuthHeaderAsync();
        var url = $"/api/friendships/search?query={Uri.EscapeDataString(query)}&limit={limit}";
        return (await _httpClient.GetFromJsonAsync<List<UserSearchResultDto>>(url, _jsonOptions))
            ?? new List<UserSearchResultDto>();
    }

    public async Task<FriendProfileDto?> GetUserProfileAsync(Guid userId)
    {
        await AddAuthHeaderAsync();
        return await _httpClient.GetFromJsonAsync<FriendProfileDto>($"/api/friendships/profile/{userId}", _jsonOptions);
    }

    // Activity Feed
    public async Task<ActivityFeedPagedResult> GetFeedAsync(ActivityFeedQuery query)
    {
        await AddAuthHeaderAsync();
        var queryParams = new List<string>();

        if (query.Types != null && query.Types.Count > 0)
        {
            foreach (var type in query.Types)
                queryParams.Add($"Types={Uri.EscapeDataString(type)}");
        }

        if (query.UserIds != null && query.UserIds.Count > 0)
        {
            foreach (var friendId in query.UserIds)
                queryParams.Add($"friendIds={friendId}");
        }

        queryParams.Add($"page={query.Page}");
        queryParams.Add($"pageSize={query.PageSize}");

        var url = queryParams.Count > 0
            ? $"/api/feed?{string.Join("&", queryParams)}"
            : "/api/feed";

        return (await _httpClient.GetFromJsonAsync<ActivityFeedPagedResult>(url, _jsonOptions))
            ?? new ActivityFeedPagedResult();
    }

    public async Task<ActivityFeedPagedResult> GetMyActivityAsync(int page = 1, int pageSize = 20)
    {
        await AddAuthHeaderAsync();
        var url = $"/api/feed/my-activity?page={page}&pageSize={pageSize}";
        return (await _httpClient.GetFromJsonAsync<ActivityFeedPagedResult>(url, _jsonOptions))
            ?? new ActivityFeedPagedResult();
    }

    public async Task<ActivitySummaryDto> GetActivitySummaryAsync()
    {
        await AddAuthHeaderAsync();
        return (await _httpClient.GetFromJsonAsync<ActivitySummaryDto>("/api/feed/summary", _jsonOptions))
            ?? new ActivitySummaryDto();
    }

    public async Task<List<UserRatingDto>> GetUserRatingsAsync(Guid userId, int skip = 0, int take = 50)
    {
        await AddAuthHeaderAsync();
        var url = $"/api/ratings/user/{userId}?skip={skip}&take={take}";
        return (await _httpClient.GetFromJsonAsync<List<UserRatingDto>>(url, _jsonOptions)) ?? new List<UserRatingDto>();
    }

    // Tags (Personal Recipe Organization)
    public async Task<List<TagDto>> GetMyTagsAsync()
    {
        await AddAuthHeaderAsync();
        return (await _httpClient.GetFromJsonAsync<List<TagDto>>("/api/tags", _jsonOptions))
            ?? new List<TagDto>();
    }

    public async Task<TagDto?> GetTagAsync(Guid id)
    {
        await AddAuthHeaderAsync();
        try
        {
            return await _httpClient.GetFromJsonAsync<TagDto>($"/api/tags/{id}", _jsonOptions);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    public async Task<TagDto> CreateTagAsync(CreateTagDto dto)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("/api/tags", dto, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TagDto>(_jsonOptions))!;
    }

    public async Task<TagDto> UpdateTagAsync(Guid id, UpdateTagDto dto)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PutAsJsonAsync($"/api/tags/{id}", dto, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<TagDto>(_jsonOptions))!;
    }

    public async Task DeleteTagAsync(Guid id)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.DeleteAsync($"/api/tags/{id}");
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<TagReferenceDto>> GetRecipeTagsAsync(Guid recipeId)
    {
        await AddAuthHeaderAsync();
        return (await _httpClient.GetFromJsonAsync<List<TagReferenceDto>>($"/api/tags/recipe/{recipeId}", _jsonOptions))
            ?? new List<TagReferenceDto>();
    }

    public async Task SetRecipeTagsAsync(Guid recipeId, List<Guid> tagIds)
    {
        await AddAuthHeaderAsync();
        var dto = new UpdateRecipeTagsDto { TagIds = tagIds };
        var response = await _httpClient.PutAsJsonAsync($"/api/tags/recipe/{recipeId}", dto, _jsonOptions);
        response.EnsureSuccessStatusCode();
    }

    // HelloFresh
    public async Task<HelloFreshRawResponse?> GetHelloFreshTestRawAsync()
    {
        return await _httpClient.GetFromJsonAsync<HelloFreshRawResponse>("/api/hellofresh/test-proxy", _jsonOptions);
    }

    public async Task<HelloFreshSyncResult> TriggerHelloFreshSyncAsync(bool force = false)
    {
        var url = force ? "/api/hellofresh/sync?force=true" : "/api/hellofresh/sync";
        var response = await _httpClient.PostAsync(url, null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<HelloFreshSyncResult>(_jsonOptions))
            ?? new HelloFreshSyncResult { Message = "Unknown result" };
    }

    public async Task<HelloFreshSyncStatus?> GetHelloFreshSyncStatusAsync()
    {
        return await _httpClient.GetFromJsonAsync<HelloFreshSyncStatus>("/api/hellofresh/sync-status", _jsonOptions);
    }

    public async Task<List<string>> GetAvailableHelloFreshWeeksAsync()
    {
        return (await _httpClient.GetFromJsonAsync<List<string>>("/api/hellofresh/weeks", _jsonOptions))
            ?? new List<string>();
    }
}
