using StorhaugenWebsite.DTOs;

namespace StorhaugenWebsite.Services
{
    public interface IUserFriendshipService
    {
        // Friendship queries
        Task<FriendshipListDto> GetFriendshipsAsync();
        Task<List<FriendProfileDto>> GetFriendsAsync();
        Task<UserFriendshipDto?> GetFriendshipAsync(Guid id);

        // Friend requests
        Task<UserFriendshipDto> SendFriendRequestAsync(Guid targetUserId);
        Task<UserFriendshipDto> SendFriendRequestByEmailAsync(string email);
        Task<UserFriendshipDto> AcceptFriendRequestAsync(Guid friendshipId);
        Task<UserFriendshipDto> RejectFriendRequestAsync(Guid friendshipId);
        Task RemoveFriendAsync(Guid friendshipId);

        // Search and profiles
        Task<List<UserSearchResultDto>> SearchUsersAsync(string query, int limit = 20);
        Task<FriendProfileDto?> GetUserProfileAsync(Guid userId);

        // Cache
        List<FriendProfileDto> CachedFriends { get; }
        int PendingRequestCount { get; }
        void InvalidateCache();
    }
}
