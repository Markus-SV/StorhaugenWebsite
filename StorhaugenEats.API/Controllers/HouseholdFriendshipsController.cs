using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;
using StorhaugenWebsite.Shared.DTOs;
using StorhaugenEats.API.Models;
using StorhaugenEats.API.Services;

namespace StorhaugenEats.API.Controllers;

/// <summary>
/// DEPRECATED: This controller manages friendships between households.
/// Use FriendshipsController for user-to-user friendship management instead.
/// This controller is maintained for backward compatibility during migration.
/// </summary>
[Obsolete("Use FriendshipsController instead. This controller will be removed after migration is complete.")]
[ApiController]
[Route("api/household-friendships")]
[Authorize]
public class HouseholdFriendshipsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public HouseholdFriendshipsController(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public async Task<ActionResult<List<HouseholdFriendshipDto>>> GetFriendships()
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var user = await _context.Users.FindAsync(userId);
        if (user?.CurrentHouseholdId == null)
            return BadRequest(new { message = "You must select a household first" });

        var householdId = user.CurrentHouseholdId.Value;

        var friendships = await _context.HouseholdFriendships
            .Include(f => f.RequesterHousehold)
            .Include(f => f.TargetHousehold)
            .Where(f => f.RequesterHouseholdId == householdId || f.TargetHouseholdId == householdId)
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        return friendships.Select(MapToDto).ToList();
    }

    //[HttpPost("request")]
    //public async Task<ActionResult<HouseholdFriendshipDto>> RequestFriendship([FromBody] SendHouseholdFriendRequestDto dto)
    //{
    //    var userId = await _currentUserService.GetOrCreateUserIdAsync();
    //    var user = await _context.Users.FindAsync(userId);
    //    if (user?.CurrentHouseholdId == null)
    //        return BadRequest(new { message = "You must select a household first" });

    //    var requesterHouseholdId = user.CurrentHouseholdId.Value;

    //    Guid? targetHouseholdId = dto.HouseholdId;
    //    if (!targetHouseholdId.HasValue && !string.IsNullOrWhiteSpace(dto.HouseholdShareId))
    //    {
    //        var target = await _context.Households
    //            .FirstOrDefaultAsync(h => h.UniqueShareId == dto.HouseholdShareId.Trim().ToUpperInvariant());
    //        targetHouseholdId = target?.Id;
    //    }

    //    if (!targetHouseholdId.HasValue)
    //        return BadRequest(new { message = "Target household not found" });

    //    if (targetHouseholdId.Value == requesterHouseholdId)
    //        return BadRequest(new { message = "Cannot friend your own household" });

    //    // Prevent sending requests to private households unless using share ID
    //    var targetHousehold = await _context.Households.FindAsync(targetHouseholdId.Value);
    //    if (targetHousehold == null)
    //        return NotFound(new { message = "Target household not found" });

    //    if (targetHousehold.IsPrivate && string.IsNullOrWhiteSpace(dto.HouseholdShareId))
    //        return BadRequest(new { message = "This household is private" });

    //    var existing = await _context.HouseholdFriendships
    //        .FirstOrDefaultAsync(f => f.RequesterHouseholdId == requesterHouseholdId && f.TargetHouseholdId == targetHouseholdId.Value);
    //    if (existing != null)
    //        return Conflict(new { message = "A request already exists" });

    //    var friendship = new HouseholdFriendship
    //    {
    //        RequesterHouseholdId = requesterHouseholdId,
    //        TargetHouseholdId = targetHouseholdId.Value,
    //        Status = "pending",
    //        CreatedAt = DateTime.UtcNow,
    //        RequestedByUserId = userId,
    //        Message = dto.Message
    //    };

    //    _context.HouseholdFriendships.Add(friendship);
    //    await _context.SaveChangesAsync();

    //    friendship = await _context.HouseholdFriendships
    //        .Include(f => f.RequesterHousehold)
    //        .Include(f => f.TargetHousehold)
    //        .FirstAsync(f => f.Id == friendship.Id);

    //    return CreatedAtAction(nameof(GetFriendships), MapToDto(friendship));
    //}

    //[HttpPost("{id}/respond")]
    //public async Task<ActionResult<HouseholdFriendshipDto>> Respond(Guid id, [FromBody] RespondHouseholdFriendRequestDto dto)
    //{
    //    var userId = await _currentUserService.GetOrCreateUserIdAsync();
    //    var user = await _context.Users.FindAsync(userId);
    //    if (user?.CurrentHouseholdId == null)
    //        return BadRequest(new { message = "You must select a household first" });

    //    var friendship = await _context.HouseholdFriendships
    //        .Include(f => f.RequesterHousehold)
    //        .Include(f => f.TargetHousehold)
    //        .FirstOrDefaultAsync(f => f.Id == id);

    //    if (friendship == null)
    //        return NotFound();

    //    if (friendship.TargetHouseholdId != user.CurrentHouseholdId.Value && friendship.RequesterHouseholdId != user.CurrentHouseholdId.Value)
    //        return Forbid();

    //    if (friendship.Status != "pending")
    //        return BadRequest(new { message = "Request already processed" });

    //    var action = dto.Action.ToLowerInvariant();
    //    if (action == "accept")
    //    {
    //        friendship.Status = "accepted";
    //    }
    //    else if (action == "reject")
    //    {
    //        friendship.Status = "rejected";
    //    }
    //    else
    //    {
    //        return BadRequest(new { message = "Invalid action" });
    //    }

    //    friendship.RespondedAt = DateTime.UtcNow;
    //    friendship.RespondedByUserId = userId;
    //    await _context.SaveChangesAsync();

    //    return MapToDto(friendship);
    //}

    private static HouseholdFriendshipDto MapToDto(HouseholdFriendship friendship)
    {
        return new HouseholdFriendshipDto
        {
            Id = friendship.Id,
            RequesterHouseholdId = friendship.RequesterHouseholdId,
            TargetHouseholdId = friendship.TargetHouseholdId,
            Status = friendship.Status,
            Message = friendship.Message,
            CreatedAt = friendship.CreatedAt,
            RespondedAt = friendship.RespondedAt,
            RequesterHouseholdName = friendship.RequesterHousehold?.Name ?? string.Empty,
            TargetHouseholdName = friendship.TargetHousehold?.Name ?? string.Empty
        };
    }
}
