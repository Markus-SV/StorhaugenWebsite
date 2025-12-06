using StorhaugenEats.API.Models;

namespace StorhaugenEats.API.Services;

public interface IGlobalRecipeService
{
    Task<GlobalRecipe?> GetByIdAsync(Guid id);
    Task<GlobalRecipe?> GetByHellofreshUuidAsync(string uuid);
    Task<IEnumerable<GlobalRecipe>> GetPublicRecipesAsync(int skip = 0, int take = 50, string? sortBy = "rating");
    Task<GlobalRecipe> CreateAsync(GlobalRecipe recipe);
    Task<GlobalRecipe> UpdateAsync(GlobalRecipe recipe);
    Task<bool> DeleteAsync(Guid id);
    Task UpsertHellofreshRecipeAsync(GlobalRecipe recipe);
    Task<Dictionary<string, GlobalRecipe>> GetHellofreshRecipesByUuidsAsync(IEnumerable<string> uuids);
    Task BatchUpsertHellofreshRecipesAsync(IEnumerable<GlobalRecipe> recipes);
}
