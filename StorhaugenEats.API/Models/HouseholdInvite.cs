using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorhaugenEats.API.Models;

[Table("household_invites")]
public class HouseholdInvite
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("household_id")]
    public Guid HouseholdId { get; set; }

    [Column("invited_user_id")]
    public Guid? InvitedUserId { get; set; }

    [Column("invited_email")]
    [MaxLength(255)]
    public string? InvitedEmail { get; set; }

    [Required]
    [Column("invited_by_user_id")]
    public Guid InvitedByUserId { get; set; }

    [Required]
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "pending"; // "pending", "accepted", "rejected"

    [Column("merge_requested")]
    public bool MergeRequested { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Household Household { get; set; } = null!;
    public User? InvitedUser { get; set; }
    public User InvitedByUser { get; set; } = null!;

    // Backward compatibility properties
    [NotMapped]
    public Guid InvitedById
    {
        get => InvitedByUserId;
        set => InvitedByUserId = value;
    }
}
