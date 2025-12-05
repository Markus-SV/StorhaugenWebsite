using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StorhaugenEats.API.Services;
using System.Security.Claims;

namespace StorhaugenEats.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim ?? throw new UnauthorizedAccessException());
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userId = GetCurrentUserId();
        var user = await _userService.GetByIdAsync(userId);

        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await _userService.GetByIdAsync(id);

        if (user == null)
            return NotFound();

        return Ok(user);
    }

    [HttpGet("share/{shareId}")]
    public async Task<IActionResult> GetByShareId(string shareId)
    {
        var user = await _userService.GetByShareIdAsync(shareId);

        if (user == null)
            return NotFound(new { message = "User not found with this share ID" });

        return Ok(new { user.Id, user.DisplayName, user.AvatarUrl, user.CurrentHouseholdId });
    }

    [HttpPost]
    [AllowAnonymous] // Called during signup
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
    {
        var user = await _userService.CreateAsync(
            request.AuthUserId,
            request.Email,
            request.DisplayName,
            request.AvatarUrl
        );

        return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateCurrentUser([FromBody] UpdateUserRequest request)
    {
        var userId = GetCurrentUserId();
        var user = await _userService.GetByIdAsync(userId);

        if (user == null)
            return NotFound();

        user.DisplayName = request.DisplayName ?? user.DisplayName;
        user.AvatarUrl = request.AvatarUrl ?? user.AvatarUrl;

        await _userService.UpdateAsync(user);

        return Ok(user);
    }
}

public record CreateUserRequest(Guid AuthUserId, string Email, string DisplayName, string? AvatarUrl);
public record UpdateUserRequest(string? DisplayName, string? AvatarUrl);
