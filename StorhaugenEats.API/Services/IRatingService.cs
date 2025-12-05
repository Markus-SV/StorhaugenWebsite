using StorhaugenEats.API.Models;

namespace StorhaugenEats.API.Services;

public interface IRatingService
{
    Task<Rating?> GetUserRatingForRecipeAsync(Guid userId, Guid globalRecipeId);
    Task<IEnumerable<Rating>> GetRatingsForRecipeAsync(Guid globalRecipeId);
    Task<Rating> UpsertRatingAsync(Guid userId, Guid globalRecipeId, int score, string? comment = null);
    Task<bool> DeleteRatingAsync(Guid userId, Guid globalRecipeId);
}
