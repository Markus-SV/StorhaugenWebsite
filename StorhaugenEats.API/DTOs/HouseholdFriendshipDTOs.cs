namespace StorhaugenEats.API.DTOs;

public class HouseholdFriendshipDto
{
    public Guid Id { get; set; }
    public Guid RequesterHouseholdId { get; set; }
    public required string RequesterHouseholdName { get; set; }
    public Guid TargetHouseholdId { get; set; }
    public required string TargetHouseholdName { get; set; }
    public required string Status { get; set; }
    public string? RequestedByUserName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}

public class SendFriendRequestDto
{
    public string? HouseholdShareId { get; set; }
    public Guid? HouseholdId { get; set; }
}