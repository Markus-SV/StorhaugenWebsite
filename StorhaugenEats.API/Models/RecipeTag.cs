using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorhaugenEats.API.Models;

/// <summary>
/// Represents a personal tag/category for organizing recipes.
/// Tags are user-owned and only visible to the owner.
/// Examples: "Kylling", "Rask middag", "Grill", "Søndag"
/// </summary>
[Table("recipe_tags")]
public class RecipeTag
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The user who owns this tag.
    /// </summary>
    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// The tag name (e.g., "Kylling", "Rask")
    /// </summary>
    [Required]
    [Column("name")]
    [MaxLength(50)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional color for visual distinction (hex code).
    /// </summary>
    [Column("color")]
    [MaxLength(7)]
    public string? Color { get; set; }

    /// <summary>
    /// Optional icon name (Material Icons).
    /// </summary>
    [Column("icon")]
    [MaxLength(50)]
    public string? Icon { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public ICollection<UserRecipeTag> UserRecipeTags { get; set; } = new List<UserRecipeTag>();
}

/// <summary>
/// Join table linking UserRecipes to RecipeTags.
/// Allows a recipe to have multiple tags and a tag to be applied to multiple recipes.
/// </summary>
[Table("user_recipe_tags")]
public class UserRecipeTag
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("user_recipe_id")]
    public Guid UserRecipeId { get; set; }

    [Required]
    [Column("tag_id")]
    public Guid TagId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public UserRecipe UserRecipe { get; set; } = null!;
    public RecipeTag Tag { get; set; } = null!;
}
