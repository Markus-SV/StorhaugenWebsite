using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorhaugenEats.API.Models;

/// <summary>
/// Represents a recipe owned by a user. This is the new user-centric model that replaces
/// the household-centric HouseholdRecipe. Users own their recipes directly, and households
/// see an aggregated view of their members' recipes.
/// </summary>
[Table("user_recipes")]
public class UserRecipe
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The user who owns this recipe.
    /// </summary>
    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Optional link to a global recipe. When set, this recipe references the global recipe.
    /// When null, the recipe uses local data only.
    /// </summary>
    [Column("global_recipe_id")]
    public Guid? GlobalRecipeId { get; set; }

    // Local recipe data (used when not linked or when user has customizations)
    [Column("local_title")]
    [MaxLength(255)]
    public string? LocalTitle { get; set; }

    [Column("local_description")]
    public string? LocalDescription { get; set; }

    [Column("local_ingredients", TypeName = "jsonb")]
    public string? LocalIngredients { get; set; }

    [Column("local_image_url")]
    public string? LocalImageUrl { get; set; }

    [Column("local_image_urls", TypeName = "jsonb")]
    public string LocalImageUrls { get; set; } = "[]";

    /// <summary>
    /// Personal notes visible only to the owner.
    /// </summary>
    [Column("personal_notes")]
    public string? PersonalNotes { get; set; }

    /// <summary>
    /// Visibility level: 'private', 'household', 'friends', 'public'
    /// - private: Only the owner can see
    /// - household: Members of user's households can see
    /// - friends: User's friends can see
    /// - public: Everyone can see (and it appears in browse)
    /// </summary>
    [Required]
    [Column("visibility")]
    [MaxLength(20)]
    public string Visibility { get; set; } = "private";

    [Column("is_archived")]
    public bool IsArchived { get; set; } = false;

    [Column("archived_date")]
    public DateTime? ArchivedDate { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;
    public GlobalRecipe? GlobalRecipe { get; set; }
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    public ICollection<UserRecipeTag> UserRecipeTags { get; set; } = new List<UserRecipeTag>();

    // Helper properties
    [NotMapped]
    public bool IsLinkedToGlobal => GlobalRecipeId.HasValue;

    [NotMapped]
    public string DisplayTitle => LocalTitle ?? GlobalRecipe?.Title ?? "Untitled Recipe";

    [NotMapped]
    public string? DisplayDescription => LocalDescription ?? GlobalRecipe?.Description;

    [NotMapped]
    public string DisplayImageUrls => !string.IsNullOrEmpty(LocalImageUrl)
        ? $"[\"{LocalImageUrl}\"]"
        : (!string.IsNullOrEmpty(LocalImageUrls) && LocalImageUrls != "[]"
            ? LocalImageUrls
            : GlobalRecipe?.ImageUrls ?? "[]");

    // Backward compatibility
    [NotMapped]
    public string? Name
    {
        get => LocalTitle ?? GlobalRecipe?.Title;
        set => LocalTitle = value;
    }

    [NotMapped]
    public string? Description
    {
        get => LocalDescription ?? GlobalRecipe?.Description;
        set => LocalDescription = value;
    }
}
