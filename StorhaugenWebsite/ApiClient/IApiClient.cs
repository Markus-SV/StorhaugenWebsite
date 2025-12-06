using StorhaugenWebsite.DTOs;

namespace StorhaugenWebsite.ApiClient;

public interface IApiClient
{
    // User
    Task<UserDto?> GetMyProfileAsync();
    Task<UserDto> UpdateMyProfileAsync(UpdateUserDto dto);

    // Households
    Task<List<HouseholdDto>> GetMyHouseholdsAsync();
    Task<HouseholdDto?> GetHouseholdAsync(int id);
    Task<HouseholdDto> CreateHouseholdAsync(CreateHouseholdDto dto);
    Task SwitchHouseholdAsync(int householdId);
    Task<List<HouseholdInviteDto>> GetPendingInvitesAsync();
    Task AcceptInviteAsync(int inviteId);
    Task InviteToHouseholdAsync(int householdId, InviteToHouseholdDto dto);

    // Household Recipes
    Task<List<HouseholdRecipeDto>> GetRecipesAsync(bool includeArchived = false);
    Task<HouseholdRecipeDto?> GetRecipeAsync(int id);
    Task<HouseholdRecipeDto> CreateRecipeAsync(CreateHouseholdRecipeDto dto);
    Task<HouseholdRecipeDto> UpdateRecipeAsync(int id, UpdateHouseholdRecipeDto dto);
    Task ArchiveRecipeAsync(int id);
    Task RestoreRecipeAsync(int id);
    Task RateRecipeAsync(int id, int rating);
    Task ForkRecipeAsync(int id);
    Task DeleteRecipeAsync(int id);

    // Global Recipes
    Task<GlobalRecipePagedResult> BrowseGlobalRecipesAsync(BrowseGlobalRecipesQuery query);
    Task<GlobalRecipeDto?> GetGlobalRecipeAsync(int id);
    Task<List<GlobalRecipeDto>> SearchGlobalRecipesAsync(string query, int limit = 20);

    // Storage
    Task<UploadImageResultDto> UploadImageAsync(byte[] imageData, string fileName);
    Task DeleteImageAsync(string fileName);
}
