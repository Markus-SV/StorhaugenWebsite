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

    public async Task<HouseholdDto?> GetHouseholdAsync(int id)
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

    public async Task SwitchHouseholdAsync(int householdId)
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

    public async Task AcceptInviteAsync(int inviteId)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsync($"/api/households/invites/{inviteId}/accept", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task InviteToHouseholdAsync(int householdId, InviteToHouseholdDto dto)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsJsonAsync($"/api/households/{householdId}/invites", dto, _jsonOptions);
        response.EnsureSuccessStatusCode();
    }

    public async Task LeaveHouseholdAsync(int householdId)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsync($"/api/households/{householdId}/leave", null);
        response.EnsureSuccessStatusCode();
    }

    // Household Recipe Methods
    public async Task<List<HouseholdRecipeDto>> GetRecipesAsync(bool includeArchived = false)
    {
        await AddAuthHeaderAsync();
        var url = $"/api/household-recipes?includeArchived={includeArchived}";
        return (await _httpClient.GetFromJsonAsync<List<HouseholdRecipeDto>>(url, _jsonOptions)) ?? new();
    }

    public async Task<HouseholdRecipeDto?> GetRecipeAsync(int id)
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

    public async Task<HouseholdRecipeDto> UpdateRecipeAsync(int id, UpdateHouseholdRecipeDto dto)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PutAsJsonAsync($"/api/household-recipes/{id}", dto, _jsonOptions);
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<HouseholdRecipeDto>(_jsonOptions))!;
    }

    public async Task ArchiveRecipeAsync(int id)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsync($"/api/household-recipes/{id}/archive", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task RestoreRecipeAsync(int id)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsync($"/api/household-recipes/{id}/restore", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task RateRecipeAsync(int id, int rating)
    {
        await AddAuthHeaderAsync();
        var dto = new RateRecipeDto { Rating = rating };
        var response = await _httpClient.PostAsJsonAsync($"/api/household-recipes/{id}/rate", dto, _jsonOptions);
        response.EnsureSuccessStatusCode();
    }

    public async Task ForkRecipeAsync(int id)
    {
        await AddAuthHeaderAsync();
        var response = await _httpClient.PostAsync($"/api/household-recipes/{id}/fork", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task DeleteRecipeAsync(int id)
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

    public async Task<GlobalRecipeDto?> GetGlobalRecipeAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<GlobalRecipeDto>($"/api/global-recipes/{id}", _jsonOptions);
    }

    public async Task<List<GlobalRecipeDto>> SearchGlobalRecipesAsync(string query, int limit = 20)
    {
        var url = $"/api/global-recipes/search?q={Uri.EscapeDataString(query)}&limit={limit}";
        return (await _httpClient.GetFromJsonAsync<List<GlobalRecipeDto>>(url, _jsonOptions)) ?? new();
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
