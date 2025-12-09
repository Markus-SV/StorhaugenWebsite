using StorhaugenEats.API.DTOs;

namespace StorhaugenEats.API.Services;

/// <summary>
/// Service for managing user-owned recipes.
/// </summary>
public interface IUserRecipeService
{
    // CRUD Operations
    Task<UserRecipePagedResult> GetUserRecipesAsync(Guid userId, GetUserRecipesQuery query);
    Task<UserRecipeDto?> GetRecipeAsync(Guid recipeId, Guid requestingUserId);
    Task<UserRecipeDto> CreateRecipeAsync(Guid userId, CreateUserRecipeDto dto);
    Task<UserRecipeDto> UpdateRecipeAsync(Guid recipeId, Guid userId, UpdateUserRecipeDto dto);
    Task DeleteRecipeAsync(Guid recipeId, Guid userId);

    // Publishing
    Task<PublishRecipeResultDto> PublishRecipeAsync(Guid recipeId, Guid userId);
    Task<UserRecipeDto> DetachRecipeAsync(Guid recipeId, Guid userId);

    // Rating
    Task<UserRecipeDto> RateRecipeAsync(Guid recipeId, Guid userId, int rating, string? comment = null);
    Task RemoveRatingAsync(Guid recipeId, Guid userId);

    // Archive
    Task<UserRecipeDto> ArchiveRecipeAsync(Guid recipeId, Guid userId);
    Task<UserRecipeDto> RestoreRecipeAsync(Guid recipeId, Guid userId);

    // Visibility checks
    Task<bool> CanUserViewRecipeAsync(Guid recipeId, Guid requestingUserId);

    // Household aggregation
    Task<AggregatedRecipePagedResult> GetHouseholdRecipesAsync(Guid householdId, Guid requestingUserId, GetCombinedRecipesQuery query);
    Task<List<CommonFavoriteDto>> GetCommonFavoritesAsync(Guid householdId, Guid requestingUserId, GetCommonFavoritesQuery query);
}
