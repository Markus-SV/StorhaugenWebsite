using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StorhaugenWebsite.Shared.DTOs;
using StorhaugenEats.API.Services;

namespace StorhaugenEats.API.Controllers;

[ApiController]
[Route("api/collections")]
[Authorize]
public class CollectionsController : ControllerBase
{
    private readonly ICollectionService _collectionService;
    private readonly ICurrentUserService _currentUserService;

    public CollectionsController(
        ICollectionService collectionService,
        ICurrentUserService currentUserService)
    {
        _collectionService = collectionService;
        _currentUserService = currentUserService;
    }

    // ==========================================
    // COLLECTION CRUD
    // ==========================================

    /// <summary>
    /// Get all collections for the current user (owned and member of).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<CollectionDto>>> GetCollections()
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var collections = await _collectionService.GetUserCollectionsAsync(userId);
        return Ok(collections);
    }

    /// <summary>
    /// Get a specific collection by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<CollectionDto>> GetCollection(Guid id)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var collection = await _collectionService.GetCollectionAsync(id, userId);

        if (collection == null)
            return NotFound(new { message = "Collection not found or you don't have access" });

        return Ok(collection);
    }

    /// <summary>
    /// Get a collection by its share code (for public/friends collections).
    /// </summary>
    [HttpGet("shared/{shareCode}")]
    [AllowAnonymous]
    public async Task<ActionResult<CollectionDto>> GetCollectionByShareCode(string shareCode)
    {
        Guid? userId = null;
        try
        {
            userId = await _currentUserService.GetOrCreateUserIdAsync();
        }
        catch
        {
            // Anonymous access is allowed for public collections
        }

        var collection = await _collectionService.GetCollectionByShareCodeAsync(shareCode, userId);

        if (collection == null)
            return NotFound(new { message = "Collection not found or you don't have access" });

        return Ok(collection);
    }

    /// <summary>
    /// Create a new collection.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<CollectionDto>> CreateCollection([FromBody] CreateCollectionDto dto)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            var collection = await _collectionService.CreateCollectionAsync(userId, dto);
            return CreatedAtAction(nameof(GetCollection), new { id = collection.Id }, collection);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update a collection.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<CollectionDto>> UpdateCollection(Guid id, [FromBody] UpdateCollectionDto dto)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            var collection = await _collectionService.UpdateCollectionAsync(id, userId, dto);
            return Ok(collection);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Delete a collection (recipes are not deleted, just unlinked).
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteCollection(Guid id)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            await _collectionService.DeleteCollectionAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ==========================================
    // RECIPE-COLLECTION MANAGEMENT
    // ==========================================

    /// <summary>
    /// Get all recipes in a collection.
    /// </summary>
    [HttpGet("{id}/recipes")]
    public async Task<ActionResult<CollectionRecipesResult>> GetCollectionRecipes(
        Guid id,
        [FromQuery] GetCollectionRecipesQuery? query)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            var result = await _collectionService.GetCollectionRecipesAsync(id, userId, query);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Add a recipe to a collection.
    /// </summary>
    [HttpPost("{id}/recipes")]
    public async Task<ActionResult> AddRecipeToCollection(Guid id, [FromBody] AddRecipeToCollectionDto dto)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            await _collectionService.AddRecipeToCollectionAsync(id, userId, dto);
            return Ok(new { message = "Recipe added to collection" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove a recipe from a collection.
    /// </summary>
    [HttpDelete("{id}/recipes/{recipeId}")]
    public async Task<ActionResult> RemoveRecipeFromCollection(Guid id, Guid recipeId)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            await _collectionService.RemoveRecipeFromCollectionAsync(id, recipeId, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ==========================================
    // COLLECTION MEMBERSHIP
    // ==========================================

    /// <summary>
    /// Get all members of a collection.
    /// </summary>
    [HttpGet("{id}/members")]
    public async Task<ActionResult<List<CollectionMemberDto>>> GetCollectionMembers(Guid id)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            var members = await _collectionService.GetCollectionMembersAsync(id, userId);
            return Ok(members);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Add a member to a collection.
    /// </summary>
    [HttpPost("{id}/members")]
    public async Task<ActionResult> AddMember(Guid id, [FromBody] AddCollectionMemberDto dto)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            await _collectionService.AddMemberAsync(id, userId, dto);
            return Ok(new { message = "Member added to collection" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove a member from a collection.
    /// </summary>
    [HttpDelete("{id}/members/{memberId}")]
    public async Task<ActionResult> RemoveMember(Guid id, Guid memberId)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            await _collectionService.RemoveMemberAsync(id, memberId, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Leave a collection (cannot be used by owner).
    /// </summary>
    [HttpPost("{id}/leave")]
    public async Task<ActionResult> LeaveCollection(Guid id)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            await _collectionService.LeaveCollectionAsync(id, userId);
            return Ok(new { message = "Left collection" });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
