using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorhaugenEats.API.Models;

[Table("household_friendships")]
public class HouseholdFriendship
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("requester_household_id")]
    public Guid RequesterHouseholdId { get; set; }

    [Required]
    [Column("target_household_id")]
    public Guid TargetHouseholdId { get; set; }

    [Required]
    [Column("status")] // pending, accepted, rejected
    public string Status { get; set; } = "pending";

    [Column("requested_by_user_id")]
    public Guid? RequestedByUserId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("responded_at")]
    public DateTime? RespondedAt { get; set; }

    // Navigation properties
    public Household? RequesterHousehold { get; set; }
    public Household? TargetHousehold { get; set; }
    public User? RequestedByUser { get; set; }
}