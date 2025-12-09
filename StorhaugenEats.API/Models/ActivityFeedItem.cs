using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorhaugenEats.API.Models;

/// <summary>
/// Represents an activity item in the social feed.
/// Activities are denormalized for fast query performance.
/// </summary>
[Table("activity_feed")]
public class ActivityFeedItem
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The user who performed the activity.
    /// </summary>
    [Required]
    [Column("user_id")]
    public Guid UserId { get; set; }

    /// <summary>
    /// Type of activity: 'rated', 'added', 'published', 'joined_household'
    /// </summary>
    [Required]
    [Column("activity_type")]
    [MaxLength(50)]
    public string ActivityType { get; set; } = string.Empty;

    /// <summary>
    /// Type of target entity: 'user_recipe', 'global_recipe', 'household'
    /// </summary>
    [Required]
    [Column("target_type")]
    [MaxLength(50)]
    public string TargetType { get; set; } = string.Empty;

    /// <summary>
    /// ID of the target entity.
    /// </summary>
    [Required]
    [Column("target_id")]
    public Guid TargetId { get; set; }

    /// <summary>
    /// Additional metadata as JSON (recipe name, rating score, household name, etc.)
    /// Example: { "recipeName": "Spaghetti", "rating": 8, "imageUrl": "..." }
    /// </summary>
    [Column("metadata", TypeName = "jsonb")]
    public string Metadata { get; set; } = "{}";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User User { get; set; } = null!;

    // Static factory methods for creating activities
    public static ActivityFeedItem CreateRatingActivity(Guid userId, Guid recipeId, string recipeName, int rating, string? imageUrl = null)
    {
        var metadata = new Dictionary<string, object?>
        {
            ["recipeName"] = recipeName,
            ["rating"] = rating,
            ["imageUrl"] = imageUrl
        };

        return new ActivityFeedItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ActivityType = "rated",
            TargetType = "user_recipe",
            TargetId = recipeId,
            Metadata = System.Text.Json.JsonSerializer.Serialize(metadata),
            CreatedAt = DateTime.UtcNow
        };
    }

    public static ActivityFeedItem CreateAddedRecipeActivity(Guid userId, Guid recipeId, string recipeName, string? imageUrl = null)
    {
        var metadata = new Dictionary<string, object?>
        {
            ["recipeName"] = recipeName,
            ["imageUrl"] = imageUrl
        };

        return new ActivityFeedItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ActivityType = "added",
            TargetType = "user_recipe",
            TargetId = recipeId,
            Metadata = System.Text.Json.JsonSerializer.Serialize(metadata),
            CreatedAt = DateTime.UtcNow
        };
    }

    public static ActivityFeedItem CreatePublishedActivity(Guid userId, Guid globalRecipeId, string recipeName, string? imageUrl = null)
    {
        var metadata = new Dictionary<string, object?>
        {
            ["recipeName"] = recipeName,
            ["imageUrl"] = imageUrl
        };

        return new ActivityFeedItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ActivityType = "published",
            TargetType = "global_recipe",
            TargetId = globalRecipeId,
            Metadata = System.Text.Json.JsonSerializer.Serialize(metadata),
            CreatedAt = DateTime.UtcNow
        };
    }

    public static ActivityFeedItem CreateJoinedHouseholdActivity(Guid userId, Guid householdId, string householdName)
    {
        var metadata = new Dictionary<string, object?>
        {
            ["householdName"] = householdName
        };

        return new ActivityFeedItem
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            ActivityType = "joined_household",
            TargetType = "household",
            TargetId = householdId,
            Metadata = System.Text.Json.JsonSerializer.Serialize(metadata),
            CreatedAt = DateTime.UtcNow
        };
    }
}
