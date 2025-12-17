using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;
using StorhaugenEats.API.Models;
using StorhaugenWebsite.Shared.DTOs;
using Supabase.Gotrue;

namespace StorhaugenEats.API.Services;

public class RatingService : IRatingService
{
    private readonly AppDbContext _context;

    public RatingService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Rating?> GetUserRatingForRecipeAsync(Guid userId, Guid globalRecipeId)
    {
        return await _context.Ratings
            .FirstOrDefaultAsync(r => r.UserId == userId && r.GlobalRecipeId == globalRecipeId);
    }

    public async Task<IEnumerable<Rating>> GetRatingsForRecipeAsync(Guid globalRecipeId)
    {
        return await _context.Ratings
            .Include(r => r.User)
            .Where(r => r.GlobalRecipeId == globalRecipeId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<Rating> UpsertRatingAsync(Guid userId, Guid globalRecipeId, decimal score, string? comment = null)
    {
        if (score < 0 || score > 10)
            throw new ArgumentException("Score must be between 0 and 10");

        var existing = await GetUserRatingForRecipeAsync(userId, globalRecipeId);

        if (existing != null)
        {
            existing.Score = score;
            existing.Comment = comment;
            existing.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return existing;
        }

        var rating = new Rating
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GlobalRecipeId = globalRecipeId,
            Score = score,
            Comment = comment,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Ratings.Add(rating);
        await _context.SaveChangesAsync();

        // Note: The database trigger will automatically update global_recipes.average_rating

        return rating;
    }

    public async Task<bool> DeleteRatingAsync(Guid userId, Guid globalRecipeId)
    {
        var rating = await GetUserRatingForRecipeAsync(userId, globalRecipeId);
        if (rating == null) return false;

        _context.Ratings.Remove(rating);
        await _context.SaveChangesAsync();

        // Note: The database trigger will automatically update global_recipes.average_rating

        return true;
    }

    public async Task<List<UserRatingDto>> GetGlobalRecipeRatingsForUserAsync(Guid userId, int skip = 0, int take = 50)
    {
        return await _context.Ratings
            .AsNoTracking()
           .Where(r => r.UserId == userId && r.GlobalRecipeId != null)
            .OrderByDescending(r => r.UpdatedAt)
            .Skip(skip)
            .Take(take)
            .Select(r => new UserRatingDto
            {
                GlobalRecipeId = r.GlobalRecipeId!.Value,
                RecipeTitle = r.GlobalRecipe!.Title,
                ImageUrl = r.GlobalRecipe!.ImageUrl,
                Score = r.Score,
                Comment = r.Comment,
                RatedAt = r.UpdatedAt
            })
            .ToListAsync();
    }

}
