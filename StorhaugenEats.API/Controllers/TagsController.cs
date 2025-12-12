using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StorhaugenWebsite.Shared.DTOs;
using StorhaugenEats.API.Services;

namespace StorhaugenEats.API.Controllers;

/// <summary>
/// Controller for managing personal recipe tags/categories.
/// Tags are used for personal organization and are private to each user.
/// </summary>
[ApiController]
[Route("api/tags")]
[Authorize]
public class TagsController : ControllerBase
{
    private readonly ITagService _tagService;
    private readonly ICurrentUserService _currentUserService;

    public TagsController(ITagService tagService, ICurrentUserService currentUserService)
    {
        _tagService = tagService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Get all tags for the current user.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<TagDto>>> GetMyTags()
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var tags = await _tagService.GetUserTagsAsync(userId);
        return Ok(tags);
    }

    /// <summary>
    /// Get a specific tag by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<TagDto>> GetTag(Guid id)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var tag = await _tagService.GetTagAsync(id, userId);

        if (tag == null)
            return NotFound(new { message = "Tag not found" });

        return Ok(tag);
    }

    /// <summary>
    /// Create a new tag.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TagDto>> CreateTag([FromBody] CreateTagDto dto)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            var tag = await _tagService.CreateTagAsync(userId, dto);
            return CreatedAtAction(nameof(GetTag), new { id = tag.Id }, tag);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update a tag.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<TagDto>> UpdateTag(Guid id, [FromBody] UpdateTagDto dto)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            var tag = await _tagService.UpdateTagAsync(id, userId, dto);
            return Ok(tag);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a tag.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTag(Guid id)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            await _tagService.DeleteTagAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ==========================================
    // RECIPE-TAG MANAGEMENT
    // ==========================================

    /// <summary>
    /// Get tags for a specific recipe.
    /// </summary>
    [HttpGet("recipe/{recipeId}")]
    public async Task<ActionResult<List<TagReferenceDto>>> GetRecipeTags(Guid recipeId)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            var tags = await _tagService.GetRecipeTagsAsync(recipeId, userId);
            return Ok(tags);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Set tags for a recipe (replaces existing tags).
    /// </summary>
    [HttpPut("recipe/{recipeId}")]
    public async Task<ActionResult> SetRecipeTags(Guid recipeId, [FromBody] UpdateRecipeTagsDto dto)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            await _tagService.SetRecipeTagsAsync(recipeId, userId, dto.TagIds);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Add a tag to a recipe.
    /// </summary>
    [HttpPost("recipe/{recipeId}/tag/{tagId}")]
    public async Task<ActionResult> AddTagToRecipe(Guid recipeId, Guid tagId)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            await _tagService.AddTagToRecipeAsync(recipeId, tagId, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove a tag from a recipe.
    /// </summary>
    [HttpDelete("recipe/{recipeId}/tag/{tagId}")]
    public async Task<ActionResult> RemoveTagFromRecipe(Guid recipeId, Guid tagId)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            await _tagService.RemoveTagFromRecipeAsync(recipeId, tagId, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
