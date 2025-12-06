using StorhaugenWebsite.DTOs;

namespace StorhaugenWebsite.ApiClient;

public interface IApiClient
{
    // User
    Task<UserDto?> GetMyProfileAsync();
    Task<UserDto> UpdateMyProfileAsync(UpdateUserDto dto);

    // Households
    Task<List<HouseholdDto>> GetMyHouseholdsAsync();
    Task<HouseholdDto?> GetHouseholdAsync(Guid id);
    Task<HouseholdDto> CreateHouseholdAsync(CreateHouseholdDto dto);
    Task SwitchHouseholdAsync(Guid householdId);
    Task<List<HouseholdInviteDto>> GetPendingInvitesAsync();
    Task AcceptInviteAsync(Guid inviteId);
    Task InviteToHouseholdAsync(Guid householdId, InviteToHouseholdDto dto);
    Task LeaveHouseholdAsync(Guid householdId);

    // Household Recipes
    Task<List<HouseholdRecipeDto>> GetRecipesAsync(bool includeArchived = false);
    Task<HouseholdRecipeDto?> GetRecipeAsync(Guid id);
    Task<HouseholdRecipeDto> CreateRecipeAsync(CreateHouseholdRecipeDto dto);
    Task<HouseholdRecipeDto> UpdateRecipeAsync(Guid id, UpdateHouseholdRecipeDto dto);
    Task ArchiveRecipeAsync(Guid id);
    Task RestoreRecipeAsync(Guid id);
    Task RateRecipeAsync(Guid id, int rating);
    Task ForkRecipeAsync(Guid id);
    Task DeleteRecipeAsync(Guid id);

    // Global Recipes
    Task<GlobalRecipePagedResult> BrowseGlobalRecipesAsync(BrowseGlobalRecipesQuery query);
    Task<GlobalRecipeDto?> GetGlobalRecipeAsync(Guid id);
    Task<List<GlobalRecipeDto>> SearchGlobalRecipesAsync(string query, int limit = 20);

    // Storage
    Task<UploadImageResultDto> UploadImageAsync(byte[] imageData, string fileName);
    Task DeleteImageAsync(string fileName);
}
