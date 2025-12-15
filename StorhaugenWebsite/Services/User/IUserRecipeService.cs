using StorhaugenWebsite.Shared.DTOs;

namespace StorhaugenWebsite.Services
{
    public interface IUserRecipeService
    {
        // Recipe CRUD
        Task<UserRecipePagedResult> GetMyRecipesAsync(GetUserRecipesQuery? query = null);
        Task<UserRecipeDto?> GetRecipeAsync(Guid id);
        Task<UserRecipeDto> CreateRecipeAsync(CreateUserRecipeDto dto);
        Task<UserRecipeDto> UpdateRecipeAsync(Guid id, UpdateUserRecipeDto dto);
        Task DeleteRecipeAsync(Guid id);

        // Publishing
        Task<PublishRecipeResultDto> PublishRecipeAsync(Guid id);
        Task<UserRecipeDto> DetachRecipeAsync(Guid id);

        // Rating
        Task<UserRecipeDto> RateRecipeAsync(Guid id, int rating, string? comment = null);
        Task RemoveRatingAsync(Guid id);

        // Archiving
        Task<UserRecipeDto> ArchiveRecipeAsync(Guid id);
        Task<UserRecipeDto> RestoreRecipeAsync(Guid id);

        // Friends' Recipes
        Task<UserRecipePagedResult> GetFriendsRecipesAsync(GetUserRecipesQuery? query = null);

        // Cache
        List<UserRecipeDto> CachedRecipes { get; }
        void InvalidateCache();
    }
}
