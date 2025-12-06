namespace StorhaugenEats.API.DTOs;

public class HouseholdDto
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int CreatedById { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<HouseholdMemberDto> Members { get; set; } = new();
}

public class HouseholdMemberDto
{
    public int UserId { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class CreateHouseholdDto
{
    public required string Name { get; set; }
}

public class UpdateHouseholdDto
{
    public required string Name { get; set; }
}

public class InviteToHouseholdDto
{
    public required string Email { get; set; }
}

public class HouseholdInviteDto
{
    public int Id { get; set; }
    public int HouseholdId { get; set; }
    public required string HouseholdName { get; set; }
    public int InvitedById { get; set; }
    public required string InvitedByName { get; set; }
    public required string InvitedEmail { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }
}
