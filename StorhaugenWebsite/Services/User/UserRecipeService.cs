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

        public async Task<UserRecipeDto> RateRecipeAsync(Guid id, decimal rating, string? comment = null)
        {
            ValidateAuthorization();

            if (rating < 0 || rating > 10)
                throw new ArgumentException("Rating must be between 0 and 10.");

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
