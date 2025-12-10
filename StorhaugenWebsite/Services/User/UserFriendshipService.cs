using StorhaugenWebsite.ApiClient;
using StorhaugenWebsite.Shared.DTOs;

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

			// FIX 1: Corrected class name from 'SendUserFriendRequestDto' to 'SendFriendRequestDto'
			var dto = new SendFriendRequestDto
			{
				TargetUserId = targetUserId
			};

			var result = await _apiClient.SendFriendRequestAsync(dto);
			InvalidateCache();
			return result;
		}

		public async Task<UserFriendshipDto> SendFriendRequestByEmailAsync(string email)
		{
			ValidateAuthentication();

			if (string.IsNullOrWhiteSpace(email))
				throw new ArgumentException("Email is required.");

			// WARNING: Your backend 'SendFriendRequestDto' does NOT have an Email property.
			// You must add 'public string? TargetEmail { get; set; }' to the DTO in the Shared project
			// and handle it in the Backend Controller for this to work.

			// Assuming you will add it, the code would look like this:
			/*
			var dto = new SendFriendRequestDto { TargetEmail = email };
			var result = await _apiClient.SendFriendRequestAsync(dto);
			InvalidateCache();
			return result;
			*/

			throw new NotImplementedException("The backend DTO is missing the 'TargetEmail' property.");
		}

		public async Task<UserFriendshipDto> AcceptFriendRequestAsync(Guid friendshipId)
		{
			ValidateAuthentication();

			// FIX 2: Create the expected DTO with the string action "accept"
			var dto = new RespondFriendRequestDto { Action = "accept" };

			// Update ApiClient call to pass the object, not an enum
			var result = await _apiClient.RespondToFriendRequestAsync(friendshipId, dto);

			InvalidateCache();
			return result;
		}

		public async Task<UserFriendshipDto> RejectFriendRequestAsync(Guid friendshipId)
		{
			ValidateAuthentication();

			// FIX 2: Create the expected DTO with the string action "reject"
			var dto = new RespondFriendRequestDto { Action = "reject" };

			// Update ApiClient call to pass the object, not an enum
			var result = await _apiClient.RespondToFriendRequestAsync(friendshipId, dto);

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
