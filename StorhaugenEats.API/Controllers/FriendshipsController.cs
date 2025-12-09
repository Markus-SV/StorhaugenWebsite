using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StorhaugenEats.API.DTOs;
using StorhaugenEats.API.Services;

namespace StorhaugenEats.API.Controllers;

[ApiController]
[Route("api/friendships")]
[Authorize]
public class FriendshipsController : ControllerBase
{
    private readonly IUserFriendshipService _friendshipService;
    private readonly ICurrentUserService _currentUserService;

    public FriendshipsController(
        IUserFriendshipService friendshipService,
        ICurrentUserService currentUserService)
    {
        _friendshipService = friendshipService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Get all friendships (accepted, pending sent, pending received).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<FriendshipListDto>> GetFriendships()
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var result = await _friendshipService.GetFriendshipsAsync(userId);
        return Ok(result);
    }

    /// <summary>
    /// Get list of accepted friends only.
    /// </summary>
    [HttpGet("friends")]
    public async Task<ActionResult<List<FriendProfileDto>>> GetFriends()
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var friends = await _friendshipService.GetFriendsAsync(userId);
        return Ok(friends);
    }

    /// <summary>
    /// Get a specific friendship by ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserFriendshipDto>> GetFriendship(Guid id)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var friendship = await _friendshipService.GetFriendshipAsync(id, userId);

        if (friendship == null)
            return NotFound(new { message = "Friendship not found" });

        return Ok(friendship);
    }

    /// <summary>
    /// Send a friend request.
    /// </summary>
    [HttpPost("request")]
    public async Task<ActionResult<UserFriendshipDto>> SendFriendRequest([FromBody] SendFriendRequestDto dto)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            var friendship = await _friendshipService.SendFriendRequestAsync(userId, dto);
            return CreatedAtAction(nameof(GetFriendship), new { id = friendship.Id }, friendship);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Respond to a friend request (accept or reject).
    /// </summary>
    [HttpPost("{id}/respond")]
    public async Task<ActionResult<UserFriendshipDto>> RespondToRequest(Guid id, [FromBody] RespondFriendRequestDto dto)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            var friendship = await _friendshipService.RespondToRequestAsync(id, userId, dto.Action);
            return Ok(friendship);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Remove a friendship or cancel a pending request.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<ActionResult> RemoveFriendship(Guid id)
    {
        try
        {
            var userId = await _currentUserService.GetOrCreateUserIdAsync();
            await _friendshipService.RemoveFriendshipAsync(id, userId);
            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Search for users to add as friends.
    /// </summary>
    [HttpGet("search")]
    public async Task<ActionResult<List<UserSearchResultDto>>> SearchUsers([FromQuery] string query, [FromQuery] int limit = 20)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            return Ok(new List<UserSearchResultDto>());

        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var results = await _friendshipService.SearchUsersAsync(userId, query, limit);
        return Ok(results);
    }

    /// <summary>
    /// Get a user's public profile.
    /// </summary>
    [HttpGet("profile/{profileUserId}")]
    public async Task<ActionResult<FriendProfileDto>> GetUserProfile(Guid profileUserId)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var profile = await _friendshipService.GetUserProfileAsync(profileUserId, userId);

        if (profile == null)
            return NotFound(new { message = "User not found or profile is private" });

        return Ok(profile);
    }
}
