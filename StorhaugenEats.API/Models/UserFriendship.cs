using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorhaugenEats.API.Models;

/// <summary>
/// Represents a friendship connection between two users.
/// This enables the social features of the app - users can see what their friends
/// are cooking and rating.
/// </summary>
[Table("user_friendships")]
public class UserFriendship
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    /// <summary>
    /// The user who initiated the friend request.
    /// </summary>
    [Required]
    [Column("requester_user_id")]
    public Guid RequesterUserId { get; set; }

    /// <summary>
    /// The user who received the friend request.
    /// </summary>
    [Required]
    [Column("target_user_id")]
    public Guid TargetUserId { get; set; }

    /// <summary>
    /// Status of the friendship: 'pending', 'accepted', 'rejected', 'blocked'
    /// </summary>
    [Required]
    [Column("status")]
    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Optional message sent with the friend request.
    /// </summary>
    [Column("message")]
    [MaxLength(255)]
    public string? Message { get; set; }

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("responded_at")]
    public DateTime? RespondedAt { get; set; }

    // Navigation properties
    public User RequesterUser { get; set; } = null!;
    public User TargetUser { get; set; } = null!;

    // Helper properties
    [NotMapped]
    public bool IsPending => Status == "pending";

    [NotMapped]
    public bool IsAccepted => Status == "accepted";

    /// <summary>
    /// Gets the "other" user from the perspective of the given user ID.
    /// </summary>
    public User? GetOtherUser(Guid currentUserId)
    {
        if (currentUserId == RequesterUserId)
            return TargetUser;
        if (currentUserId == TargetUserId)
            return RequesterUser;
        return null;
    }

    /// <summary>
    /// Gets the ID of the "other" user from the perspective of the given user ID.
    /// </summary>
    public Guid GetOtherUserId(Guid currentUserId)
    {
        return currentUserId == RequesterUserId ? TargetUserId : RequesterUserId;
    }
}
