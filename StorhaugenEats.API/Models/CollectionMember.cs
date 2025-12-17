using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorhaugenEats.API.Models;

/// <summary>
/// Represents a member of a collection. The owner is always a member with Role="owner".
/// </summary>
[Table("collection_members")]
public class CollectionMember
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("collection_id")]
    public Guid CollectionId { get; set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Role of the member: "owner" or "member"
    /// </summary>
    [Required]
    [Column("role")]
    [MaxLength(20)]
    public string Role { get; set; } = "member";

    [Column("joined_at")]
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    [Column("invited_by_user_id")]
    public Guid? InvitedByUserId { get; set; }

    // Navigation properties
    public Collection Collection { get; set; } = null!;
    public User User { get; set; } = null!;
    public User? InvitedByUser { get; set; }
}
