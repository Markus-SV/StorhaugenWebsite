using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;
using StorhaugenEats.API.Models;

namespace StorhaugenEats.API.Services;

/// <summary>
/// DEPRECATED: Use UserRecipeService for user-centric recipe management.
/// This service is maintained for backward compatibility during migration.
/// </summary>
[Obsolete("Use UserRecipeService instead. This service will be removed after migration is complete.")]
public class HouseholdRecipeService : IHouseholdRecipeService
{
    private readonly AppDbContext _context;

    public HouseholdRecipeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<HouseholdRecipe?> GetByIdAsync(Guid id)
    {
        return await _context.HouseholdRecipes
            .Include(hr => hr.GlobalRecipe)
            .Include(hr => hr.Household)
            .Include(hr => hr.AddedByUser)
            .FirstOrDefaultAsync(hr => hr.Id == id);
    }

    public async Task<IEnumerable<HouseholdRecipe>> GetByHouseholdAsync(Guid householdId, bool includeArchived = false)
    {
        var query = _context.HouseholdRecipes
            .Include(hr => hr.GlobalRecipe)
            .Include(hr => hr.AddedByUser)
            .Where(hr => hr.HouseholdId == householdId);

        if (!includeArchived)
        {
            query = query.Where(hr => !hr.IsArchived);
        }

        return await query.OrderByDescending(hr => hr.CreatedAt).ToListAsync();
    }

    public async Task<HouseholdRecipe> AddLinkedRecipeAsync(Guid householdId, Guid globalRecipeId, Guid addedByUserId, string? personalNotes = null)
    {
        // Check if already exists (prevent duplicates)
        var existing = await _context.HouseholdRecipes
            .FirstOrDefaultAsync(hr => hr.HouseholdId == householdId && hr.GlobalRecipeId == globalRecipeId);

        if (existing != null)
        {
            // Unarchive if was archived
            if (existing.IsArchived)
            {
                existing.IsArchived = false;
                existing.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }
            return existing;
        }

        var recipe = new HouseholdRecipe
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            GlobalRecipeId = globalRecipeId,
            PersonalNotes = personalNotes,
            AddedByUserId = addedByUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.HouseholdRecipes.Add(recipe);
        await _context.SaveChangesAsync();

        return await GetByIdAsync(recipe.Id) ?? recipe;
    }

    public async Task<HouseholdRecipe> AddForkedRecipeAsync(Guid householdId, Guid addedByUserId, string title, string? description, string ingredients, string? imageUrl, string? personalNotes = null)
    {
        var recipe = new HouseholdRecipe
        {
            Id = Guid.NewGuid(),
            HouseholdId = householdId,
            GlobalRecipeId = null, // Forked = no global link
            LocalTitle = title,
            LocalDescription = description,
            LocalIngredients = ingredients,
            LocalImageUrl = imageUrl,
            PersonalNotes = personalNotes,
            AddedByUserId = addedByUserId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.HouseholdRecipes.Add(recipe);
        await _context.SaveChangesAsync();

        return recipe;
    }

    public async Task<HouseholdRecipe> UpdateAsync(HouseholdRecipe recipe)
    {
        recipe.UpdatedAt = DateTime.UtcNow;
        _context.HouseholdRecipes.Update(recipe);
        await _context.SaveChangesAsync();
        return recipe;
    }

    public async Task<HouseholdRecipe> ForkRecipeAsync(Guid householdRecipeId)
    {
        var recipe = await GetByIdAsync(householdRecipeId);
        if (recipe == null || recipe.GlobalRecipe == null)
            throw new InvalidOperationException("Recipe not found or already forked");

        // Copy global data to local fields
        recipe.LocalTitle = recipe.GlobalRecipe.Title;
        recipe.LocalDescription = recipe.GlobalRecipe.Description;
        recipe.LocalIngredients = recipe.GlobalRecipe.Ingredients;
        recipe.LocalImageUrl = recipe.GlobalRecipe.ImageUrl;

        // Break the link
        recipe.GlobalRecipeId = null;
        recipe.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return recipe;
    }

    public async Task<bool> ArchiveAsync(Guid id)
    {
        var recipe = await _context.HouseholdRecipes.FindAsync(id);
        if (recipe == null) return false;

        recipe.IsArchived = true;
        recipe.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UnarchiveAsync(Guid id)
    {
        var recipe = await _context.HouseholdRecipes.FindAsync(id);
        if (recipe == null) return false;

        recipe.IsArchived = false;
        recipe.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var recipe = await _context.HouseholdRecipes.FindAsync(id);
        if (recipe == null) return false;

        _context.HouseholdRecipes.Remove(recipe);
        await _context.SaveChangesAsync();

        return true;
    }
}
