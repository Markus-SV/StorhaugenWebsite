using StorhaugenWebsite.ApiClient;
using StorhaugenWebsite.DTOs;

namespace StorhaugenWebsite.Services
{
    public class UserFriendshipService : IUserFriendshipService
    {
        private readonly IApiClient _apiClient;
        private readonly IAuthService _authService;

        public List<FriendProfileDto> CachedFriends { get; private set; } = new();
        public int PendingRequestCount { get; private set; } = 0;

        public UserFriendshipService(IApiClient apiClient, IAuthService authService)
        {
            _apiClient = apiClient;
            _authService = authService;
        }

        private void ValidateAuthentication()
        {
            if (!_authService.IsAuthenticated)
            {
                throw new UnauthorizedAccessException("You must be logged in to access friendships.");
            }
        }

        public async Task<FriendshipListDto> GetFriendshipsAsync()
        {
            ValidateAuthentication();

            var result = await _apiClient.GetFriendshipsAsync();
            PendingRequestCount = result.PendingReceived?.Count ?? 0;
            return result;
        }

        public async Task<List<FriendProfileDto>> GetFriendsAsync()
        {
            ValidateAuthentication();

            var friends = await _apiClient.GetFriendsAsync();
            CachedFriends = friends;
            return friends;
        }

        public async Task<UserFriendshipDto?> GetFriendshipAsync(Guid id)
        {
            ValidateAuthentication();
            return await _apiClient.GetFriendshipAsync(id);
        }

        public async Task<UserFriendshipDto> SendFriendRequestAsync(Guid targetUserId)
        {
            ValidateAuthentication();

            var dto = new SendUserFriendRequestDto { TargetUserId = targetUserId };
            var result = await _apiClient.SendFriendRequestAsync(dto);
            InvalidateCache();
            return result;
        }

        public async Task<UserFriendshipDto> SendFriendRequestByEmailAsync(string email)
        {
            ValidateAuthentication();

            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email is required.");

            var dto = new SendUserFriendRequestDto { TargetEmail = email };
            var result = await _apiClient.SendFriendRequestAsync(dto);
            InvalidateCache();
            return result;
        }

        public async Task<UserFriendshipDto> AcceptFriendRequestAsync(Guid friendshipId)
        {
            ValidateAuthentication();

            var result = await _apiClient.RespondToFriendRequestAsync(friendshipId, FriendRequestAction.Accept);
            InvalidateCache();
            return result;
        }

        public async Task<UserFriendshipDto> RejectFriendRequestAsync(Guid friendshipId)
        {
            ValidateAuthentication();

            var result = await _apiClient.RespondToFriendRequestAsync(friendshipId, FriendRequestAction.Reject);
            InvalidateCache();
            return result;
        }

        public async Task RemoveFriendAsync(Guid friendshipId)
        {
            ValidateAuthentication();

            await _apiClient.RemoveFriendshipAsync(friendshipId);
            InvalidateCache();
        }

        public async Task<List<UserSearchResultDto>> SearchUsersAsync(string query, int limit = 20)
        {
            ValidateAuthentication();

            if (string.IsNullOrWhiteSpace(query))
                return new List<UserSearchResultDto>();

            return await _apiClient.SearchUsersAsync(query, limit);
        }

        public async Task<FriendProfileDto?> GetUserProfileAsync(Guid userId)
        {
            ValidateAuthentication();
            return await _apiClient.GetUserProfileAsync(userId);
        }

        public void InvalidateCache()
        {
            CachedFriends = new List<FriendProfileDto>();
            PendingRequestCount = 0;
        }
    }
}
