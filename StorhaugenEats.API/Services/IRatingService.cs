using StorhaugenEats.API.Models;
using StorhaugenWebsite.Shared.DTOs;

namespace StorhaugenEats.API.Services;

public interface IRatingService
{
    Task<Rating?> GetUserRatingForRecipeAsync(Guid userId, Guid globalRecipeId);
    Task<IEnumerable<Rating>> GetRatingsForRecipeAsync(Guid globalRecipeId);
    Task<Rating> UpsertRatingAsync(Guid userId, Guid globalRecipeId, int score, string? comment = null);
    Task<bool> DeleteRatingAsync(Guid userId, Guid globalRecipeId);
    Task<List<UserRatingDto>> GetGlobalRecipeRatingsForUserAsync(Guid userId, int skip = 0, int take = 50);

}
