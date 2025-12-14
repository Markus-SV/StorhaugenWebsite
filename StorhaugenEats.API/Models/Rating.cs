using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorhaugenEats.API.Models;

[Table("ratings")]
public class Rating
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("global_recipe_id")]
    public Guid? GlobalRecipeId { get; set; }

    /// <summary>
    /// Reference to the user recipe being rated (new user-centric model).
    /// </summary>
    [Column("user_recipe_id")]
    public Guid? UserRecipeId { get; set; }

    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    [Required]
    [Column("score")]
    [Range(0, 10)]
    public int Score { get; set; }

    [Column("comment")]
    public string? Comment { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public GlobalRecipe? GlobalRecipe { get; set; }
    public UserRecipe? UserRecipe { get; set; }
    public User User { get; set; } = null!;

    // Backward compatibility properties
    [NotMapped]
    public int RatingValue
    {
        get => Score;
        set => Score = value;
    }
}
