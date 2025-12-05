using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StorhaugenEats.API.Services;
using System.Security.Claims;

namespace StorhaugenEats.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GlobalRecipesController : ControllerBase
{
    private readonly IGlobalRecipeService _globalRecipeService;

    public GlobalRecipesController(IGlobalRecipeService globalRecipeService)
    {
        _globalRecipeService = globalRecipeService;
    }

    private Guid? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(userIdClaim, out var userId) ? userId : null;
    }

    [HttpGet]
    [AllowAnonymous] // Public recipes can be viewed by anyone
    public async Task<IActionResult> GetPublicRecipes(
        [FromQuery] int skip = 0,
        [FromQuery] int take = 50,
        [FromQuery] string? sortBy = "rating")
    {
        if (take > 100) take = 100; // Max limit

        var recipes = await _globalRecipeService.GetPublicRecipesAsync(skip, take, sortBy);
        return Ok(recipes);
    }

    [HttpGet("{id}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRecipe(Guid id)
    {
        var recipe = await _globalRecipeService.GetByIdAsync(id);

        if (recipe == null)
            return NotFound();

        return Ok(recipe);
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateRecipe([FromBody] CreateGlobalRecipeRequest request)
    {
        var userId = GetCurrentUserId();

        if (userId == null)
            return Unauthorized();

        var recipe = new Models.GlobalRecipe
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            Ingredients = request.Ingredients,
            NutritionData = request.NutritionData,
            CookTimeMinutes = request.CookTimeMinutes,
            Difficulty = request.Difficulty,
            IsHellofresh = false,
            IsPublic = request.IsPublic,
            CreatedByUserId = userId
        };

        await _globalRecipeService.CreateAsync(recipe);

        return CreatedAtAction(nameof(GetRecipe), new { id = recipe.Id }, recipe);
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateRecipe(Guid id, [FromBody] UpdateGlobalRecipeRequest request)
    {
        var recipe = await _globalRecipeService.GetByIdAsync(id);

        if (recipe == null)
            return NotFound();

        var userId = GetCurrentUserId();

        // Only creator can update (or admin in future)
        if (recipe.CreatedByUserId != userId)
            return Forbid();

        recipe.Title = request.Title ?? recipe.Title;
        recipe.Description = request.Description ?? recipe.Description;
        recipe.ImageUrl = request.ImageUrl ?? recipe.ImageUrl;
        recipe.Ingredients = request.Ingredients ?? recipe.Ingredients;
        recipe.NutritionData = request.NutritionData ?? recipe.NutritionData;
        recipe.CookTimeMinutes = request.CookTimeMinutes ?? recipe.CookTimeMinutes;
        recipe.Difficulty = request.Difficulty ?? recipe.Difficulty;

        if (request.IsPublic.HasValue)
            recipe.IsPublic = request.IsPublic.Value;

        await _globalRecipeService.UpdateAsync(recipe);

        return Ok(recipe);
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteRecipe(Guid id)
    {
        var recipe = await _globalRecipeService.GetByIdAsync(id);

        if (recipe == null)
            return NotFound();

        var userId = GetCurrentUserId();

        // Only creator can delete (or admin in future)
        if (recipe.CreatedByUserId != userId)
            return Forbid();

        // Can't delete HelloFresh recipes
        if (recipe.IsHellofresh)
            return BadRequest(new { message = "Cannot delete HelloFresh recipes" });

        await _globalRecipeService.DeleteAsync(id);

        return NoContent();
    }
}

public record CreateGlobalRecipeRequest(
    string Title,
    string? Description,
    string? ImageUrl,
    string Ingredients,
    string? NutritionData,
    int? CookTimeMinutes,
    string? Difficulty,
    bool IsPublic
);

public record UpdateGlobalRecipeRequest(
    string? Title,
    string? Description,
    string? ImageUrl,
    string? Ingredients,
    string? NutritionData,
    int? CookTimeMinutes,
    string? Difficulty,
    bool? IsPublic
);
