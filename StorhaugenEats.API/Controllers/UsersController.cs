using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;
using StorhaugenEats.API.DTOs;
using StorhaugenEats.API.Services;

namespace StorhaugenEats.API.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public UsersController(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Get current user's profile
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<UserDto>> GetMyProfile()
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return NotFound();

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            CurrentHouseholdId = user.CurrentHouseholdId,
            CreatedAt = user.CreatedAt
        });
    }

    /// <summary>
    /// Update current user's profile
    /// </summary>
    [HttpPut("me")]
    public async Task<ActionResult<UserDto>> UpdateMyProfile([FromBody] UpdateUserDto dto)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var user = await _context.Users.FindAsync(userId);

        if (user == null)
            return NotFound();

        if (dto.DisplayName != null)
            user.DisplayName = dto.DisplayName;

        if (dto.AvatarUrl != null)
            user.AvatarUrl = dto.AvatarUrl;

        if (dto.CurrentHouseholdId.HasValue)
        {
            // Verify user is a member of the household
            var isMember = await _context.HouseholdMembers
                .AnyAsync(hm => hm.HouseholdId == dto.CurrentHouseholdId.Value && hm.UserId == userId);

            if (!isMember)
                return BadRequest(new { message = "You are not a member of that household" });

            user.CurrentHouseholdId = dto.CurrentHouseholdId.Value;
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            CurrentHouseholdId = user.CurrentHouseholdId,
            CreatedAt = user.CreatedAt
        });
    }

    /// <summary>
    /// Get a user's public profile (for household members)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUserProfile(int id)
    {
        var currentUserId = await _currentUserService.GetOrCreateUserIdAsync();
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        // Check if current user shares a household with this user
        var sharedHousehold = await _context.HouseholdMembers
            .Where(hm => hm.UserId == currentUserId)
            .Select(hm => hm.HouseholdId)
            .Intersect(
                _context.HouseholdMembers
                    .Where(hm => hm.UserId == id)
                    .Select(hm => hm.HouseholdId)
            )
            .AnyAsync();

        if (!sharedHousehold && currentUserId != id)
            return Forbid();

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            CurrentHouseholdId = null, // Don't expose current household for privacy
            CreatedAt = user.CreatedAt
        });
    }

    /// <summary>
    /// Trigger HelloFresh ETL sync (checks 24-hour limit)
    /// </summary>
    [HttpPost("trigger-hellofresh-sync")]
    [AllowAnonymous] // Called on login, before auth
    public async Task<IActionResult> TriggerHelloFreshSync()
    {
        // Check last sync time from etl_sync_log
        var lastSync = await _context.EtlSyncLogs
            .Where(log => log.Status == "success")
            .OrderByDescending(log => log.StartedAt)
            .FirstOrDefaultAsync();

        var shouldSync = lastSync == null || (DateTime.UtcNow - lastSync.StartedAt).TotalHours > 24;

        if (!shouldSync)
        {
            return Ok(new
            {
                message = "HelloFresh sync not needed yet",
                lastSync = lastSync?.StartedAt,
                nextSync = lastSync?.StartedAt.AddHours(24)
            });
        }

        // Return a message to trigger sync via the HelloFresh controller
        return Ok(new
        {
            message = "HelloFresh sync is due",
            shouldSync = true,
            lastSync = lastSync?.StartedAt
        });

        // Note: Actual sync should be triggered via POST /api/HelloFresh/sync
        // This endpoint just checks if sync is needed
    }
}
