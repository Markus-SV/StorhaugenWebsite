using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;
using StorhaugenEats.API.DTOs;
using StorhaugenEats.API.Models;
using StorhaugenEats.API.Services;

namespace StorhaugenEats.API.Controllers;

[ApiController]
[Route("api/global-recipes")]
public class GlobalRecipesController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GlobalRecipesController(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Browse global recipes with filters, search, and pagination
    /// </summary>
    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<GlobalRecipePagedResult>> BrowseRecipes([FromQuery] BrowseGlobalRecipesQuery query)
    {
        var queryable = _context.GlobalRecipes.AsQueryable();

        // Filter: HelloFresh only
        if (query.HellofreshOnly)
        {
            queryable = queryable.Where(gr => gr.IsHellofresh);
        }

        // Filter: Search by name or description
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchLower = query.Search.ToLower();
            queryable = queryable.Where(gr =>
                gr.Name.ToLower().Contains(searchLower) ||
                (gr.Description != null && gr.Description.ToLower().Contains(searchLower))
            );
        }

        // Filter: Cuisine
        if (!string.IsNullOrWhiteSpace(query.Cuisine))
        {
            queryable = queryable.Where(gr => gr.Cuisine == query.Cuisine);
        }

        // Filter: Difficulty
        if (!string.IsNullOrWhiteSpace(query.Difficulty))
        {
            queryable = queryable.Where(gr => gr.Difficulty == query.Difficulty);
        }

        // Filter: Max prep time
        if (query.MaxPrepTime.HasValue)
        {
            queryable = queryable.Where(gr =>
                gr.PrepTimeMinutes.HasValue && gr.PrepTimeMinutes.Value <= query.MaxPrepTime.Value
            );
        }

        // Filter: Tags (if any tag matches)
        if (query.Tags != null && query.Tags.Count > 0)
        {
            foreach (var tag in query.Tags)
            {
                var tagLower = tag.ToLower();
                queryable = queryable.Where(gr => gr.Tags.Any(t => t.ToLower() == tagLower));
            }
        }

        // Get total count before pagination
        var totalCount = await queryable.CountAsync();

        // Sorting
        queryable = query.SortBy.ToLower() switch
        {
            "newest" => queryable.OrderByDescending(gr => gr.CreatedAt),
            "rating" => queryable.OrderByDescending(gr => gr.AverageRating).ThenByDescending(gr => gr.TotalRatings),
            "popular" => queryable.OrderByDescending(gr => gr.TotalTimesAdded),
            "name" => queryable.OrderBy(gr => gr.Name),
            _ => queryable.OrderByDescending(gr => gr.TotalTimesAdded) // Default: popular
        };

        // Pagination
        var recipes = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(gr => MapToDto(gr))
            .ToListAsync();

        return Ok(new GlobalRecipePagedResult
        {
            Recipes = recipes,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        });
    }

    /// <summary>
    /// Get a specific global recipe by ID
    /// </summary>
    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<ActionResult<GlobalRecipeDto>> GetRecipe(Guid id)
    {
        var recipe = await _context.GlobalRecipes
            .Include(gr => gr.CreatedByUser)
            .FirstOrDefaultAsync(gr => gr.Id == id);

        if (recipe == null)
            return NotFound();

        return Ok(MapToDto(recipe));
    }

    /// <summary>
    /// Search global recipes by name/description
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<List<GlobalRecipeDto>>> SearchRecipes(
        [FromQuery] string q,
        [FromQuery] int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(q))
            return BadRequest(new { message = "Search query is required" });

        if (limit > 100) limit = 100;

        var searchLower = q.ToLower();
        var recipes = await _context.GlobalRecipes
            .Include(gr => gr.CreatedByUser)
            .Where(gr =>
                gr.Name.ToLower().Contains(searchLower) ||
                (gr.Description != null && gr.Description.ToLower().Contains(searchLower))
            )
            .OrderByDescending(gr => gr.TotalTimesAdded)
            .Take(limit)
            .Select(gr => MapToDto(gr))
            .ToListAsync();

        return Ok(recipes);
    }

    /// <summary>
    /// Get only HelloFresh recipes
    /// </summary>
    [HttpGet("hellofresh")]
    [AllowAnonymous]
    public async Task<ActionResult<GlobalRecipePagedResult>> GetHelloFreshRecipes(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string sortBy = "newest")
    {
        if (pageSize > 100) pageSize = 100;

        var query = _context.GlobalRecipes.Where(gr => gr.IsHellofresh);

        var totalCount = await query.CountAsync();

        // Sorting
        query = sortBy.ToLower() switch
        {
            "newest" => query.OrderByDescending(gr => gr.CreatedAt),
            "rating" => query.OrderByDescending(gr => gr.AverageRating),
            "name" => query.OrderBy(gr => gr.Name),
            _ => query.OrderByDescending(gr => gr.CreatedAt)
        };

        var recipes = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(gr => MapToDto(gr))
            .ToListAsync();

        return Ok(new GlobalRecipePagedResult
        {
            Recipes = recipes,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    /// <summary>
    /// Create a user-contributed global recipe
    /// </summary>
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<GlobalRecipeDto>> CreateRecipe([FromBody] CreateGlobalRecipeDto dto)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();

        var recipe = new GlobalRecipe
        {
            Name = dto.Name,
            Description = dto.Description,
            ImageUrl = dto.ImageUrl,
            ImageUrls = dto.ImageUrls,
            Ingredients = dto.Ingredients,
            NutritionData = dto.NutritionData,
            PrepTimeMinutes = dto.PrepTimeMinutes,
            CookTimeMinutes = dto.CookTimeMinutes,
            TotalTimeMinutes = (dto.PrepTimeMinutes ?? 0) + (dto.CookTimeMinutes ?? 0),
            Servings = dto.Servings,
            Difficulty = dto.Difficulty,
            Tags = dto.Tags,
            Cuisine = dto.Cuisine,
            IsHellofresh = false,
            CreatedByUserId = userId,
            AverageRating = 0,
            TotalRatings = 0,
            TotalTimesAdded = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.GlobalRecipes.Add(recipe);
        await _context.SaveChangesAsync();

        // Reload with user info
        recipe = await _context.GlobalRecipes
            .Include(gr => gr.CreatedByUser)
            .FirstAsync(gr => gr.Id == recipe.Id);

        return CreatedAtAction(nameof(GetRecipe), new { id = recipe.Id }, MapToDto(recipe));
    }

    /// <summary>
    /// Update a user-contributed recipe (only creator can update)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<ActionResult<GlobalRecipeDto>> UpdateRecipe(Guid id, [FromBody] CreateGlobalRecipeDto dto)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();

        var recipe = await _context.GlobalRecipes
            .Include(gr => gr.CreatedByUser)
            .FirstOrDefaultAsync(gr => gr.Id == id);

        if (recipe == null)
            return NotFound();

        // Only creator can update
        if (recipe.CreatedByUserId != userId)
            return Forbid();

        // Can't update HelloFresh recipes
        if (recipe.IsHellofresh)
            return BadRequest(new { message = "Cannot update HelloFresh recipes" });

        recipe.Name = dto.Name;
        recipe.Description = dto.Description;
        recipe.ImageUrl = dto.ImageUrl;
        recipe.ImageUrls = dto.ImageUrls;
        recipe.Ingredients = dto.Ingredients;
        recipe.NutritionData = dto.NutritionData;
        recipe.PrepTimeMinutes = dto.PrepTimeMinutes;
        recipe.CookTimeMinutes = dto.CookTimeMinutes;
        recipe.TotalTimeMinutes = (dto.PrepTimeMinutes ?? 0) + (dto.CookTimeMinutes ?? 0);
        recipe.Servings = dto.Servings;
        recipe.Difficulty = dto.Difficulty;
        recipe.Tags = dto.Tags;
        recipe.Cuisine = dto.Cuisine;
        recipe.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(MapToDto(recipe));
    }

    /// <summary>
    /// Delete a user-contributed recipe (only creator can delete)
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteRecipe(Guid id)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();

        var recipe = await _context.GlobalRecipes.FindAsync(id);

        if (recipe == null)
            return NotFound();

        // Only creator can delete
        if (recipe.CreatedByUserId != userId)
            return Forbid();

        // Can't delete HelloFresh recipes
        if (recipe.IsHellofresh)
            return BadRequest(new { message = "Cannot delete HelloFresh recipes" });

        // Check if any households are using this recipe
        var usageCount = await _context.HouseholdRecipes.CountAsync(hr => hr.GlobalRecipeId == id);
        if (usageCount > 0)
            return BadRequest(new { message = $"Cannot delete recipe that is used by {usageCount} household(s). They would need to fork it first." });

        _context.GlobalRecipes.Remove(recipe);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Recipe deleted" });
    }

    /// <summary>
    /// Get available filter options (cuisines, difficulties, tags)
    /// </summary>
    [HttpGet("filters")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFilterOptions()
    {
        var cuisines = await _context.GlobalRecipes
            .Where(gr => gr.Cuisine != null)
            .Select(gr => gr.Cuisine!)
            .Distinct()
            .OrderBy(c => c)
            .ToListAsync();

        var difficulties = await _context.GlobalRecipes
            .Where(gr => gr.Difficulty != null)
            .Select(gr => gr.Difficulty!)
            .Distinct()
            .OrderBy(d => d)
            .ToListAsync();

        var allTags = await _context.GlobalRecipes
            .Where(gr => gr.Tags.Count > 0)
            .SelectMany(gr => gr.Tags)
            .Distinct()
            .OrderBy(t => t)
            .ToListAsync();

        return Ok(new
        {
            cuisines,
            difficulties,
            tags = allTags
        });
    }

    private static GlobalRecipeDto MapToDto(GlobalRecipe recipe)
    {
        return new GlobalRecipeDto
        {
            Id = recipe.Id,
            Name = recipe.Name,
            Description = recipe.Description,
            ImageUrl = recipe.ImageUrl,
            ImageUrls = recipe.ImageUrls,
            Ingredients = recipe.Ingredients,
            NutritionData = recipe.NutritionData,
            PrepTimeMinutes = recipe.PrepTimeMinutes,
            CookTimeMinutes = recipe.CookTimeMinutes,
            TotalTimeMinutes = recipe.TotalTimeMinutes,
            Servings = recipe.Servings,
            Difficulty = recipe.Difficulty,
            Tags = recipe.Tags,
            Cuisine = recipe.Cuisine,
            IsHellofresh = recipe.IsHellofresh,
            HellofreshUuid = recipe.HellofreshUuid,
            HellofreshSlug = recipe.HellofreshSlug,
            CreatedByUserId = recipe.CreatedByUserId,
            CreatedByUserName = recipe.CreatedByUser?.DisplayName,
            AverageRating = recipe.AverageRating,
            TotalRatings = recipe.TotalRatings,
            TotalTimesAdded = recipe.TotalTimesAdded,
            CreatedAt = recipe.CreatedAt
        };
    }
}
