using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorhaugenEats.API.Models;

/// <summary>
/// DEPRECATED: Recipes scoped to a household.
/// Use UserRecipe for user-centric recipe ownership.
/// This model is maintained for backward compatibility and migration purposes.
/// </summary>
[Obsolete("Use UserRecipe instead. This model will be removed after migration is complete.")]
[Table("household_recipes")]
public class HouseholdRecipe
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("household_id")]
    public Guid HouseholdId { get; set; }

    // Reference/Fork Logic
    [Column("global_recipe_id")]
    public Guid? GlobalRecipeId { get; set; }

    // Local data (used when forked, or as personal notes when linked)
    [Column("local_title")]
    [MaxLength(255)]
    public string? LocalTitle { get; set; }

    [Column("local_description")]
    public string? LocalDescription { get; set; }

    [Column("local_ingredients", TypeName = "jsonb")]
    public string? LocalIngredients { get; set; }

    [Column("is_public")]
    public bool IsPublic { get; set; } = false;

    [Column("local_image_url")]
    public string? LocalImageUrl { get; set; }

    [Column("personal_notes")]
    public string? PersonalNotes { get; set; }

    // Metadata
    [Column("added_by_user_id")]
    public Guid? AddedByUserId { get; set; }

    [Column("is_archived")]
    public bool IsArchived { get; set; } = false;

    [Column("archived_date")]
    public DateTime? ArchivedDate { get; set; }

    [Column("archived_by_user_id")]
    public Guid? ArchivedByUserId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Household Household { get; set; } = null!;
    public GlobalRecipe? GlobalRecipe { get; set; }
    public User? AddedByUser { get; set; }
    public User? ArchivedBy { get; set; }
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();

    // Helper property to determine mode
    [NotMapped]
    public string RecipeMode => GlobalRecipeId.HasValue ? "linked" : "forked";

    // Helper to get display title
    [NotMapped]
    public string DisplayTitle => LocalTitle ?? GlobalRecipe?.Title ?? "Untitled Recipe";

    // Backward compatibility properties
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

    [NotMapped]
    public string? ImageUrls
    {
        get => LocalImageUrl != null ? $"[\"{LocalImageUrl}\"]" : GlobalRecipe?.ImageUrls;
        set => LocalImageUrl = value;
    }

    [NotMapped]
    public User? AddedBy => AddedByUser;

    [NotMapped]
    public DateTime DateAdded => CreatedAt;

    [NotMapped]
    public bool IsForked => !GlobalRecipeId.HasValue;
}
