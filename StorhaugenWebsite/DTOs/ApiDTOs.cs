namespace StorhaugenWebsite.DTOs;

// User DTOs
public class UserDto
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public Guid? CurrentHouseholdId { get; set; }
    public required string UniqueShareId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateUserDto
{
    public string? DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public Guid? CurrentHouseholdId { get; set; }
}

// Household DTOs
public class HouseholdDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public Guid CreatedById { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<HouseholdMemberDto> Members { get; set; } = new();
    public string? UniqueShareId { get; set; }
    public bool IsPrivate { get; set; }
}

public class HouseholdMemberDto
{
    public Guid UserId { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public string? AvatarUrl { get; set; }
    public DateTime JoinedAt { get; set; }
}

public class CreateHouseholdDto
{
    public required string Name { get; set; }
}

public class UpdateHouseholdDto
{
    public required string Name { get; set; }
}

public class UpdateHouseholdSettingsDto
{
    public bool? IsPrivate { get; set; }
}

public class InviteToHouseholdDto
{
    public string? Email { get; set; }
    public string? UniqueShareId { get; set; }
}

public class HouseholdInviteDto
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public required string HouseholdName { get; set; }
    public Guid InvitedById { get; set; }
    public required string InvitedByName { get; set; }
    public required string InvitedEmail { get; set; }
    public string Status { get; set; } = "pending";
    public DateTime CreatedAt { get; set; }
}

public class HouseholdSearchResultDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? UniqueShareId { get; set; }
    public int MemberCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsPrivate { get; set; }
}

public class HouseholdFriendshipDto
{
    public Guid Id { get; set; }
    public Guid RequesterHouseholdId { get; set; }
    public required string RequesterHouseholdName { get; set; }
    public Guid TargetHouseholdId { get; set; }
    public required string TargetHouseholdName { get; set; }
    public required string Status { get; set; }
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}

public class SendFriendRequestDto
{
    public string? HouseholdShareId { get; set; }
    public Guid? HouseholdId { get; set; }
    public string? Message { get; set; }
}

public class RespondFriendRequestDto
{
    public required string Action { get; set; }
}

// Recipe DTOs
public class HouseholdRecipeDto
{
    public Guid Id { get; set; }
    public Guid HouseholdId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public Dictionary<string, int?> Ratings { get; set; } = new();
    public double AverageRating { get; set; }
    public DateTime DateAdded { get; set; }
    public Guid AddedByUserId { get; set; }
    public string? AddedByName { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedDate { get; set; }
    public Guid? ArchivedByUserId { get; set; }
    public string? ArchivedByName { get; set; }

    // Multi-tenant fields
    public Guid? GlobalRecipeId { get; set; }
    public string? GlobalRecipeName { get; set; }
    public bool IsForked { get; set; }
    public string? PersonalNotes { get; set; }

    // Public sharing
    public bool IsPublic { get; set; }
    public string? HouseholdName { get; set; }
}

public class CreateHouseholdRecipeDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string>? ImageUrls { get; set; }
    public string? ImageUrl { get; set; }
    public string? PersonalNotes { get; set; }
    public Guid? GlobalRecipeId { get; set; }
    public bool Fork { get; set; } = false;
    public bool IsPublic { get; set; } = false;

    // Additional fields for forking
    public int? PrepTimeMinutes { get; set; }
    public int? Servings { get; set; }
    public string? Difficulty { get; set; }
    public string? Cuisine { get; set; }
    public List<string>? Tags { get; set; }
    public object? Ingredients { get; set; }
    public object? Instructions { get; set; }
}

public class UpdateHouseholdRecipeDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string>? ImageUrls { get; set; }
    public string? PersonalNotes { get; set; }
    public bool? IsPublic { get; set; }
}

public class RateRecipeDto
{
    public int Rating { get; set; } // 0-10
}

