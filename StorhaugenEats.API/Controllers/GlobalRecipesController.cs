using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;
using StorhaugenWebsite.Shared.DTOs;
using StorhaugenEats.API.Models;
using StorhaugenEats.API.Services;
using StorhaugenEats.API.Helpers;

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

        // Filter: HelloFresh week
        if (!string.IsNullOrWhiteSpace(query.HellofreshWeek))
        {
            queryable = queryable.Where(gr => gr.HellofreshWeek == query.HellofreshWeek);
        }

        // Filter: Search by name or description
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchLower = query.Search.ToLower();
            queryable = queryable.Where(gr =>
                gr.Title.ToLower().Contains(searchLower) ||
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
        // Note: Tags are stored as JSON strings, so we need to filter in memory after fetching
        // TODO: Implement PostgreSQL JSONB queries for better performance
        var filterTags = query.Tags != null && query.Tags.Count > 0 ? query.Tags : null;

        // Get total count before pagination
        var totalCount = await queryable.CountAsync();

        // Sorting
        queryable = query.SortBy.ToLower() switch
        {
            "newest" => queryable.OrderByDescending(gr => gr.CreatedAt),
            "rating" => queryable.OrderByDescending(gr => gr.AverageRating).ThenByDescending(gr => gr.TotalRatings),
            "popular" => queryable.OrderByDescending(gr => gr.TotalTimesAdded),
            "name" => queryable.OrderBy(gr => gr.Title),
            _ => queryable.OrderByDescending(gr => gr.TotalTimesAdded) // Default: popular
        };

        // Pagination
        var recipes = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        // Apply tag filtering in memory if needed
        if (filterTags != null)
        {
            recipes = recipes.Where(gr =>
            {
                var recipeTags = JsonHelper.JsonToList(gr.Tags);
                return filterTags.Any(tag => recipeTags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)));
            }).ToList();
        }

        var recipeDtos = recipes.Select(gr => MapToDto(gr)).ToList();

        return Ok(new GlobalRecipePagedResult
        {
            Recipes = recipeDtos,
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
                gr.Title.ToLower().Contains(searchLower) ||
                (gr.Description != null && gr.Description.ToLower().Contains(searchLower))
            )
            .OrderByDescending(gr => gr.TotalTimesAdded)
            .Take(limit)
            .ToListAsync();

        var recipeDtos = recipes.Select(gr => MapToDto(gr)).ToList();

        return Ok(recipeDtos);
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
            "name" => query.OrderBy(gr => gr.Title),
            _ => query.OrderByDescending(gr => gr.CreatedAt)
        };

        var recipes = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var recipeDtos = recipes.Select(gr => MapToDto(gr)).ToList();

        return Ok(new GlobalRecipePagedResult
        {
            Recipes = recipeDtos,
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
            Title = dto.Name,
            Description = dto.Description,
            ImageUrl = dto.ImageUrl,
            ImageUrls = JsonHelper.ListToJson(dto.ImageUrls),
            Ingredients = JsonHelper.ObjectToJson(dto.Ingredients) ?? "[]",
            NutritionData = JsonHelper.ObjectToJson(dto.NutritionData),
            PrepTimeMinutes = dto.PrepTimeMinutes,
            CookTimeMinutes = dto.CookTimeMinutes,
            TotalTimeMinutes = (dto.PrepTimeMinutes ?? 0) + (dto.CookTimeMinutes ?? 0),
            Servings = dto.Servings,
            Difficulty = dto.Difficulty,
            Tags = JsonHelper.ListToJson(dto.Tags),
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

        recipe.Title = dto.Name;
        recipe.Description = dto.Description;
        recipe.ImageUrl = dto.ImageUrl;
        recipe.ImageUrls = JsonHelper.ListToJson(dto.ImageUrls);
        recipe.Ingredients = JsonHelper.ObjectToJson(dto.Ingredients) ?? "[]";
        recipe.NutritionData = JsonHelper.ObjectToJson(dto.NutritionData);
        recipe.PrepTimeMinutes = dto.PrepTimeMinutes;
        recipe.CookTimeMinutes = dto.CookTimeMinutes;
        recipe.TotalTimeMinutes = (dto.PrepTimeMinutes ?? 0) + (dto.CookTimeMinutes ?? 0);
        recipe.Servings = dto.Servings;
        recipe.Difficulty = dto.Difficulty;
        recipe.Tags = JsonHelper.ListToJson(dto.Tags);
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

        // Extract tags from JSON strings (done in memory)
        var allRecipes = await _context.GlobalRecipes
            .Where(gr => gr.Tags != null && gr.Tags != "[]")
            .Select(gr => gr.Tags)
            .ToListAsync();

        var allTags = allRecipes
            .SelectMany(jsonTags => JsonHelper.JsonToList(jsonTags))
            .Distinct()
            .OrderBy(t => t)
            .ToList();

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
            Name = recipe.Title,
            Description = recipe.Description,
            ImageUrl = recipe.ImageUrl,
            ImageUrls = JsonHelper.JsonToList(recipe.ImageUrls),
            Ingredients = JsonHelper.JsonToObject(recipe.Ingredients),
            NutritionData = JsonHelper.JsonToObject(recipe.NutritionData),
            PrepTimeMinutes = recipe.PrepTimeMinutes,
            CookTimeMinutes = recipe.CookTimeMinutes,
            TotalTimeMinutes = recipe.TotalTimeMinutes,
            Servings = recipe.Servings,
            Difficulty = recipe.Difficulty,
            Tags = JsonHelper.JsonToList(recipe.Tags),
            Cuisine = recipe.Cuisine,
            IsHellofresh = recipe.IsHellofresh,
            HellofreshUuid = recipe.HellofreshUuid,
            HellofreshSlug = recipe.HellofreshSlug,
            HellofreshWeek = recipe.HellofreshWeek,
            CreatedByUserId = recipe.CreatedByUserId,
            CreatedByUserName = recipe.CreatedByUser?.DisplayName,
            AverageRating = (double)recipe.AverageRating,
            TotalRatings = recipe.TotalRatings,
            TotalTimesAdded = recipe.TotalTimesAdded,
            CreatedAt = recipe.CreatedAt
        };
    }
}
