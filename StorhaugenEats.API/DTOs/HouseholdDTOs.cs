namespace StorhaugenEats.API.DTOs;

public class HouseholdDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public Guid CreatedById { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<HouseholdMemberDto> Members { get; set; } = new();
    public string? UniqueShareId { get; set; }
    public bool IsPrivate { get; set; }
}

public class HouseholdMemberDto
{
    public Guid UserId { get; set; }
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
    public bool? IsPrivate { get; set; }
}

public class UpdateHouseholdSettingsDto
{
    public bool? IsPrivate { get; set; }
}

public class UpdateHouseholdSettingsDto
{
    public bool? IsPrivate { get; set; }
}

public class InviteToHouseholdDto
{
    public string? Email { get; set; }
    public string? UniqueShareId { get; set; }
}

public class HouseholdSearchResultDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? UniqueShareId { get; set; }
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsPrivate { get; set; }
}

public class HouseholdInviteDto
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public required string HouseholdName { get; set; }
    public Guid InvitedById { get; set; }
    public required string InvitedByName { get; set; }
    public required string InvitedEmail { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }
}
