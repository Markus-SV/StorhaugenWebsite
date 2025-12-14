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

    [Required]
    [Column("name")]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The user who owns this collection.
    /// </summary>
    [Required]
    [Column("owner_id")]
    public Guid OwnerId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User Owner { get; set; } = null!;
    public ICollection<CollectionMember> Members { get; set; } = new List<CollectionMember>();
    public ICollection<UserRecipeCollection> UserRecipeCollections { get; set; } = new List<UserRecipeCollection>();
}
