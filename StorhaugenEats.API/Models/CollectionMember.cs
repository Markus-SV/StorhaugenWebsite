using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorhaugenEats.API.Models;

/// <summary>
/// Represents a member of a collection. The owner is always a member with IsOwner=true.
/// </summary>
[Table("collection_members")]
public class CollectionMember
{
    [Required]
    [Column("collection_id")]
    public Guid CollectionId { get; set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// True if this member is the owner of the collection.
    /// </summary>
    [Column("is_owner")]
    public bool IsOwner { get; set; } = false;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Collection Collection { get; set; } = null!;
    public User User { get; set; } = null!;
}
