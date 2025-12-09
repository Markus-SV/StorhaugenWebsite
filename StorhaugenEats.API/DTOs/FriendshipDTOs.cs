namespace StorhaugenEats.API.DTOs;

// ==========================================
// USER FRIENDSHIP DTOs
// ==========================================

/// <summary>
/// DTO for a single friendship.
/// </summary>
public class UserFriendshipDto
{
    public Guid Id { get; set; }
    public Guid FriendUserId { get; set; }
    public string FriendDisplayName { get; set; } = string.Empty;
    public string? FriendAvatarUrl { get; set; }
    public string FriendShareId { get; set; } = string.Empty;

    /// <summary>
    /// Status from the current user's perspective:
    /// - "accepted" - You are friends
    /// - "pending_sent" - You sent a request, waiting for response
    /// - "pending_received" - You received a request, needs your response
    /// </summary>
    public string Status { get; set; } = string.Empty;

    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }

    /// <summary>
    /// Number of recipes the friend has (visible to you).
    /// </summary>
    public int RecipeCount { get; set; }
}

/// <summary>
/// DTO containing all friendship lists for a user.
/// </summary>
public class FriendshipListDto
{
    public List<UserFriendshipDto> Friends { get; set; } = new();
    public List<UserFriendshipDto> PendingSent { get; set; } = new();
    public List<UserFriendshipDto> PendingReceived { get; set; } = new();

    public int TotalFriends => Friends.Count;
    public int PendingCount => PendingSent.Count + PendingReceived.Count;
}

/// <summary>
/// DTO for sending a friend request.
/// </summary>
public class SendFriendRequestDto
{
    /// <summary>
    /// Target user ID (if known).
    /// </summary>
    public Guid? TargetUserId { get; set; }

    /// <summary>
    /// Target user's share ID (alternative to UserId).
    /// </summary>
    public string? TargetShareId { get; set; }

    /// <summary>
    /// Optional message to include with the request.
    /// </summary>
    public string? Message { get; set; }
}

/// <summary>
/// DTO for responding to a friend request.
/// </summary>
public class RespondFriendRequestDto
{
    /// <summary>
    /// Action to take: "accept" or "reject".
    /// </summary>
    public string Action { get; set; } = string.Empty;
}

/// <summary>
/// DTO for a friend's basic profile info.
/// </summary>
public class FriendProfileDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string ShareId { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public bool IsProfilePublic { get; set; }
    public List<string> FavoriteCuisines { get; set; } = new();
    public int RecipeCount { get; set; }
    public DateTime JoinedAt { get; set; }
}

/// <summary>
/// Search result for finding users to friend.
/// </summary>
public class UserSearchResultDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string ShareId { get; set; } = string.Empty;

    /// <summary>
    /// Current friendship status with this user:
    /// - "none" - No relationship
    /// - "friends" - Already friends
    /// - "pending_sent" - You sent them a request
    /// - "pending_received" - They sent you a request
    /// </summary>
    public string FriendshipStatus { get; set; } = "none";
}
