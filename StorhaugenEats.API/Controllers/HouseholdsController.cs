using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StorhaugenEats.API.Services;
using System.Security.Claims;

namespace StorhaugenEats.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class HouseholdsController : ControllerBase
{
    private readonly IHouseholdService _householdService;
    private readonly IUserService _userService;

    public HouseholdsController(IHouseholdService householdService, IUserService userService)
    {
        _householdService = householdService;
        _userService = userService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim ?? throw new UnauthorizedAccessException());
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetHousehold(Guid id)
    {
        var household = await _householdService.GetByIdAsync(id);

        if (household == null)
            return NotFound();

        return Ok(household);
    }

    [HttpPost]
    public async Task<IActionResult> CreateHousehold([FromBody] CreateHouseholdRequest request)
    {
        var userId = GetCurrentUserId();
        var household = await _householdService.CreateAsync(request.Name, userId);

        return CreatedAtAction(nameof(GetHousehold), new { id = household.Id }, household);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateHousehold(Guid id, [FromBody] UpdateHouseholdRequest request)
    {
        var household = await _householdService.GetByIdAsync(id);

        if (household == null)
            return NotFound();

        // Only leader can update
        var userId = GetCurrentUserId();
        if (household.LeaderId != userId)
            return Forbid();

        household.Name = request.Name ?? household.Name;
        household.Settings = request.Settings ?? household.Settings;

        await _householdService.UpdateAsync(household);

        return Ok(household);
    }

    [HttpGet("{id}/members")]
    public async Task<IActionResult> GetMembers(Guid id)
    {
        var members = await _householdService.GetMembersAsync(id);
        return Ok(members);
    }

    [HttpPost("{id}/members")]
    public async Task<IActionResult> AddMember(Guid id, [FromBody] AddMemberRequest request)
    {
        var household = await _householdService.GetByIdAsync(id);

        if (household == null)
            return NotFound();

        // Only leader can add members
        var userId = GetCurrentUserId();
        if (household.LeaderId != userId)
            return Forbid();

        var success = await _householdService.AddMemberAsync(id, request.UserId);

        if (!success)
            return BadRequest(new { message = "Failed to add member" });

        return Ok();
    }

    [HttpDelete("{id}/members/{userId}")]
    public async Task<IActionResult> RemoveMember(Guid id, Guid userId)
    {
        var household = await _householdService.GetByIdAsync(id);

        if (household == null)
            return NotFound();

        // Only leader can remove members (or user can remove themselves)
        var currentUserId = GetCurrentUserId();
        if (household.LeaderId != currentUserId && currentUserId != userId)
            return Forbid();

        var success = await _householdService.RemoveMemberAsync(id, userId);

        if (!success)
            return BadRequest(new { message = "Failed to remove member" });

        return Ok();
    }

    [HttpPost("join")]
    public async Task<IActionResult> JoinByShareId([FromBody] JoinHouseholdRequest request)
    {
        var userId = GetCurrentUserId();
        var targetUser = await _userService.GetByShareIdAsync(request.ShareId);

        if (targetUser == null || targetUser.CurrentHouseholdId == null)
            return NotFound(new { message = "Invalid share ID or user has no household" });

        var success = await _householdService.AddMemberAsync(targetUser.CurrentHouseholdId.Value, userId);

        if (!success)
            return BadRequest(new { message = "Failed to join household" });

        return Ok(new { householdId = targetUser.CurrentHouseholdId });
    }

    [HttpPost("merge")]
    public async Task<IActionResult> MergeHouseholds([FromBody] MergeHouseholdsRequest request)
    {
        var userId = GetCurrentUserId();

        // Verify user is leader of target household
        var targetHousehold = await _householdService.GetByIdAsync(request.TargetHouseholdId);

        if (targetHousehold == null || targetHousehold.LeaderId != userId)
            return Forbid();

        var success = await _householdService.MergeHouseholdsAsync(request.SourceHouseholdId, request.TargetHouseholdId);

        if (!success)
            return BadRequest(new { message = "Failed to merge households" });

        return Ok(new { message = "Households merged successfully" });
    }
}

public record CreateHouseholdRequest(string Name);
public record UpdateHouseholdRequest(string? Name, string? Settings);
public record AddMemberRequest(Guid UserId);
public record JoinHouseholdRequest(string ShareId);
public record MergeHouseholdsRequest(Guid SourceHouseholdId, Guid TargetHouseholdId);
