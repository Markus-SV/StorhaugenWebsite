using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorhaugenEats.API.Models;

[Table("global_recipes")]
public class GlobalRecipe
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("title")]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    [Column("description")]
    public string? Description { get; set; }

    [Column("image_url")]
    public string? ImageUrl { get; set; }

    [Required]
    [Column("ingredients", TypeName = "jsonb")]
    public string Ingredients { get; set; } = "[]";

    [Column("nutrition_data", TypeName = "jsonb")]
    public string? NutritionData { get; set; }

    [Column("cook_time_minutes")]
    public int? CookTimeMinutes { get; set; }

    [Column("difficulty")]
    [MaxLength(50)]
    public string? Difficulty { get; set; }

    // Source tracking
    [Column("is_hellofresh")]
    public bool IsHellofresh { get; set; } = false;

    [Column("hellofresh_uuid")]
    [MaxLength(255)]
    public string? HellofreshUuid { get; set; }

    [Column("created_by_user_id")]
    public Guid? CreatedByUserId { get; set; }

    // Visibility
    [Column("is_public")]
    public bool IsPublic { get; set; } = false;

    // Aggregated ratings
    [Column("average_rating")]
    public decimal AverageRating { get; set; } = 0.00m;

    [Column("rating_count")]
    public int RatingCount { get; set; } = 0;

    [Column("tags", TypeName = "jsonb")]
    public string Tags { get; set; } = "[]";

    [Column("cuisine")]
    [MaxLength(100)]
    public string? Cuisine { get; set; }

    [Column("servings")]
    public int? Servings { get; set; }

    [Column("prep_time_minutes")]
    public int? PrepTimeMinutes { get; set; }

    [Column("total_time_minutes")]
    public int? TotalTimeMinutes { get; set; }

    [Column("hellofresh_slug")]
    [MaxLength(255)]
    public string? HellofreshSlug { get; set; }

    [Column("total_ratings")]
    public int TotalRatings { get; set; } = 0;

    [Column("total_times_added")]
    public int TotalTimesAdded { get; set; } = 0;

    [Column("image_urls", TypeName = "jsonb")]
    public string ImageUrls { get; set; } = "[]";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? CreatedByUser { get; set; }
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();

    // Backward compatibility properties
    [NotMapped]
    public string Name
    {
        get => Title;
        set => Title = value;
    }
}
