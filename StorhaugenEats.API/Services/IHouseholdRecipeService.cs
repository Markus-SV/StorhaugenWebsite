using StorhaugenEats.API.Models;

namespace StorhaugenEats.API.Services;

public interface IHouseholdRecipeService
{
    Task<HouseholdRecipe?> GetByIdAsync(Guid id);
    Task<IEnumerable<HouseholdRecipe>> GetByHouseholdAsync(Guid householdId, bool includeArchived = false);
    Task<HouseholdRecipe> AddLinkedRecipeAsync(Guid householdId, Guid globalRecipeId, Guid addedByUserId, string? personalNotes = null);
    Task<HouseholdRecipe> AddForkedRecipeAsync(Guid householdId, Guid addedByUserId, string title, string? description, string ingredients, string? imageUrl, string? personalNotes = null);
    Task<HouseholdRecipe> UpdateAsync(HouseholdRecipe recipe);
    Task<HouseholdRecipe> ForkRecipeAsync(Guid householdRecipeId); // Convert linked → forked
    Task<bool> ArchiveAsync(Guid id);
    Task<bool> UnarchiveAsync(Guid id);
    Task<bool> DeleteAsync(Guid id);
}
