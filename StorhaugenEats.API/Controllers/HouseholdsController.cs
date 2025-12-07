using System.Linq;
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
                UniqueShareId = h.UniqueShareId,
                IsPrivate = h.IsPrivate,
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
                UniqueShareId = h.UniqueShareId,
                IsPrivate = h.IsPrivate,
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

    [HttpPut("{id}/settings")]
    public async Task<ActionResult<HouseholdDto>> UpdateHouseholdSettings(Guid id, [FromBody] UpdateHouseholdSettingsDto dto)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var household = await _context.Households.FirstOrDefaultAsync(h => h.Id == id && h.LeaderId == userId);

        if (household == null) return NotFound(new { message = "Household not found or you are not the leader" });

        if (dto.IsPrivate.HasValue) household.IsPrivate = dto.IsPrivate.Value;

        // Generate Share ID if it doesn't exist and we're saving settings
        if (string.IsNullOrEmpty(household.UniqueShareId))
        {
            household.UniqueShareId = await GenerateUniqueHouseholdShareIdAsync();
        }

        await _context.SaveChangesAsync();

        // Return full DTO (re-query or map existing)
        // ... existing mapping logic ...
        return Ok(new HouseholdDto
        {
            Id = household.Id,
            Name = household.Name,
            IsPrivate = household.IsPrivate,
            UniqueShareId = household.UniqueShareId
            /* map other fields */
        });
    }

    /// <summary>
    /// Create a new household
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<HouseholdDto>> CreateHousehold([FromBody] CreateHouseholdDto dto)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var user = await _context.Users.FindAsync(userId);

        // 1. Generate the ID here and assign it to the entity
        var household = new Household
        {
            Name = dto.Name,
            LeaderId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            UniqueShareId = await GenerateUniqueHouseholdShareIdAsync(),
            IsPrivate = false
        };

        _context.Households.Add(household);
        await _context.SaveChangesAsync(); // Now it's saved to the DB

        var member = new HouseholdMember
        {
            HouseholdId = household.Id,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        };
        _context.HouseholdMembers.Add(member);

        if (user != null)
        {
            user.CurrentHouseholdId = household.Id;
            user.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        // 2. Read it from the entity for the DTO
        var householdDto = new HouseholdDto
        {
            Id = household.Id,
            Name = household.Name,
            CreatedById = userId,
            CreatedByName = user?.DisplayName,
            CreatedAt = household.CreatedAt,
            UniqueShareId = household.UniqueShareId,
            IsPrivate = household.IsPrivate,
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
    /// Send an invitation to join the household (by email or unique share ID)
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

        // Determine email from dto.Email or by looking up UniqueShareId
        string? invitedEmail = dto.Email;

        if (string.IsNullOrWhiteSpace(invitedEmail) && !string.IsNullOrWhiteSpace(dto.UniqueShareId))
        {
            // Look up user by UniqueShareId
            var targetUser = await _context.Users
                .FirstOrDefaultAsync(u => u.UniqueShareId == dto.UniqueShareId.Trim().ToUpperInvariant());

            if (targetUser == null)
                return NotFound(new { message = "User with that share ID not found" });

            invitedEmail = targetUser.Email;
        }

        if (string.IsNullOrWhiteSpace(invitedEmail))
            return BadRequest(new { message = "Either email or share ID is required" });

        // Check if user is already a member
        var existingMember = await _context.HouseholdMembers
            .Include(hm => hm.User)
            .FirstOrDefaultAsync(hm => hm.HouseholdId == id && hm.User.Email == invitedEmail);

        if (existingMember != null)
            return BadRequest(new { message = "User is already a member of this household" });

        // Check if invite already exists
        var existingInvite = await _context.HouseholdInvites
            .FirstOrDefaultAsync(i => i.HouseholdId == id && i.InvitedEmail == invitedEmail && i.Status == "pending");

        if (existingInvite != null)
            return BadRequest(new { message = "An invite is already pending for this user" });

        // Create invite
        var invite = new HouseholdInvite
        {
            HouseholdId = id,
            InvitedByUserId = userId,
            InvitedEmail = invitedEmail,
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
            InvitedEmail = invitedEmail,
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

    /// <summary>
    /// Regenerate the household share ID
    /// </summary>
    [HttpPost("{id}/regenerate-share-id")]
    public async Task<ActionResult<HouseholdDto>> RegenerateShareId(Guid id)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();

        var household = await _context.Households.FirstOrDefaultAsync(h => h.Id == id && h.LeaderId == userId);
        if (household == null)
            return NotFound(new { message = "Household not found or you are not the creator" });

        household.UniqueShareId = await GenerateUniqueHouseholdShareIdAsync();
        household.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetHousehold(id);
    }

    /// <summary>
    /// Search for households (only non-private are returned unless exact share ID match)
    /// </summary>
    [HttpGet("search")]
    [AllowAnonymous]
    public async Task<ActionResult<List<HouseholdSearchResultDto>>> SearchHouseholds([FromQuery] string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<HouseholdSearchResultDto>();

        var normalized = query.Trim().ToUpperInvariant();

        var households = await _context.Households
            .Where(h => (!h.IsPrivate && h.Name.ToUpper().Contains(normalized)) || (h.UniqueShareId == normalized))
            .Select(h => new HouseholdSearchResultDto
            {
                Id = h.Id,
                Name = h.Name,
                UniqueShareId = h.UniqueShareId,
                MemberCount = h.HouseholdMembers.Count,
                CreatedAt = h.CreatedAt,
                IsPrivate = h.IsPrivate
            })
            .OrderBy(h => h.IsPrivate)
            .ThenBy(h => h.Name)
            .ToListAsync();

        return households;
    }

    private async Task<string> GenerateUniqueHouseholdShareIdAsync()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var random = new Random();
        string shareId;

        do
        {
            shareId = new string(Enumerable.Range(0, 12)
                .Select(_ => chars[random.Next(chars.Length)])
                .ToArray());
        } while (await _context.Households.AnyAsync(h => h.UniqueShareId == shareId));

        return shareId;
    }
}
