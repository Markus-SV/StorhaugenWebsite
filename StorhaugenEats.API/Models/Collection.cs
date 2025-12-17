using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorhaugenEats.API.Models;

/// <summary>
/// A collection of recipes that can be shared with other users.
/// Replaces the household-centric model.
/// </summary>
[Table("collections")]
public class Collection
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The user who owns this collection.
    /// </summary>
    [Required]
    [Column("owner_user_id")]
    public Guid OwnerUserId { get; set; }

    [Required]
    [Column("name")]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of the collection.
    /// </summary>
    [Column("description")]
    [MaxLength(500)]
    public string? Description { get; set; }

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Column("color")]
    [MaxLength(7)]
    public string? Color { get; set; }

    [Column("icon")]
    [MaxLength(50)]
    public string? Icon { get; set; }

    [Column("is_default")]
    public bool IsDefault { get; set; } = false;

    [Column("is_shared")]
    public bool IsShared { get; set; } = false;

    /// <summary>
    /// Unique share code for shared collections
    /// </summary>
    [Column("unique_share_id")]
    [MaxLength(12)]
    public string? UniqueShareId { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; } = 0;

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User Owner { get; set; } = null!;
    public ICollection<CollectionMember> Members { get; set; } = new List<CollectionMember>();
    public ICollection<UserRecipeCollection> UserRecipeCollections { get; set; } = new List<UserRecipeCollection>();
}
