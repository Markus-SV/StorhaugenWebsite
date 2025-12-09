using StorhaugenEats.API.DTOs;

namespace StorhaugenEats.API.Services;

/// <summary>
/// Service for managing user-to-user friendships.
/// </summary>
public interface IUserFriendshipService
{
    // Friendship queries
    Task<FriendshipListDto> GetFriendshipsAsync(Guid userId);
    Task<List<FriendProfileDto>> GetFriendsAsync(Guid userId);
    Task<UserFriendshipDto?> GetFriendshipAsync(Guid friendshipId, Guid userId);

    // Friend requests
    Task<UserFriendshipDto> SendFriendRequestAsync(Guid userId, SendFriendRequestDto dto);
    Task<UserFriendshipDto> RespondToRequestAsync(Guid friendshipId, Guid userId, string action);
    Task RemoveFriendshipAsync(Guid friendshipId, Guid userId);

    // Queries
    Task<List<UserSearchResultDto>> SearchUsersAsync(Guid userId, string query, int limit = 20);
    Task<FriendProfileDto?> GetUserProfileAsync(Guid profileUserId, Guid requestingUserId);

    // Helpers
    Task<bool> AreFriendsAsync(Guid userId1, Guid userId2);
    Task<List<Guid>> GetFriendIdsAsync(Guid userId);
}
