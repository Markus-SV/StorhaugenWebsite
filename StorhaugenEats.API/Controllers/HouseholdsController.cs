using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;
using StorhaugenEats.API.DTOs;
using StorhaugenEats.API.Models;
using StorhaugenEats.API.Services;

namespace StorhaugenEats.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HouseholdsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public HouseholdsController(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Get all households the current user is a member of
    /// </summary>
    [HttpGet("my")]
    public async Task<ActionResult<List<HouseholdDto>>> GetMyHouseholds()
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();

        var households = await _context.Households
            .Where(h => h.HouseholdMembers.Any(m => m.UserId == userId))
            .Select(h => new HouseholdDto
            {
                Id = h.Id,
                Name = h.Name,
                CreatedById = h.LeaderId ?? Guid.Empty,
                CreatedByName = h.Leader != null ? h.Leader.DisplayName : null,
                CreatedAt = h.CreatedAt,
                Members = h.HouseholdMembers.Select(m => new HouseholdMemberDto
                {
                    UserId = m.UserId,
                    Email = m.User.Email,
                    DisplayName = m.User.DisplayName,
                    AvatarUrl = m.User.AvatarUrl,
                    JoinedAt = m.JoinedAt
                }).ToList()
            })
            .ToListAsync();

        return Ok(households);
    }

    /// <summary>
    /// Get a specific household by ID
    /// </summary>
    [HttpGet("{id}")]
    public async Task<ActionResult<HouseholdDto>> GetHousehold(Guid id)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();

        var household = await _context.Households
            .Where(h => h.Id == id && h.HouseholdMembers.Any(m => m.UserId == userId))
            .Select(h => new HouseholdDto
            {
                Id = h.Id,
                Name = h.Name,
                CreatedById = h.LeaderId ?? Guid.Empty,
                CreatedByName = h.Leader != null ? h.Leader.DisplayName : null,
                CreatedAt = h.CreatedAt,
                Members = h.HouseholdMembers.Select(m => new HouseholdMemberDto
                {
                    UserId = m.UserId,
                    Email = m.User.Email,
                    DisplayName = m.User.DisplayName,
                    AvatarUrl = m.User.AvatarUrl,
                    JoinedAt = m.JoinedAt
                }).ToList()
            })
            .FirstOrDefaultAsync();

        if (household == null)
            return NotFound(new { message = "Household not found or you are not a member" });

        return Ok(household);
    }

    /// <summary>
    /// Create a new household
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<HouseholdDto>> CreateHousehold([FromBody] CreateHouseholdDto dto)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var user = await _context.Users.FindAsync(userId);

        var household = new Household
        {
            Name = dto.Name,
            LeaderId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Households.Add(household);
        await _context.SaveChangesAsync();

        // Add creator as first member
        var member = new HouseholdMember
        {
            HouseholdId = household.Id,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        };
        _context.HouseholdMembers.Add(member);

        // Set as user's current household
        if (user != null)
        {
            user.CurrentHouseholdId = household.Id;
            user.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // Build and return DTO directly (don't call GetHousehold internally)
        var householdDto = new HouseholdDto
        {
            Id = household.Id,
            Name = household.Name,
            CreatedById = userId,
            CreatedByName = user?.DisplayName,
            CreatedAt = household.CreatedAt,
            Members = new List<HouseholdMemberDto>
            {
                new HouseholdMemberDto
                {
                    UserId = userId,
                    Email = user?.Email ?? "",
                    DisplayName = user?.DisplayName,
                    AvatarUrl = user?.AvatarUrl,
                    JoinedAt = member.JoinedAt
                }
            }
        };

        return CreatedAtAction(nameof(GetHousehold), new { id = household.Id }, householdDto);
    }

    /// <summary>
    /// Update household name
    /// </summary>
    [HttpPut("{id}")]
    public async Task<ActionResult<HouseholdDto>> UpdateHousehold(Guid id, [FromBody] UpdateHouseholdDto dto)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();

        var household = await _context.Households
            .FirstOrDefaultAsync(h => h.Id == id && h.LeaderId == userId);

        if (household == null)
            return NotFound(new { message = "Household not found or you are not the creator" });

        household.Name = dto.Name;
        household.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return await GetHousehold(id);
    }

    /// <summary>
    /// Send an invitation to join the household
    /// </summary>
    [HttpPost("{id}/invites")]
    public async Task<ActionResult<HouseholdInviteDto>> InviteToHousehold(Guid id, [FromBody] InviteToHouseholdDto dto)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();

        // Verify user is member of household
        var isMember = await _context.HouseholdMembers
            .AnyAsync(hm => hm.HouseholdId == id && hm.UserId == userId);

        if (!isMember)
            return Forbid();

        var household = await _context.Households.FindAsync(id);
        if (household == null)
            return NotFound();

        // Check if invite already exists
        var existingInvite = await _context.HouseholdInvites
            .FirstOrDefaultAsync(i => i.HouseholdId == id && i.InvitedEmail == dto.Email && i.Status == "pending");

        if (existingInvite != null)
            return BadRequest(new { message = "An invite is already pending for this email" });

        // Create invite
        var invite = new HouseholdInvite
        {
            HouseholdId = id,
            InvitedByUserId = userId,
            InvitedEmail = dto.Email,
            Status = "pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.HouseholdInvites.Add(invite);
        await _context.SaveChangesAsync();

        var inviter = await _context.Users.FindAsync(userId);

        return Ok(new HouseholdInviteDto
        {
            Id = invite.Id,
            HouseholdId = household.Id,
            HouseholdName = household.Name,
            InvitedById = userId,
            InvitedByName = inviter?.DisplayName ?? "Unknown",
            InvitedEmail = dto.Email,
            Status = "pending",
            CreatedAt = invite.CreatedAt
        });
    }

    /// <summary>
    /// Get pending invites for the current user
    /// </summary>
    [HttpGet("invites/pending")]
    public async Task<ActionResult<List<HouseholdInviteDto>>> GetPendingInvites()
    {
        var email = _currentUserService.GetUserEmail();
        if (string.IsNullOrEmpty(email))
            return Unauthorized();

        var invites = await _context.HouseholdInvites
            .Where(i => i.InvitedEmail == email && i.Status == "pending")
            .Select(i => new HouseholdInviteDto
            {
                Id = i.Id,
                HouseholdId = i.HouseholdId,
                HouseholdName = i.Household.Name,
                InvitedById = i.InvitedByUserId,
                InvitedByName = i.InvitedByUser.DisplayName,
                InvitedEmail = i.InvitedEmail ?? string.Empty,
                Status = i.Status,
                CreatedAt = i.CreatedAt
            })
            .ToListAsync();

        return Ok(invites);
    }

    /// <summary>
    /// Accept an invitation
    /// </summary>
    [HttpPost("invites/{inviteId}/accept")]
    public async Task<ActionResult<HouseholdDto>> AcceptInvite(Guid inviteId)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var email = _currentUserService.GetUserEmail();

        var invite = await _context.HouseholdInvites
            .FirstOrDefaultAsync(i => i.Id == inviteId && i.InvitedEmail == email && i.Status == "pending");

        if (invite == null)
            return NotFound(new { message = "Invite not found or already processed" });

        // Add user to household
        var existingMember = await _context.HouseholdMembers
            .FirstOrDefaultAsync(hm => hm.HouseholdId == invite.HouseholdId && hm.UserId == userId);

        if (existingMember == null)
        {
            _context.HouseholdMembers.Add(new HouseholdMember
            {
                HouseholdId = invite.HouseholdId,
                UserId = userId,
                JoinedAt = DateTime.UtcNow
            });
        }

        // Update invite status
        invite.Status = "accepted";
        invite.UpdatedAt = DateTime.UtcNow;

        // Set as user's current household
        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.CurrentHouseholdId = invite.HouseholdId;
            user.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return await GetHousehold(invite.HouseholdId);
    }

    /// <summary>
    /// Reject an invitation
    /// </summary>
    [HttpPost("invites/{inviteId}/reject")]
    public async Task<IActionResult> RejectInvite(Guid inviteId)
    {
        var email = _currentUserService.GetUserEmail();

        var invite = await _context.HouseholdInvites
            .FirstOrDefaultAsync(i => i.Id == inviteId && i.InvitedEmail == email && i.Status == "pending");

        if (invite == null)
            return NotFound(new { message = "Invite not found or already processed" });

        invite.Status = "rejected";
        await _context.SaveChangesAsync();

        return Ok(new { message = "Invite rejected" });
    }

    /// <summary>
    /// Switch to a different household (must be a member)
    /// </summary>
    [HttpPost("{id}/switch")]
    public async Task<IActionResult> SwitchHousehold(Guid id)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();

        // Verify user is member of household
        var isMember = await _context.HouseholdMembers
            .AnyAsync(hm => hm.HouseholdId == id && hm.UserId == userId);

        if (!isMember)
            return NotFound(new { message = "You are not a member of this household" });

        var user = await _context.Users.FindAsync(userId);
        if (user != null)
        {
            user.CurrentHouseholdId = id;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }

        return Ok(new { message = "Switched household successfully", currentHouseholdId = id });
    }

    /// <summary>
    /// Leave a household
    /// </summary>
    [HttpPost("{id}/leave")]
    public async Task<IActionResult> LeaveHousehold(Guid id)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();

        var membership = await _context.HouseholdMembers
            .FirstOrDefaultAsync(hm => hm.HouseholdId == id && hm.UserId == userId);

        if (membership == null)
            return NotFound(new { message = "You are not a member of this household" });

        // Check if user is the creator and there are other members
        var household = await _context.Households.FindAsync(id);
        if (household?.LeaderId == userId)
        {
            var memberCount = await _context.HouseholdMembers.CountAsync(hm => hm.HouseholdId == id);
            if (memberCount > 1)
                return BadRequest(new { message = "Cannot leave household as creator while other members exist. Transfer ownership or remove all members first." });
        }

        _context.HouseholdMembers.Remove(membership);

        // If this was user's current household, clear it
        var user = await _context.Users.FindAsync(userId);
        if (user?.CurrentHouseholdId == id)
        {
            user.CurrentHouseholdId = null;
            user.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "Left household successfully" });
    }
}
