using StorhaugenEats.API.Models;

namespace StorhaugenEats.API.Services;

/// <summary>
/// DEPRECATED: Use IUserRecipeService for user-centric recipe management.
/// This interface is maintained for backward compatibility during migration.
/// </summary>
[Obsolete("Use IUserRecipeService instead. This interface will be removed after migration is complete.")]
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
