using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StorhaugenWebsite.Shared.DTOs;
using StorhaugenEats.API.Services;

namespace StorhaugenEats.API.Controllers;

[ApiController]
[Route("api/user-recipes")]
[Authorize]
public class UserRecipesController : ControllerBase
{
    private readonly IUserRecipeService _userRecipeService;
    private readonly ICurrentUserService _currentUserService;

    public UserRecipesController(
        IUserRecipeService userRecipeService,
        ICurrentUserService currentUserService)
    {
        _userRecipeService = userRecipeService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Get all recipes for the current user.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<UserRecipePagedResult>> GetMyRecipes([FromQuery] GetUserRecipesQuery query)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var result = await _userRecipeService.GetUserRecipesAsync(userId, query);
        return Ok(result);
    }

    /// <summary>
    /// Get recipes from friends that are visible to the current user.
    /// </summary>
    [HttpGet("friends")]
    public async Task<ActionResult<UserRecipePagedResult>> GetFriendsRecipes([FromQuery] GetUserRecipesQuery query)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var result = await _userRecipeService.GetFriendsRecipesAsync(userId, query);
        return Ok(result);
    }

    /// <summary>
    /// Get a specific recipe by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserRecipeDto>> GetRecipe(Guid id)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var recipe = await _userRecipeService.GetRecipeAsync(id, userId);

        if (recipe == null)
            return NotFound(new { message = "Recipe not found or you don't have permission to view it" });

        return Ok(recipe);
    }

    /// <summary>
    /// Create a new recipe.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<UserRecipeDto>> CreateRecipe([FromBody] CreateUserRecipeDto dto)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            var recipe = await _userRecipeService.CreateRecipeAsync(userId, dto);
            return CreatedAtAction(nameof(GetRecipe), new { id = recipe.Id }, recipe);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update a recipe.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<UserRecipeDto>> UpdateRecipe(Guid id, [FromBody] UpdateUserRecipeDto dto)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            var recipe = await _userRecipeService.UpdateRecipeAsync(id, userId, dto);
            return Ok(recipe);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a recipe.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteRecipe(Guid id)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            await _userRecipeService.DeleteRecipeAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Publish a local recipe to the global recipe catalog.
    /// </summary>
    [HttpPost("{id}/publish")]
    public async Task<ActionResult<PublishRecipeResultDto>> PublishRecipe(Guid id)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            var result = await _userRecipeService.PublishRecipeAsync(id, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Detach a recipe from its linked global recipe (hard fork).
    /// </summary>
    [HttpPost("{id}/detach")]
    public async Task<ActionResult<UserRecipeDto>> DetachRecipe(Guid id)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            var recipe = await _userRecipeService.DetachRecipeAsync(id, userId);
            return Ok(recipe);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Rate a recipe.
    /// </summary>
    [HttpPost("{id}/rate")]
    public async Task<ActionResult<UserRecipeDto>> RateRecipe(Guid id, [FromBody] RateRecipeDto dto)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            var recipe = await _userRecipeService.RateRecipeAsync(id, userId, dto.Rating, dto.Comment);
            return Ok(recipe);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove rating from a recipe.
    /// </summary>
    [HttpDelete("{id}/rate")]
    public async Task<ActionResult> RemoveRating(Guid id)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            await _userRecipeService.RemoveRatingAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Archive a recipe.
    /// </summary>
    [HttpPost("{id}/archive")]
    public async Task<ActionResult<UserRecipeDto>> ArchiveRecipe(Guid id)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            var recipe = await _userRecipeService.ArchiveRecipeAsync(id, userId);
            return Ok(recipe);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Restore an archived recipe.
    /// </summary>
    [HttpPost("{id}/restore")]
    public async Task<ActionResult<UserRecipeDto>> RestoreRecipe(Guid id)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            var recipe = await _userRecipeService.RestoreRecipeAsync(id, userId);
            return Ok(recipe);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}

/// <summary>
/// DTO for rating a recipe.
/// </summary>
public class RateRecipeDto
{
    public int Rating { get; set; }
    public string? Comment { get; set; }
}
