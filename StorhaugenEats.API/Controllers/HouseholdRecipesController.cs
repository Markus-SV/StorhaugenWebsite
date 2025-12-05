using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StorhaugenEats.API.Services;
using System.Security.Claims;

namespace StorhaugenEats.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HouseholdRecipesController : ControllerBase
{
    private readonly IHouseholdRecipeService _householdRecipeService;
    private readonly IUserService _userService;

    public HouseholdRecipesController(IHouseholdRecipeService householdRecipeService, IUserService userService)
    {
        _householdRecipeService = householdRecipeService;
        _userService = userService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim ?? throw new UnauthorizedAccessException());
    }

    [HttpGet]
    public async Task<IActionResult> GetMyHouseholdRecipes([FromQuery] bool includeArchived = false)
    {
        var userId = GetCurrentUserId();
        var user = await _userService.GetByIdAsync(userId);

        if (user?.CurrentHouseholdId == null)
            return BadRequest(new { message = "User is not in a household" });

        var recipes = await _householdRecipeService.GetByHouseholdAsync(user.CurrentHouseholdId.Value, includeArchived);

        return Ok(recipes);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetRecipe(Guid id)
    {
        var recipe = await _householdRecipeService.GetByIdAsync(id);

        if (recipe == null)
            return NotFound();

        // Verify user is in the household
        var userId = GetCurrentUserId();
        var user = await _userService.GetByIdAsync(userId);

        if (user?.CurrentHouseholdId != recipe.HouseholdId)
            return Forbid();

        return Ok(recipe);
    }

    [HttpPost("link")]
    public async Task<IActionResult> AddLinkedRecipe([FromBody] AddLinkedRecipeRequest request)
    {
        var userId = GetCurrentUserId();
        var user = await _userService.GetByIdAsync(userId);

        if (user?.CurrentHouseholdId == null)
            return BadRequest(new { message = "User is not in a household" });

        var recipe = await _householdRecipeService.AddLinkedRecipeAsync(
            user.CurrentHouseholdId.Value,
            request.GlobalRecipeId,
            userId,
            request.PersonalNotes
        );

        return CreatedAtAction(nameof(GetRecipe), new { id = recipe.Id }, recipe);
    }

    [HttpPost("fork")]
    public async Task<IActionResult> AddForkedRecipe([FromBody] AddForkedRecipeRequest request)
    {
        var userId = GetCurrentUserId();
        var user = await _userService.GetByIdAsync(userId);

        if (user?.CurrentHouseholdId == null)
            return BadRequest(new { message = "User is not in a household" });

        var recipe = await _householdRecipeService.AddForkedRecipeAsync(
            user.CurrentHouseholdId.Value,
            userId,
            request.Title,
            request.Description,
            request.Ingredients,
            request.ImageUrl,
            request.PersonalNotes
        );

        return CreatedAtAction(nameof(GetRecipe), new { id = recipe.Id }, recipe);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateRecipe(Guid id, [FromBody] UpdateHouseholdRecipeRequest request)
    {
        var recipe = await _householdRecipeService.GetByIdAsync(id);

        if (recipe == null)
            return NotFound();

        // Verify user is in the household
        var userId = GetCurrentUserId();
        var user = await _userService.GetByIdAsync(userId);

        if (user?.CurrentHouseholdId != recipe.HouseholdId)
            return Forbid();

        // Update personal notes (always allowed)
        if (request.PersonalNotes != null)
            recipe.PersonalNotes = request.PersonalNotes;

        // Update local data only if forked
        if (recipe.GlobalRecipeId == null)
        {
            recipe.LocalTitle = request.LocalTitle ?? recipe.LocalTitle;
            recipe.LocalDescription = request.LocalDescription ?? recipe.LocalDescription;
            recipe.LocalIngredients = request.LocalIngredients ?? recipe.LocalIngredients;
            recipe.LocalImageUrl = request.LocalImageUrl ?? recipe.LocalImageUrl;
        }

        await _householdRecipeService.UpdateAsync(recipe);

        return Ok(recipe);
    }

    [HttpPost("{id}/fork")]
    public async Task<IActionResult> ForkRecipe(Guid id)
    {
        var recipe = await _householdRecipeService.GetByIdAsync(id);

        if (recipe == null)
            return NotFound();

        // Verify user is in the household
        var userId = GetCurrentUserId();
        var user = await _userService.GetByIdAsync(userId);

        if (user?.CurrentHouseholdId != recipe.HouseholdId)
            return Forbid();

        var forkedRecipe = await _householdRecipeService.ForkRecipeAsync(id);

        return Ok(forkedRecipe);
    }

    [HttpPost("{id}/archive")]
    public async Task<IActionResult> ArchiveRecipe(Guid id)
    {
        var recipe = await _householdRecipeService.GetByIdAsync(id);

        if (recipe == null)
            return NotFound();

        // Verify user is in the household
        var userId = GetCurrentUserId();
        var user = await _userService.GetByIdAsync(userId);

        if (user?.CurrentHouseholdId != recipe.HouseholdId)
            return Forbid();

        await _householdRecipeService.ArchiveAsync(id);

        return Ok();
    }

    [HttpPost("{id}/unarchive")]
    public async Task<IActionResult> UnarchiveRecipe(Guid id)
    {
        var recipe = await _householdRecipeService.GetByIdAsync(id);

        if (recipe == null)
            return NotFound();

        // Verify user is in the household
        var userId = GetCurrentUserId();
        var user = await _userService.GetByIdAsync(userId);

        if (user?.CurrentHouseholdId != recipe.HouseholdId)
            return Forbid();

        await _householdRecipeService.UnarchiveAsync(id);

        return Ok();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteRecipe(Guid id)
    {
        var recipe = await _householdRecipeService.GetByIdAsync(id);

        if (recipe == null)
            return NotFound();

        // Verify user is in the household
        var userId = GetCurrentUserId();
        var user = await _userService.GetByIdAsync(userId);

        if (user?.CurrentHouseholdId != recipe.HouseholdId)
            return Forbid();

        await _householdRecipeService.DeleteAsync(id);

        return NoContent();
    }
}

public record AddLinkedRecipeRequest(Guid GlobalRecipeId, string? PersonalNotes);
public record AddForkedRecipeRequest(string Title, string? Description, string Ingredients, string? ImageUrl, string? PersonalNotes);
public record UpdateHouseholdRecipeRequest(string? LocalTitle, string? LocalDescription, string? LocalIngredients, string? LocalImageUrl, string? PersonalNotes);
