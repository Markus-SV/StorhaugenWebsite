using StorhaugenWebsite.Shared.DTOs;

namespace StorhaugenWebsite.ApiClient;

public interface IApiClient
{
    // User
    Task<UserDto?> GetMyProfileAsync();
    Task<UserDto> UpdateMyProfileAsync(UpdateUserDto dto);

    // Collections
    Task<List<CollectionDto>> GetMyCollectionsAsync();
    Task<CollectionDto?> GetCollectionAsync(Guid id);
    Task<CollectionDto?> GetCollectionByShareCodeAsync(string shareCode);
    Task<CollectionDto> CreateCollectionAsync(CreateCollectionDto dto);
    Task<CollectionDto> UpdateCollectionAsync(Guid id, UpdateCollectionDto dto);
    Task DeleteCollectionAsync(Guid id);
    Task<CollectionRecipesResult> GetCollectionRecipesAsync(Guid id, GetCollectionRecipesQuery? query = null);
    Task AddRecipeToCollectionAsync(Guid collectionId, AddRecipeToCollectionDto dto);
    Task RemoveRecipeFromCollectionAsync(Guid collectionId, Guid recipeId);
    Task<List<CollectionMemberDto>> GetCollectionMembersAsync(Guid collectionId);
    Task AddCollectionMemberAsync(Guid collectionId, AddCollectionMemberDto dto);
    Task RemoveCollectionMemberAsync(Guid collectionId, Guid memberId);
    Task LeaveCollectionAsync(Guid collectionId);

    // Global Recipes
    Task<GlobalRecipePagedResult> BrowseGlobalRecipesAsync(BrowseGlobalRecipesQuery query);
    Task<GlobalRecipeDto?> GetGlobalRecipeAsync(Guid id);
    Task<List<GlobalRecipeDto>> SearchGlobalRecipesAsync(string query, int limit = 20);
    Task DeleteGlobalRecipeAsync(Guid id);

    // Storage
    Task<UploadImageResultDto> UploadImageAsync(byte[] imageData, string fileName);
    Task DeleteImageAsync(string fileName);

    // User Recipes (user-centric recipe management)
    Task<UserRecipePagedResult> GetMyUserRecipesAsync(GetUserRecipesQuery query);
    Task<UserRecipeDto?> GetUserRecipeAsync(Guid id);
    Task<UserRecipeDto> CreateUserRecipeAsync(CreateUserRecipeDto dto);
    Task<UserRecipeDto> UpdateUserRecipeAsync(Guid id, UpdateUserRecipeDto dto);
    Task DeleteUserRecipeAsync(Guid id);
    Task<PublishRecipeResultDto> PublishUserRecipeAsync(Guid id);
    Task<UserRecipeDto> DetachUserRecipeAsync(Guid id);
    Task<UserRecipeDto> RateUserRecipeAsync(Guid id, int rating, string? comment = null);
    Task RemoveUserRecipeRatingAsync(Guid id);
    Task<UserRecipeDto> ArchiveUserRecipeAsync(Guid id);
    Task<UserRecipeDto> RestoreUserRecipeAsync(Guid id);
    Task<UserRecipePagedResult> GetFriendsRecipesAsync(GetUserRecipesQuery query);

    // User Friendships
    Task<FriendshipListDto> GetFriendshipsAsync();
    Task<List<FriendProfileDto>> GetFriendsAsync();
    Task<UserFriendshipDto?> GetFriendshipAsync(Guid id);
    Task<UserFriendshipDto> SendFriendRequestAsync(SendFriendRequestDto dto);
    Task<UserFriendshipDto> RespondToFriendRequestAsync(Guid id, RespondFriendRequestDto action);
    Task RemoveFriendshipAsync(Guid id);
    Task<List<UserSearchResultDto>> SearchUsersAsync(string query, int limit = 20);
    Task<FriendProfileDto?> GetUserProfileAsync(Guid userId);

    // Activity Feed
    Task<ActivityFeedPagedResult> GetFeedAsync(ActivityFeedQuery query);
    Task<ActivityFeedPagedResult> GetMyActivityAsync(int page = 1, int pageSize = 20);
    Task<ActivitySummaryDto> GetActivitySummaryAsync();

    // Tags (personal recipe organization)
    Task<List<TagDto>> GetMyTagsAsync();
    Task<TagDto?> GetTagAsync(Guid id);
    Task<TagDto> CreateTagAsync(CreateTagDto dto);
    Task<TagDto> UpdateTagAsync(Guid id, UpdateTagDto dto);
    Task DeleteTagAsync(Guid id);
    Task<List<TagReferenceDto>> GetRecipeTagsAsync(Guid recipeId);
    Task SetRecipeTagsAsync(Guid recipeId, List<Guid> tagIds);

    // Ratings
    Task<List<UserRatingDto>> GetUserRatingsAsync(Guid userId, int skip = 0, int take = 50);

    // HelloFresh
    Task<HelloFreshRawResponse?> GetHelloFreshTestRawAsync();
    Task<HelloFreshSyncResult> TriggerHelloFreshSyncAsync(bool force = false);
    Task<HelloFreshSyncStatus?> GetHelloFreshSyncStatusAsync();
    Task<List<string>> GetAvailableHelloFreshWeeksAsync();
}
