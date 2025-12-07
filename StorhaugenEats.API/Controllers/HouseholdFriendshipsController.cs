using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;
using StorhaugenEats.API.DTOs;
using StorhaugenEats.API.Models;
using StorhaugenEats.API.Services;

namespace StorhaugenEats.API.Controllers;

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
    public async Task<ActionResult<List<HouseholdFriendshipDto>>> GetMyFriendships()
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var user = await _context.Users.FindAsync(userId);
        if (user?.CurrentHouseholdId == null) return BadRequest("No household selected");

        var friendships = await _context.HouseholdFriendships
            .Include(f => f.RequesterHousehold)
            .Include(f => f.TargetHousehold)
            .Where(f => f.RequesterHouseholdId == user.CurrentHouseholdId || f.TargetHouseholdId == user.CurrentHouseholdId)
            .ToListAsync();

        return Ok(friendships.Select(MapToDto));
    }

    [HttpPost]
    public async Task<ActionResult> SendFriendRequest([FromBody] SendFriendRequestDto dto)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var user = await _context.Users.FindAsync(userId);
        if (user?.CurrentHouseholdId == null) return BadRequest("No household selected");

        Household? targetHousehold = null;

        if (!string.IsNullOrEmpty(dto.HouseholdShareId))
        {
            targetHousehold = await _context.Households.FirstOrDefaultAsync(h => h.UniqueShareId == dto.HouseholdShareId);
        }
        else if (dto.HouseholdId.HasValue)
        {
            targetHousehold = await _context.Households.FindAsync(dto.HouseholdId);
        }

        if (targetHousehold == null) return NotFound("Target household not found");
        if (targetHousehold.Id == user.CurrentHouseholdId) return BadRequest("Cannot friend your own household");

        var existing = await _context.HouseholdFriendships
            .FirstOrDefaultAsync(f =>
                (f.RequesterHouseholdId == user.CurrentHouseholdId && f.TargetHouseholdId == targetHousehold.Id) ||
                (f.RequesterHouseholdId == targetHousehold.Id && f.TargetHouseholdId == user.CurrentHouseholdId));

        if (existing != null) return BadRequest($"Friendship already exists or is pending (Status: {existing.Status})");

        var friendship = new HouseholdFriendship
        {
            RequesterHouseholdId = user.CurrentHouseholdId.Value,
            TargetHouseholdId = targetHousehold.Id,
            RequestedByUserId = userId,
            Status = "pending"
        };

        _context.HouseholdFriendships.Add(friendship);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Friend request sent" });
    }

    [HttpPost("{id}/accept")]
    public async Task<ActionResult> AcceptRequest(Guid id)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var user = await _context.Users.FindAsync(userId);
        if (user?.CurrentHouseholdId == null) return BadRequest("No household");

        var friendship = await _context.HouseholdFriendships.FindAsync(id);
        if (friendship == null) return NotFound();

        if (friendship.TargetHouseholdId != user.CurrentHouseholdId) return Forbid();

        friendship.Status = "accepted";
        friendship.RespondedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Ok(new { message = "Friend request accepted" });
    }

    private HouseholdFriendshipDto MapToDto(HouseholdFriendship f)
    {
        return new HouseholdFriendshipDto
        {
            Id = f.Id,
            RequesterHouseholdId = f.RequesterHouseholdId,
            RequesterHouseholdName = f.RequesterHousehold?.Name ?? "Unknown",
            TargetHouseholdId = f.TargetHouseholdId,
            TargetHouseholdName = f.TargetHousehold?.Name ?? "Unknown",
            Status = f.Status,
            CreatedAt = f.CreatedAt
        };
    }
}