using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;
using StorhaugenWebsite.Shared.DTOs;
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
            UniqueShareId = user.UniqueShareId,
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

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            UniqueShareId = user.UniqueShareId,
            CreatedAt = user.CreatedAt
        });
    }

    /// <summary>
    /// Get a user's public profile (for collection members or friends)
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<UserDto>> GetUserProfile(Guid id)
    {
        var currentUserId = await _currentUserService.GetOrCreateUserIdAsync();
        var user = await _context.Users.FindAsync(id);

        if (user == null)
            return NotFound();

        // Allow if same user
        if (currentUserId == id)
        {
            return Ok(new UserDto
            {
                Id = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                AvatarUrl = user.AvatarUrl,
                UniqueShareId = user.UniqueShareId,
                CreatedAt = user.CreatedAt
            });
        }

        // Check if current user shares a collection with this user
        var sharedCollection = await _context.CollectionMembers
            .Where(cm => cm.UserId == currentUserId)
            .Select(cm => cm.CollectionId)
            .Intersect(
                _context.CollectionMembers
                    .Where(cm => cm.UserId == id)
                    .Select(cm => cm.CollectionId)
            )
            .AnyAsync();

        if (!sharedCollection)
            return Forbid();

        return Ok(new UserDto
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            UniqueShareId = user.UniqueShareId,
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
