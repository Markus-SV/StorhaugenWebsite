using StorhaugenWebsite.ApiClient;
using StorhaugenWebsite.Shared.DTOs;

namespace StorhaugenWebsite.Services
{
    public class UserRecipeService : IUserRecipeService
    {
        private readonly IApiClient _apiClient;
        private readonly IAuthService _authService;

        public List<UserRecipeDto> CachedRecipes { get; private set; } = new();

        public UserRecipeService(IApiClient apiClient, IAuthService authService)
        {
            _apiClient = apiClient;
            _authService = authService;
        }

        private void ValidateAuthorization()
        {
            if (!_authService.IsAuthorized)
            {
                throw new UnauthorizedAccessException("You are not authorized to perform this action.");
            }
        }

        public async Task<UserRecipePagedResult> GetMyRecipesAsync(GetUserRecipesQuery? query = null)
        {
            if (!_authService.IsAuthenticated)
                throw new UnauthorizedAccessException();

            query ??= new GetUserRecipesQuery();
            var result = await _apiClient.GetMyUserRecipesAsync(query);

            // Update cache with first page results
            if (query.Page == 1 && !query.IncludeArchived)
            {
                CachedRecipes = result.Recipes;
            }

            return result;
        }

        public async Task<UserRecipeDto?> GetRecipeAsync(Guid id)
        {
            if (!_authService.IsAuthenticated)
                throw new UnauthorizedAccessException();

            return await _apiClient.GetUserRecipeAsync(id);
        }

        public async Task<UserRecipeDto> CreateRecipeAsync(CreateUserRecipeDto dto)
        {
            ValidateAuthorization();

            if (string.IsNullOrWhiteSpace(dto.Name) && !dto.GlobalRecipeId.HasValue)
                throw new ArgumentException("Recipe must have either a title or be linked to a global recipe.");

            var result = await _apiClient.CreateUserRecipeAsync(dto);
            InvalidateCache();
            return result;
        }

        public async Task<UserRecipeDto> UpdateRecipeAsync(Guid id, UpdateUserRecipeDto dto)
        {
            ValidateAuthorization();

            var result = await _apiClient.UpdateUserRecipeAsync(id, dto);
            InvalidateCache();
            return result;
        }

        public async Task DeleteRecipeAsync(Guid id)
        {
            ValidateAuthorization();

            await _apiClient.DeleteUserRecipeAsync(id);
            InvalidateCache();
        }

        public async Task<PublishRecipeResultDto> PublishRecipeAsync(Guid id)
        {
            ValidateAuthorization();

            var result = await _apiClient.PublishUserRecipeAsync(id);
            InvalidateCache();
            return result;
        }

        public async Task<UserRecipeDto> DetachRecipeAsync(Guid id)
        {
            ValidateAuthorization();

            var result = await _apiClient.DetachUserRecipeAsync(id);
            InvalidateCache();
            return result;
        }

        public async Task<UserRecipeDto> RateRecipeAsync(Guid id, int rating, string? comment = null)
        {
            ValidateAuthorization();

            if (rating < 1 || rating > 10)
                throw new ArgumentException("Rating must be between 1 and 10.");

            var result = await _apiClient.RateUserRecipeAsync(id, rating, comment);
            InvalidateCache();
            return result;
        }

        public async Task RemoveRatingAsync(Guid id)
        {
            ValidateAuthorization();

            await _apiClient.RemoveUserRecipeRatingAsync(id);
            InvalidateCache();
        }

        public async Task<UserRecipeDto> ArchiveRecipeAsync(Guid id)
        {
            ValidateAuthorization();

            var result = await _apiClient.ArchiveUserRecipeAsync(id);
            InvalidateCache();
            return result;
        }

        public async Task<UserRecipeDto> RestoreRecipeAsync(Guid id)
        {
            ValidateAuthorization();

            var result = await _apiClient.RestoreUserRecipeAsync(id);
            InvalidateCache();
            return result;
        }

        public async Task<AggregatedRecipePagedResult> GetHouseholdCombinedRecipesAsync(Guid householdId, GetCombinedRecipesQuery? query = null)
        {
            if (!_authService.IsAuthenticated)
                throw new UnauthorizedAccessException();

            query ??= new GetCombinedRecipesQuery();
            return await _apiClient.GetHouseholdCombinedRecipesAsync(householdId, query);
        }

        public async Task<List<CommonFavoriteDto>> GetHouseholdCommonFavoritesAsync(Guid householdId, int minimumMembers = 2, int limit = 10)
        {
            if (!_authService.IsAuthenticated)
                throw new UnauthorizedAccessException();

            return await _apiClient.GetHouseholdCommonFavoritesAsync(householdId, minimumMembers, limit);
        }

        // Multi-group Aggregation
        public async Task<AggregatedRecipePagedResult> GetGroupsCombinedRecipesAsync(List<Guid> groupIds, GetCombinedRecipesQuery? query = null)
        {
            if (!_authService.IsAuthenticated)
                throw new UnauthorizedAccessException();

            if (groupIds == null || !groupIds.Any())
                return new AggregatedRecipePagedResult();

            query ??= new GetCombinedRecipesQuery();
            var multiGroupQuery = new GetMultiGroupRecipesQuery
            {
                GroupIds = groupIds,
                FilterByMembers = query.FilterByMembers,
                MinRating = query.MinRating,
                Search = query.Search,
                SortBy = query.SortBy,
                SortDescending = query.SortDescending,
                Page = query.Page,
                PageSize = query.PageSize
            };

            return await _apiClient.GetGroupsCombinedRecipesAsync(multiGroupQuery);
        }

        public async Task<List<CommonFavoriteDto>> GetGroupsCommonFavoritesAsync(List<Guid> groupIds, int minimumMembers = 2, int limit = 10)
        {
            if (!_authService.IsAuthenticated)
                throw new UnauthorizedAccessException();

            if (groupIds == null || !groupIds.Any())
                return new List<CommonFavoriteDto>();

            var query = new GetMultiGroupFavoritesQuery
            {
                GroupIds = groupIds,
                MinMembers = minimumMembers,
                Limit = limit
            };

            return await _apiClient.GetGroupsCommonFavoritesAsync(query);
        }

        public async Task<UserRecipePagedResult> GetFriendsRecipesAsync(GetUserRecipesQuery? query = null)
        {
            if (!_authService.IsAuthenticated)
                throw new UnauthorizedAccessException();

            query ??= new GetUserRecipesQuery();
            return await _apiClient.GetFriendsRecipesAsync(query);
        }

        public void InvalidateCache()
        {
            CachedRecipes = new List<UserRecipeDto>();
        }
    }
}
