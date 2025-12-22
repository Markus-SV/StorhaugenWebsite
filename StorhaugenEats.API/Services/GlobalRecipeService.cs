using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;
using StorhaugenEats.API.Models;

namespace StorhaugenEats.API.Services;

public class GlobalRecipeService : IGlobalRecipeService
{
    private readonly AppDbContext _context;

    public GlobalRecipeService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<GlobalRecipe?> GetByIdAsync(Guid id)
    {
        return await _context.GlobalRecipes
            .Include(gr => gr.Ratings)
            .FirstOrDefaultAsync(gr => gr.Id == id);
    }

    public async Task<GlobalRecipe?> GetByHellofreshUuidAsync(string uuid)
    {
        return await _context.GlobalRecipes
            .FirstOrDefaultAsync(gr => gr.HellofreshUuid == uuid);
    }

    public async Task<IEnumerable<GlobalRecipe>> GetPublicRecipesAsync(int skip = 0, int take = 50, string? sortBy = "rating")
    {
        var query = _context.GlobalRecipes
            .Where(gr => gr.IsHellofresh || gr.IsPublic);

        query = sortBy?.ToLower() switch
        {
            "rating" => query.OrderByDescending(gr => gr.AverageRating).ThenByDescending(gr => gr.RatingCount),
            "date" => query.OrderByDescending(gr => gr.CreatedAt),
            "title" => query.OrderBy(gr => gr.Title),
            _ => query.OrderByDescending(gr => gr.AverageRating)
        };

        return await query
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<GlobalRecipe> CreateAsync(GlobalRecipe recipe)
    {
        recipe.CreatedAt = DateTime.UtcNow;
        recipe.UpdatedAt = DateTime.UtcNow;
        _context.GlobalRecipes.Add(recipe);
        await _context.SaveChangesAsync();
        return recipe;
    }

    public async Task<GlobalRecipe> UpdateAsync(GlobalRecipe recipe)
    {
        recipe.UpdatedAt = DateTime.UtcNow;
        _context.GlobalRecipes.Update(recipe);
        await _context.SaveChangesAsync();
        return recipe;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var recipe = await _context.GlobalRecipes.FindAsync(id);
        if (recipe == null) return false;

        _context.GlobalRecipes.Remove(recipe);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task UpsertHellofreshRecipeAsync(GlobalRecipe recipe)
    {
        var existing = await GetByHellofreshUuidAsync(recipe.HellofreshUuid!);

        if (existing != null)
        {
            // Update existing
            existing.Title = recipe.Title;
            existing.Description = recipe.Description;
            existing.ImageUrl = null;
            existing.ImageUrls = "[]";
            existing.Ingredients = recipe.Ingredients;
            existing.NutritionData = recipe.NutritionData;
            existing.CookTimeMinutes = recipe.CookTimeMinutes;
            existing.Difficulty = recipe.Difficulty;
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
        else
        {
            // Insert new
            recipe.IsHellofresh = true;
            recipe.ImageUrl = null;
            recipe.ImageUrls = "[]";
            recipe.CreatedAt = DateTime.UtcNow;
            recipe.UpdatedAt = DateTime.UtcNow;
            _context.GlobalRecipes.Add(recipe);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Dictionary<string, GlobalRecipe>> GetHellofreshRecipesByUuidsAsync(IEnumerable<string> uuids)
    {
        var recipes = await _context.GlobalRecipes
            .Where(gr => gr.IsHellofresh && uuids.Contains(gr.HellofreshUuid!))
            .ToListAsync();

        return recipes.ToDictionary(r => r.HellofreshUuid!, r => r);
    }

    public async Task BatchUpsertHellofreshRecipesAsync(IEnumerable<GlobalRecipe> recipes)
    {
        var recipeList = recipes.ToList();
        if (!recipeList.Any()) return;

        // Get all UUIDs from incoming recipes
        var uuids = recipeList.Select(r => r.HellofreshUuid!).ToList();

        // Fetch all existing recipes in one query
        var existingRecipes = await GetHellofreshRecipesByUuidsAsync(uuids);

        var toAdd = new List<GlobalRecipe>();
        var toUpdate = new List<GlobalRecipe>();

        foreach (var recipe in recipeList)
        {
            if (existingRecipes.TryGetValue(recipe.HellofreshUuid!, out var existing))
            {
                // Update existing
                existing.Title = recipe.Title;
                existing.Description = recipe.Description;
                existing.ImageUrl = recipe.ImageUrl;
                existing.Ingredients = recipe.Ingredients;
                existing.NutritionData = recipe.NutritionData;
                existing.CookTimeMinutes = recipe.CookTimeMinutes;
                existing.Difficulty = recipe.Difficulty;
                existing.UpdatedAt = DateTime.UtcNow;
                toUpdate.Add(existing);
            }
            else
            {
                // Insert new
                recipe.IsHellofresh = true;
                recipe.CreatedAt = DateTime.UtcNow;
                recipe.UpdatedAt = DateTime.UtcNow;
                toAdd.Add(recipe);
            }
        }

        // Batch add new recipes
        if (toAdd.Any())
        {
            await _context.GlobalRecipes.AddRangeAsync(toAdd);
        }

        // Save all changes in one transaction
        await _context.SaveChangesAsync();
    }
}