// Global Recipe DTOs
public class GlobalRecipeDto
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public object? Ingredients { get; set; }
    public object? NutritionData { get; set; }
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public int? TotalTimeMinutes { get; set; }
    public int? Servings { get; set; }
    public string? Difficulty { get; set; }
    public List<string> Tags { get; set; } = new();
    public string? Cuisine { get; set; }
    public bool IsHellofresh { get; set; }
    public string? HellofreshUuid { get; set; }
    public string? HellofreshSlug { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public string? CreatedByUserName { get; set; }
    public double AverageRating { get; set; }
    public int TotalRatings { get; set; }
    public int TotalTimesAdded { get; set; }
    public DateTime CreatedAt { get; set; }
    public object? Instructions { get; set; }

}

public class GlobalRecipePagedResult
{
    public List<GlobalRecipeDto> Recipes { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class BrowseGlobalRecipesQuery
{
    public string? Search { get; set; }
    public string? Cuisine { get; set; }
    public string? Difficulty { get; set; }
    public int? MaxPrepTime { get; set; }
    public List<string>? Tags { get; set; }
    public bool HellofreshOnly { get; set; } = false;
    public string SortBy { get; set; } = "popular";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

// Public Household Recipes (community recipes)
public class PublicRecipeDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public string? ImageUrl => ImageUrls.FirstOrDefault();
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
    public DateTime DateAdded { get; set; }
    public Guid HouseholdId { get; set; }
    public string HouseholdName { get; set; } = string.Empty;
    public string AddedByName { get; set; } = string.Empty;
}

public class BrowsePublicRecipesQuery
{
    public string? Search { get; set; }
    public string SortBy { get; set; } = "newest";
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class PublicRecipePagedResult
{
    public List<PublicRecipeDto> Recipes { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

// Storage DTOs
public class UploadImageDto
{
    public required string FileName { get; set; }
    public required string Base64Data { get; set; }
    public string Bucket { get; set; } = "recipe-images";
}

public class UploadImageResultDto
{
    public required string Url { get; set; }
    public required string FileName { get; set; }
}

// ==========================================
// USER RECIPE DTOs (New User-Centric Model)
// ==========================================

public class UserRecipeDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public string? UserAvatarUrl { get; set; }

    // Recipe data (resolved from local or global)
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public object? Ingredients { get; set; }

    // Link status
    public Guid? GlobalRecipeId { get; set; }
    public string? GlobalRecipeName { get; set; }
    public bool IsLinkedToGlobal => GlobalRecipeId.HasValue;
    public bool IsPublished { get; set; }

    // Metadata
    public string Visibility { get; set; } = "private";
    public string? PersonalNotes { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Ratings
    public int? MyRating { get; set; }
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
    public Dictionary<string, int?> HouseholdRatings { get; set; } = new();
}

public class CreateUserRecipeDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string>? ImageUrls { get; set; }
    public object? Ingredients { get; set; }
    public string? PersonalNotes { get; set; }
    public Guid? GlobalRecipeId { get; set; }
    public string Visibility { get; set; } = "private";
}

public class UpdateUserRecipeDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string>? ImageUrls { get; set; }
    public object? Ingredients { get; set; }
    public string? PersonalNotes { get; set; }
    public string? Visibility { get; set; }
}

public class PublishRecipeResultDto
{
    public UserRecipeDto UserRecipe { get; set; } = null!;
    public Guid GlobalRecipeId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class GetUserRecipesQuery
{
    public string? Visibility { get; set; }
    public bool IncludeArchived { get; set; } = false;
    public string SortBy { get; set; } = "date";
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class UserRecipePagedResult
{
    public List<UserRecipeDto> Recipes { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class RateUserRecipeDto
{
    public int Rating { get; set; }
    public string? Comment { get; set; }
}

// ==========================================
// USER FRIENDSHIP DTOs
// ==========================================

public class UserFriendshipDto
{
    public Guid Id { get; set; }
    public Guid FriendUserId { get; set; }
    public string FriendDisplayName { get; set; } = string.Empty;
    public string? FriendAvatarUrl { get; set; }
    public string FriendShareId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public int RecipeCount { get; set; }
}

public class FriendshipListDto
{
    public List<UserFriendshipDto> Friends { get; set; } = new();
    public List<UserFriendshipDto> PendingSent { get; set; } = new();
    public List<UserFriendshipDto> PendingReceived { get; set; } = new();
    public int TotalFriends => Friends.Count;
    public int PendingCount => PendingSent.Count + PendingReceived.Count;
}

public class SendUserFriendRequestDto
{
    public Guid? TargetUserId { get; set; }
    public string? TargetShareId { get; set; }
    public string? Message { get; set; }
}

public class RespondUserFriendRequestDto
{
    public string Action { get; set; } = string.Empty;
}

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

public class UserSearchResultDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string ShareId { get; set; } = string.Empty;
    public string FriendshipStatus { get; set; } = "none";
}

// ==========================================
// ACTIVITY FEED DTOs
// ==========================================

public class ActivityFeedItemDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserDisplayName { get; set; } = string.Empty;
    public string? UserAvatarUrl { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string TargetType { get; set; } = string.Empty;
    public Guid TargetId { get; set; }
    public string? RecipeName { get; set; }
    public string? RecipeImageUrl { get; set; }
    public int? RatingScore { get; set; }
    public string? HouseholdName { get; set; }
    public DateTime CreatedAt { get; set; }

    public string Description => ActivityType switch
    {
        "rated" => $"{UserDisplayName} rated {RecipeName ?? "a recipe"} {RatingScore}/10",
        "added" => $"{UserDisplayName} added {RecipeName ?? "a new recipe"}",
        "published" => $"{UserDisplayName} published {RecipeName ?? "a recipe"} to the community",
        "joined_household" => $"{UserDisplayName} joined {HouseholdName ?? "a household"}",
        _ => $"{UserDisplayName} did something"
    };
}

public class ActivityFeedQuery
{
    public List<string>? Types { get; set; }
    public List<Guid>? UserIds { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class ActivityFeedPagedResult
{
    public List<ActivityFeedItemDto> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasMore => Page < TotalPages;
}

public class ActivitySummaryDto
{
    public int TotalActivities { get; set; }
    public int RecipesAdded { get; set; }
    public int RecipesRated { get; set; }
    public int RecipesPublished { get; set; }
    public DateTime? LastActivityDate { get; set; }
}

// ==========================================
// HOUSEHOLD AGGREGATION DTOs
// ==========================================

public class AggregatedRecipeDto
{
    public Guid UserRecipeId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string OwnerDisplayName { get; set; } = string.Empty;
    public string? OwnerAvatarUrl { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public Guid? GlobalRecipeId { get; set; }
    public bool IsLinkedToGlobal => GlobalRecipeId.HasValue;
    public Dictionary<string, int?> HouseholdRatings { get; set; } = new();
    public double HouseholdAverageRating { get; set; }
    public int HouseholdRatingCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CommonFavoriteDto
{
    public Guid GlobalRecipeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Description { get; set; }
    public List<MemberRatingDto> MemberRatings { get; set; } = new();
    public double AverageRating { get; set; }
    public int MembersWhoRated { get; set; }
    public bool IsInMyCollection { get; set; }
}

public class MemberRatingDto
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime RatedAt { get; set; }
}

public class GetCombinedRecipesQuery
{
    public List<Guid>? FilterByMembers { get; set; }
    public double? MinRating { get; set; }
    public string? Search { get; set; }
    public string SortBy { get; set; } = "date";
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class GetCommonFavoritesQuery
{
    public int MinMembers { get; set; } = 2;
    public double MinAverageRating { get; set; } = 4.0;
    public int Limit { get; set; } = 20;
}

public class AggregatedRecipePagedResult
{
    public List<AggregatedRecipeDto> Recipes { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

