using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorhaugenEats.API.Models;

[Table("ratings")]
public class Rating
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("global_recipe_id")]
    public Guid GlobalRecipeId { get; set; }

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
    public GlobalRecipe GlobalRecipe { get; set; } = null!;
    public User User { get; set; } = null!;
}
