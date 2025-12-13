using StorhaugenWebsite.Shared.DTOs;

namespace StorhaugenWebsite.ApiClient;

public interface IApiClient
{
    // User
    Task<UserDto?> GetMyProfileAsync();
    Task<UserDto> UpdateMyProfileAsync(UpdateUserDto dto);

    // Households
    Task<List<HouseholdDto>> GetMyHouseholdsAsync();
    Task<HouseholdDto?> GetHouseholdAsync(Guid id);
    Task<HouseholdDto> CreateHouseholdAsync(CreateHouseholdDto dto);
    Task<HouseholdDto> UpdateHouseholdSettingsAsync(Guid id, UpdateHouseholdSettingsDto dto);
    Task<HouseholdDto> UpdateHouseholdNameAsync(Guid id, UpdateHouseholdDto dto);
    Task<HouseholdDto> RegenerateHouseholdShareIdAsync(Guid id);
    Task SwitchHouseholdAsync(Guid householdId);
    Task<List<HouseholdInviteDto>> GetPendingInvitesAsync();
    Task AcceptInviteAsync(Guid inviteId);
    Task RejectInviteAsync(Guid inviteId);
    Task InviteToHouseholdAsync(Guid householdId, InviteToHouseholdDto dto);
    Task LeaveHouseholdAsync(Guid householdId);
    Task<List<HouseholdSearchResultDto>> SearchHouseholdsAsync(string query);
    Task<List<HouseholdFriendshipDto>> GetHouseholdFriendshipsAsync();
    Task<HouseholdFriendshipDto> SendHouseholdFriendRequestAsync(SendHouseholdFriendRequestDto dto);
    Task<HouseholdFriendshipDto> RespondHouseholdFriendRequestAsync(Guid requestId, RespondFriendRequestDto dto);
    Task<PublicRecipeDto?> GetPublicRecipeAsync(Guid id);

    // Household Recipes
    Task<List<HouseholdRecipeDto>> GetRecipesAsync(bool includeArchived = false);
    Task<HouseholdRecipeDto?> GetRecipeAsync(Guid id);
    Task<HouseholdRecipeDto> CreateRecipeAsync(CreateHouseholdRecipeDto dto);
    Task<HouseholdRecipeDto> UpdateRecipeAsync(Guid id, UpdateHouseholdRecipeDto dto);
    Task ArchiveRecipeAsync(Guid id);
    Task RestoreRecipeAsync(Guid id);
    Task RateRecipeAsync(Guid id, int rating);
    Task ForkRecipeAsync(Guid id);
    Task DeleteRecipeAsync(Guid id);

    // Global Recipes
    Task<GlobalRecipePagedResult> BrowseGlobalRecipesAsync(BrowseGlobalRecipesQuery query);
    Task<GlobalRecipeDto?> GetGlobalRecipeAsync(Guid id);
    Task<List<GlobalRecipeDto>> SearchGlobalRecipesAsync(string query, int limit = 20);
    Task DeleteGlobalRecipeAsync(Guid id);

    // Public Household Recipes (community recipes)
    Task<PublicRecipePagedResult> BrowsePublicRecipesAsync(BrowsePublicRecipesQuery query);

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

    // Household Recipe Aggregation (single group - backwards compatible)
    Task<AggregatedRecipePagedResult> GetHouseholdCombinedRecipesAsync(Guid householdId, GetCombinedRecipesQuery query);
    Task<List<CommonFavoriteDto>> GetHouseholdCommonFavoritesAsync(Guid householdId, int minimumMembers = 2, int limit = 10);

    // Multi-group Recipe Aggregation
    Task<AggregatedRecipePagedResult> GetGroupsCombinedRecipesAsync(GetMultiGroupRecipesQuery query);
    Task<List<CommonFavoriteDto>> GetGroupsCommonFavoritesAsync(GetMultiGroupFavoritesQuery query);

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

