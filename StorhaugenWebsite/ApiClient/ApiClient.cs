using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using StorhaugenWebsite.DTOs;
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

    // Household Methods
    public async Task<List<HouseholdDto>> GetMyHouseholdsAsync()
    {
        await AddAuthHeaderAsync();
        return (await _httpClient.GetFromJsonAsync<List<HouseholdDto>>("/api/households/my", _jsonOptions)) ?? new();
    }

    public async Task<HouseholdDto?> GetHouseholdAsync(Guid id)
    {
        await AddAuthHeaderAsync();
        return await _httpClient.GetFromJsonAsync<HouseholdDto>($"/api/households/{id}", _jsonOptions);
    }

    public async Task<HouseholdDto> CreateHouseholdAsync(CreateHouseholdDto dto)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("/api/households", dto, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<HouseholdDto>(_jsonOptions))!;
    }

    public async Task<HouseholdDto> UpdateHouseholdSettingsAsync(Guid id, UpdateHouseholdSettingsDto dto)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PutAsJsonAsync($"/api/households/{id}/settings", dto, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<HouseholdDto>(_jsonOptions))!;
    }

    public async Task<HouseholdDto> UpdateHouseholdNameAsync(Guid id, UpdateHouseholdDto dto)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PutAsJsonAsync($"/api/households/{id}", dto, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<HouseholdDto>(_jsonOptions))!;
    }

    public async Task<HouseholdDto> RegenerateHouseholdShareIdAsync(Guid id)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsync($"/api/households/{id}/regenerate-share-id", null);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<HouseholdDto>(_jsonOptions))!;
    }

    public async Task SwitchHouseholdAsync(Guid householdId)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsync($"/api/households/{householdId}/switch", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<HouseholdInviteDto>> GetPendingInvitesAsync()
    {
        await AddAuthHeaderAsync();
        return (await _httpClient.GetFromJsonAsync<List<HouseholdInviteDto>>("/api/households/invites/pending", _jsonOptions)) ?? new();
    }

    public async Task AcceptInviteAsync(Guid inviteId)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsync($"/api/households/invites/{inviteId}/accept", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task RejectInviteAsync(Guid inviteId)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsync($"/api/households/invites/{inviteId}/reject", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task InviteToHouseholdAsync(Guid householdId, InviteToHouseholdDto dto)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync($"/api/households/{householdId}/invites", dto, _jsonOptions);
        response.EnsureSuccessStatusCode();
    }

    public async Task LeaveHouseholdAsync(Guid householdId)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsync($"/api/households/{householdId}/leave", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task<List<HouseholdSearchResultDto>> SearchHouseholdsAsync(string query)
    {
        await AddAuthHeaderAsync();
        var results = await _httpClient.GetFromJsonAsync<List<HouseholdSearchResultDto>>($"/api/households/search?query={Uri.EscapeDataString(query)}", _jsonOptions);
        return results ?? new List<HouseholdSearchResultDto>();
    }

    public async Task<List<HouseholdFriendshipDto>> GetHouseholdFriendshipsAsync()
    {
        await AddAuthHeaderAsync();
        var results = await _httpClient.GetFromJsonAsync<List<HouseholdFriendshipDto>>("/api/household-friendships", _jsonOptions);
        return results ?? new List<HouseholdFriendshipDto>();
    }

    public async Task<HouseholdFriendshipDto> SendHouseholdFriendRequestAsync(SendFriendRequestDto dto)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("/api/household-friendships/request", dto, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<HouseholdFriendshipDto>(_jsonOptions))!;
    }

    public async Task<HouseholdFriendshipDto> RespondHouseholdFriendRequestAsync(Guid requestId, RespondFriendRequestDto dto)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync($"/api/household-friendships/{requestId}/respond", dto, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<HouseholdFriendshipDto>(_jsonOptions))!;
    }

    // Household Recipe Methods
    public async Task<List<HouseholdRecipeDto>> GetRecipesAsync(bool includeArchived = false)
    {
        await AddAuthHeaderAsync();
        var url = $"/api/household-recipes?includeArchived={includeArchived}";
        return (await _httpClient.GetFromJsonAsync<List<HouseholdRecipeDto>>(url, _jsonOptions)) ?? new();
    }

    public async Task<HouseholdRecipeDto?> GetRecipeAsync(Guid id)
    {
        await AddAuthHeaderAsync();
        return await _httpClient.GetFromJsonAsync<HouseholdRecipeDto>($"/api/household-recipes/{id}", _jsonOptions);
    }

    public async Task<HouseholdRecipeDto> CreateRecipeAsync(CreateHouseholdRecipeDto dto)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync("/api/household-recipes", dto, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<HouseholdRecipeDto>(_jsonOptions))!;
    }

    public async Task<HouseholdRecipeDto> UpdateRecipeAsync(Guid id, UpdateHouseholdRecipeDto dto)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PutAsJsonAsync($"/api/household-recipes/{id}", dto, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<HouseholdRecipeDto>(_jsonOptions))!;
    }

    public async Task ArchiveRecipeAsync(Guid id)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsync($"/api/household-recipes/{id}/archive", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task RestoreRecipeAsync(Guid id)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsync($"/api/household-recipes/{id}/restore", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task RateRecipeAsync(Guid id, int rating)
    {
        await AddAuthHeaderAsync();
        var dto = new RateRecipeDto { Rating = rating };
        var response = await _httpClient.PostAsJsonAsync($"/api/household-recipes/{id}/rate", dto, _jsonOptions);
        response.EnsureSuccessStatusCode();
    }

    public async Task ForkRecipeAsync(Guid id)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsync($"/api/household-recipes/{id}/fork", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteRecipeAsync(Guid id)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.DeleteAsync($"/api/household-recipes/{id}");
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

    // Public Household Recipes (community recipes)
    public async Task<PublicRecipePagedResult> BrowsePublicRecipesAsync(BrowsePublicRecipesQuery query)
    {
        var queryParams = new List<string>();

        if (!string.IsNullOrWhiteSpace(query.Search))
            queryParams.Add($"search={Uri.EscapeDataString(query.Search)}");

        queryParams.Add($"sortBy={Uri.EscapeDataString(query.SortBy)}");
        queryParams.Add($"page={query.Page}");
        queryParams.Add($"pageSize={query.PageSize}");

        var url = $"/api/household-recipes/public?{string.Join("&", queryParams)}";

        return (await _httpClient.GetFromJsonAsync<PublicRecipePagedResult>(url, _jsonOptions))
            ?? new PublicRecipePagedResult();
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
}
