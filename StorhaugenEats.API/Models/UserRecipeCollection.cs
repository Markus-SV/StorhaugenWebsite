using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorhaugenEats.API.Models;

/// <summary>
/// Join table connecting UserRecipes to Collections.
/// A recipe can belong to multiple collections, a collection can contain multiple recipes.
/// </summary>
[Table("user_recipe_collections")]
public class UserRecipeCollection
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("user_recipe_id")]
    public Guid UserRecipeId { get; set; }

    [Required]
    [Column("collection_id")]
    public Guid CollectionId { get; set; }

    [Column("added_at")]
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;

    [Column("added_by_user_id")]
    public Guid AddedByUserId { get; set; }

    [Column("notes")]
    [MaxLength(500)]
    public string? Notes { get; set; }

    [Column("sort_order")]
    public int SortOrder { get; set; } = 0;

    // Navigation properties
    public UserRecipe UserRecipe { get; set; } = null!;
    public Collection Collection { get; set; } = null!;
    public User AddedByUser { get; set; } = null!;
}
