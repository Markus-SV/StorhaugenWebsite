using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;
using StorhaugenEats.API.DTOs;
using StorhaugenEats.API.Models;
using StorhaugenEats.API.Services;
using StorhaugenEats.API.Helpers;

namespace StorhaugenEats.API.Controllers;

[ApiController]
[Route("api/household-recipes")]
[Authorize]
public class HouseholdRecipesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public HouseholdRecipesController(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Get all recipes for the current household (optionally include archived)
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<HouseholdRecipeDto>>> GetRecipes([FromQuery] bool includeArchived = false)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var user = await _context.Users.FindAsync(userId);

        if (user?.CurrentHouseholdId == null)
            return BadRequest(new { message = "You must select a household first" });

        var query = _context.HouseholdRecipes
            .Include(hr => hr.GlobalRecipe)
            .Include(hr => hr.AddedByUser)
            .Include(hr => hr.ArchivedBy)
            .Include(hr => hr.Ratings).ThenInclude(r => r.User)
            .Where(hr => hr.HouseholdId == user.CurrentHouseholdId);

        if (!includeArchived)
            query = query.Where(hr => !hr.IsArchived);

        var recipes = await query
            .OrderByDescending(hr => hr.CreatedAt)
            .ToListAsync();

        var recipeDtos = recipes.Select(hr => MapToDto(hr)).ToList();

        return Ok(recipeDtos);
    }

    /// <summary>
    /// Get a specific recipe by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<HouseholdRecipeDto>> GetRecipe(Guid id)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var user = await _context.Users.FindAsync(userId);

        if (user?.CurrentHouseholdId == null)
            return BadRequest(new { message = "You must select a household first" });

        var recipe = await _context.HouseholdRecipes
            .Include(hr => hr.GlobalRecipe)
            .Include(hr => hr.AddedByUser)
            .Include(hr => hr.ArchivedBy)
            .Include(hr => hr.Ratings).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(hr => hr.Id == id && hr.HouseholdId == user.CurrentHouseholdId);

        if (recipe == null)
            return NotFound();

        return Ok(MapToDto(recipe));
    }

    /// <summary>
    /// Add a new recipe to the household (custom or from global)
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<HouseholdRecipeDto>> CreateRecipe([FromBody] CreateHouseholdRecipeDto dto)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var user = await _context.Users.FindAsync(userId);

        if (user?.CurrentHouseholdId == null)
            return BadRequest(new { message = "You must select a household first" });

        HouseholdRecipe recipe;

        if (dto.GlobalRecipeId.HasValue)
        {
            // Adding from global recipe
            var globalRecipe = await _context.GlobalRecipes.FindAsync(dto.GlobalRecipeId.Value);
            if (globalRecipe == null)
                return NotFound(new { message = "Global recipe not found" });

            recipe = new HouseholdRecipe
            {
                HouseholdId = user.CurrentHouseholdId.Value,
                GlobalRecipeId = dto.Fork ? null : dto.GlobalRecipeId.Value,
                PersonalNotes = dto.PersonalNotes,
                AddedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsArchived = false
            };

            if (dto.Fork)
            {
                // Fork: Copy data from global recipe
                recipe.LocalTitle = dto.Name ?? globalRecipe.Title;
                recipe.LocalDescription = dto.Description ?? globalRecipe.Description;
                recipe.LocalImageUrl = dto.ImageUrls?.Count > 0 ? dto.ImageUrls[0] : (dto.ImageUrl ?? globalRecipe.ImageUrl);
            }
            else
            {
                // Link: Use global recipe data, only store personal notes
                recipe.LocalTitle = null; // Will use global recipe title
                recipe.LocalDescription = null;
                recipe.LocalImageUrl = null;
            }

            // Increment global recipe counter
            globalRecipe.TotalTimesAdded++;
            globalRecipe.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Custom recipe
            recipe = new HouseholdRecipe
            {
                HouseholdId = user.CurrentHouseholdId.Value,
                LocalTitle = dto.Name,
                LocalDescription = dto.Description,
                LocalImageUrl = dto.ImageUrls?.Count > 0 ? dto.ImageUrls[0] : dto.ImageUrl,
                PersonalNotes = dto.PersonalNotes,
                AddedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsArchived = false,
                GlobalRecipeId = null
            };
        }

        _context.HouseholdRecipes.Add(recipe);
        await _context.SaveChangesAsync();

        // Reload with includes
        recipe = await _context.HouseholdRecipes
            .Include(hr => hr.GlobalRecipe)
            .Include(hr => hr.AddedByUser)
            .Include(hr => hr.Ratings).ThenInclude(r => r.User)
            .FirstAsync(hr => hr.Id == recipe.Id);

        return CreatedAtAction(nameof(GetRecipe), new { id = recipe.Id }, MapToDto(recipe));
    }

    /// <summary>
    /// Update a recipe (name, description, notes, images)
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<HouseholdRecipeDto>> UpdateRecipe(Guid id, [FromBody] UpdateHouseholdRecipeDto dto)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var user = await _context.Users.FindAsync(userId);

        if (user?.CurrentHouseholdId == null)
            return BadRequest(new { message = "You must select a household first" });

        var recipe = await _context.HouseholdRecipes
            .Include(hr => hr.GlobalRecipe)
            .Include(hr => hr.AddedByUser)
            .Include(hr => hr.ArchivedBy)
            .Include(hr => hr.Ratings).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(hr => hr.Id == id && hr.HouseholdId == user.CurrentHouseholdId);

        if (recipe == null)
            return NotFound();

        // Only update fields that are provided
        if (dto.Name != null)
        {
            // Can only update name if recipe is forked or custom (not linked to global)
            if (recipe.GlobalRecipeId == null)
                recipe.LocalTitle = dto.Name;
            else
                return BadRequest(new { message = "Cannot update name of linked recipe. Fork it first." });
        }

        if (dto.Description != null)
        {
            if (recipe.GlobalRecipeId == null)
                recipe.LocalDescription = dto.Description;
            else
                return BadRequest(new { message = "Cannot update description of linked recipe. Fork it first." });
        }

        if (dto.ImageUrls != null)
        {
            if (recipe.GlobalRecipeId == null)
                recipe.LocalImageUrl = dto.ImageUrls.Count > 0 ? dto.ImageUrls[0] : null;
            else
                return BadRequest(new { message = "Cannot update images of linked recipe. Fork it first." });
        }

        if (dto.PersonalNotes != null)
        {
            // Personal notes can always be updated
            recipe.PersonalNotes = dto.PersonalNotes;
        }

        await _context.SaveChangesAsync();

        return Ok(MapToDto(recipe));
    }

    /// <summary>
    /// Archive a recipe
    /// </summary>
    [HttpPost("{id}/archive")]
    public async Task<IActionResult> ArchiveRecipe(Guid id)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var user = await _context.Users.FindAsync(userId);

        if (user?.CurrentHouseholdId == null)
            return BadRequest(new { message = "You must select a household first" });

        var recipe = await _context.HouseholdRecipes
            .FirstOrDefaultAsync(hr => hr.Id == id && hr.HouseholdId == user.CurrentHouseholdId);

        if (recipe == null)
            return NotFound();

        recipe.IsArchived = true;
        recipe.ArchivedDate = DateTime.UtcNow;
        recipe.ArchivedByUserId = userId;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Recipe archived" });
    }

    /// <summary>
    /// Restore an archived recipe
    /// </summary>
    [HttpPost("{id}/restore")]
    public async Task<IActionResult> RestoreRecipe(Guid id)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var user = await _context.Users.FindAsync(userId);

        if (user?.CurrentHouseholdId == null)
            return BadRequest(new { message = "You must select a household first" });

        var recipe = await _context.HouseholdRecipes
            .FirstOrDefaultAsync(hr => hr.Id == id && hr.HouseholdId == user.CurrentHouseholdId);

        if (recipe == null)
            return NotFound();

        recipe.IsArchived = false;
        recipe.ArchivedDate = null;
        recipe.ArchivedByUserId = null;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Recipe restored" });
    }

    /// <summary>
    /// Rate a recipe (0-10 scale)
    /// </summary>
    [HttpPost("{id}/rate")]
    public async Task<ActionResult<HouseholdRecipeDto>> RateRecipe(Guid id, [FromBody] RateRecipeDto dto)
    {
        if (dto.Rating < 0 || dto.Rating > 10)
            return BadRequest(new { message = "Rating must be between 0 and 10" });

        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var user = await _context.Users.FindAsync(userId);

        if (user?.CurrentHouseholdId == null)
            return BadRequest(new { message = "You must select a household first" });

        var recipe = await _context.HouseholdRecipes
            .Include(hr => hr.GlobalRecipe)
            .Include(hr => hr.AddedByUser)
            .Include(hr => hr.Ratings).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(hr => hr.Id == id && hr.HouseholdId == user.CurrentHouseholdId);

        if (recipe == null)
            return NotFound();

        // Check if rating already exists
        var existingRating = await _context.Ratings
            .FirstOrDefaultAsync(r => r.HouseholdRecipeId == id && r.UserId == userId);

        if (existingRating != null)
        {
            // Update existing rating
            existingRating.Score = dto.Rating;
            existingRating.CreatedAt = DateTime.UtcNow; // Update timestamp
        }
        else
        {
            // Create new rating
            var newRating = new Rating
            {
                HouseholdRecipeId = id,
                UserId = userId,
                Score = dto.Rating,
                CreatedAt = DateTime.UtcNow
            };
            _context.Ratings.Add(newRating);
        }

        await _context.SaveChangesAsync();

        // If recipe is linked to global, also update global rating
        if (recipe.GlobalRecipeId.HasValue)
        {
            var globalRecipe = await _context.GlobalRecipes.FindAsync(recipe.GlobalRecipeId.Value);
            if (globalRecipe != null)
            {
                // Recalculate global average
                var allRatings = await _context.Ratings
                    .Where(r => r.HouseholdRecipe!.GlobalRecipeId == recipe.GlobalRecipeId.Value)
                    .Select(r => r.Score)
                    .ToListAsync();

                globalRecipe.AverageRating = allRatings.Count > 0 ? (decimal)allRatings.Average() : 0;
                globalRecipe.TotalRatings = allRatings.Count;
                globalRecipe.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
            }
        }

        // Reload recipe with updated ratings
        recipe = await _context.HouseholdRecipes
            .Include(hr => hr.GlobalRecipe)
            .Include(hr => hr.AddedByUser)
            .Include(hr => hr.Ratings).ThenInclude(r => r.User)
            .FirstAsync(hr => hr.Id == id);

        return Ok(MapToDto(recipe));
    }

    /// <summary>
    /// Fork a linked recipe (convert to editable copy)
    /// </summary>
    [HttpPost("{id}/fork")]
    public async Task<ActionResult<HouseholdRecipeDto>> ForkRecipe(Guid id)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var user = await _context.Users.FindAsync(userId);

        if (user?.CurrentHouseholdId == null)
            return BadRequest(new { message = "You must select a household first" });

        var recipe = await _context.HouseholdRecipes
            .Include(hr => hr.GlobalRecipe)
            .Include(hr => hr.AddedByUser)
            .Include(hr => hr.Ratings).ThenInclude(r => r.User)
            .FirstOrDefaultAsync(hr => hr.Id == id && hr.HouseholdId == user.CurrentHouseholdId);

        if (recipe == null)
            return NotFound();

        if (recipe.GlobalRecipeId == null)
            return BadRequest(new { message = "Recipe is not linked to a global recipe" });

        if (!recipe.GlobalRecipeId.HasValue)
            return BadRequest(new { message = "Recipe is already forked" });

        // Copy data from global recipe and remove link (making it forked)
        var globalRecipe = recipe.GlobalRecipe;
        recipe.LocalTitle = globalRecipe!.Title;
        recipe.LocalDescription = globalRecipe.Description;
        recipe.LocalImageUrl = globalRecipe.ImageUrl;
        recipe.GlobalRecipeId = null; // Remove link to make it forked

        await _context.SaveChangesAsync();

        return Ok(MapToDto(recipe));
    }

    /// <summary>
    /// Delete a recipe permanently
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRecipe(Guid id)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var user = await _context.Users.FindAsync(userId);

        if (user?.CurrentHouseholdId == null)
            return BadRequest(new { message = "You must select a household first" });

        var recipe = await _context.HouseholdRecipes
            .FirstOrDefaultAsync(hr => hr.Id == id && hr.HouseholdId == user.CurrentHouseholdId);

        if (recipe == null)
            return NotFound();

        // Delete associated ratings
        var ratings = await _context.Ratings.Where(r => r.HouseholdRecipeId == id).ToListAsync();
        _context.Ratings.RemoveRange(ratings);

        _context.HouseholdRecipes.Remove(recipe);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Recipe deleted" });
    }

    private static HouseholdRecipeDto MapToDto(HouseholdRecipe recipe)
    {
        // Calculate average rating from household members
        var ratings = recipe.Ratings?
            .Where(r => r.User != null)
            .GroupBy(r => r.User.DisplayName ?? r.User.Email ?? "Unknown")
            .ToDictionary(g => g.Key, g => (int?)g.First().Score)
            ?? new Dictionary<string, int?>();

        var averageRating = ratings.Values.Where(r => r.HasValue).Any()
            ? ratings.Values.Where(r => r.HasValue).Average(r => r!.Value)
            : 0;

        // Get image URLs - prefer local, fallback to global
        var imageUrls = new List<string>();
        if (!string.IsNullOrEmpty(recipe.LocalImageUrl))
        {
            imageUrls.Add(recipe.LocalImageUrl);
        }
        else if (recipe.GlobalRecipe?.ImageUrls != null)
        {
            imageUrls = JsonHelper.JsonToList(recipe.GlobalRecipe.ImageUrls);
        }

        return new HouseholdRecipeDto
        {
            Id = recipe.Id,
            HouseholdId = recipe.HouseholdId,
            Name = recipe.LocalTitle ?? recipe.GlobalRecipe?.Title ?? "Unknown",
            Description = recipe.LocalDescription ?? recipe.GlobalRecipe?.Description,
            ImageUrls = imageUrls,
            Ratings = ratings,
            AverageRating = averageRating,
            DateAdded = recipe.CreatedAt,
            AddedByUserId = recipe.AddedByUserId ?? Guid.Empty,
            AddedByName = recipe.AddedBy?.DisplayName,
            IsArchived = recipe.IsArchived,
            ArchivedDate = recipe.ArchivedDate,
            ArchivedByUserId = recipe.ArchivedByUserId,
            ArchivedByName = recipe.ArchivedBy?.DisplayName,
            GlobalRecipeId = recipe.GlobalRecipeId,
            GlobalRecipeName = recipe.GlobalRecipe?.Title,
            IsForked = !recipe.GlobalRecipeId.HasValue,
            PersonalNotes = recipe.PersonalNotes
        };
    }
}
