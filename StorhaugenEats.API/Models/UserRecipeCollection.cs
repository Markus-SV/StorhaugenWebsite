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
    [Required]
    [Column("user_recipe_id")]
    public Guid UserRecipeId { get; set; }

    [Required]
    [Column("collection_id")]
    public Guid CollectionId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public UserRecipe UserRecipe { get; set; } = null!;
    public Collection Collection { get; set; } = null!;
}
