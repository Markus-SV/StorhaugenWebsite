using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorhaugenEats.API.Models;

[Table("users")]
public class User
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("email")]
    [MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [Column("display_name")]
    [MaxLength(255)]
    public string DisplayName { get; set; } = string.Empty;

    [Column("avatar_url")]
    public string? AvatarUrl { get; set; }

    [Required]
    [Column("unique_share_id")]
    [MaxLength(12)]
    public string UniqueShareId { get; set; } = string.Empty;

    [Column("current_household_id")]
    public Guid? CurrentHouseholdId { get; set; }

    [Column("supabase_user_id")]
    [MaxLength(255)]
    public string? SupabaseUserId { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // New fields for user-centric social features
    [Column("is_profile_public")]
    public bool IsProfilePublic { get; set; } = true;

    [Column("bio")]
    [MaxLength(500)]
    public string? Bio { get; set; }

    [Column("favorite_cuisines", TypeName = "jsonb")]
    public string FavoriteCuisines { get; set; } = "[]";

    // Navigation properties
    public Household? CurrentHousehold { get; set; }
    public ICollection<UserRecipe> UserRecipes { get; set; } = new List<UserRecipe>();
    public ICollection<UserFriendship> SentFriendRequests { get; set; } = new List<UserFriendship>();
    public ICollection<UserFriendship> ReceivedFriendRequests { get; set; } = new List<UserFriendship>();
    public ICollection<ActivityFeedItem> Activities { get; set; } = new List<ActivityFeedItem>();

    // Backward compatibility properties
    [NotMapped]
    public Guid UserId => Id;
}
