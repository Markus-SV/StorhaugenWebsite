using StorhaugenWebsite.ApiClient;
using StorhaugenWebsite.Models;
using StorhaugenWebsite.Shared.DTOs;

namespace StorhaugenWebsite.Services
{
    /// <summary>
    /// Legacy service for FoodItem compatibility.
    /// Wraps the user-centric recipe APIs.
    /// </summary>
    public class FoodService : IFoodService
    {
        private readonly IApiClient _apiClient;
        private readonly IAuthService _authService;
        public FoodItem? DraftRecipe { get; set; }
        public List<FoodItem> CachedFoods { get; private set; } = new();

        public FoodService(IApiClient apiClient, IAuthService authService)
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

        public async Task<List<FoodItem>> GetAllFoodsAsync(bool includeArchived = false)
        {
            if (!_authService.IsAuthenticated) throw new UnauthorizedAccessException();

            var query = new GetUserRecipesQuery { IncludeArchived = includeArchived };
            var result = await _apiClient.GetMyUserRecipesAsync(query);
            var mappedRecipes = result.Recipes.Select(MapToFoodItem).ToList();

            // Update the cache if we are fetching the standard active list
            if (!includeArchived)
            {
                CachedFoods = mappedRecipes;
            }

            return mappedRecipes;
        }

        public async Task<FoodItem?> GetFoodByIdAsync(string id)
        {
            if (!Guid.TryParse(id, out var recipeId))
                return null;

            var recipe = await _apiClient.GetUserRecipeAsync(recipeId);
            return recipe != null ? MapToFoodItem(recipe) : null;
        }

        public async Task<string> AddFoodAsync(FoodItem food)
        {
            ValidateAuthorization();

            if (string.IsNullOrWhiteSpace(food.Name))
                throw new ArgumentException("Food name cannot be empty.");

            var dto = new CreateUserRecipeDto
            {
                Name = food.Name,
                Description = food.Description,
                ImageUrls = food.ImageUrls,
                PersonalNotes = food.PersonalNotes,
                Visibility = food.IsPublic ? "public" : "private"
            };

            var created = await _apiClient.CreateUserRecipeAsync(dto);

            // Add the user's rating if provided
            foreach (var rating in food.Ratings.Where(r => r.Value.HasValue))
            {
                await _apiClient.RateUserRecipeAsync(created.Id, rating.Value!.Value);
            }

            return created.Id.ToString();
        }

        public async Task UpdateFoodAsync(FoodItem food)
        {
            ValidateAuthorization();

            if (string.IsNullOrWhiteSpace(food.Id)) throw new ArgumentException("ID required.");
            if (string.IsNullOrWhiteSpace(food.Name)) throw new ArgumentException("Name required.");

            if (!Guid.TryParse(food.Id, out var recipeId))
                throw new ArgumentException("Invalid ID format.");

            var dto = new UpdateUserRecipeDto
            {
                Name = food.Name,
                Description = food.Description,
                ImageUrls = food.ImageUrls,
                PersonalNotes = food.PersonalNotes
            };

            await _apiClient.UpdateUserRecipeAsync(recipeId, dto);
        }

        public async Task ArchiveFoodAsync(string id, string archivedBy)
        {
            ValidateAuthorization();

            if (!Guid.TryParse(id, out var recipeId))
                throw new ArgumentException("Invalid ID format.");

            await _apiClient.ArchiveUserRecipeAsync(recipeId);
        }

        public async Task RestoreFoodAsync(string id)
        {
            ValidateAuthorization();

            if (!Guid.TryParse(id, out var recipeId))
                throw new ArgumentException("Invalid ID format.");

            await _apiClient.RestoreUserRecipeAsync(recipeId);
        }

        public async Task DeleteFoodAsync(string id)
        {
            ValidateAuthorization();

            if (!Guid.TryParse(id, out var recipeId))
                throw new ArgumentException("Invalid ID format.");

            await _apiClient.DeleteUserRecipeAsync(recipeId);
        }

        public async Task UpdateRatingAsync(string foodId, string personName, decimal rating)
        {
            ValidateAuthorization();

            if (rating < 0m || rating > 10m) throw new ArgumentException("Invalid rating.");

            if (!Guid.TryParse(foodId, out var recipeId))
                throw new ArgumentException("Invalid ID format.");

            await _apiClient.RateUserRecipeAsync(recipeId, rating);
        }

        public async Task<string> UploadImageAsync(byte[] imageData, string fileName)
        {
            ValidateAuthorization();

            if (imageData == null || imageData.Length == 0)
                throw new ArgumentException("Image data is required.");

            var result = await _apiClient.UploadImageAsync(imageData, fileName);
            return result.Url;
        }

        public async Task ForkRecipeAsync(string id)
        {
            ValidateAuthorization();

            if (!Guid.TryParse(id, out var recipeId))
                throw new ArgumentException("Invalid ID format.");

            // Fork is now "detach" - creates a local copy
            await _apiClient.DetachUserRecipeAsync(recipeId);
        }

        public async Task SetPublicStatusAsync(string id, bool isPublic)
        {
            ValidateAuthorization();

            if (!Guid.TryParse(id, out var recipeId))
                throw new ArgumentException("Invalid ID format.");

            var dto = new UpdateUserRecipeDto
            {
                Visibility = isPublic ? "public" : "private"
            };

            await _apiClient.UpdateUserRecipeAsync(recipeId, dto);
        }

        // Map UserRecipeDto to FoodItem for backward compatibility
        private FoodItem MapToFoodItem(UserRecipeDto recipe)
        {
            return new FoodItem
            {
                Id = recipe.Id.ToString(),
                Name = recipe.Name,
                Description = recipe.Description,
                ImageUrls = recipe.ImageUrls,
                Ratings = recipe.MemberRatings ?? new Dictionary<string, decimal?>(),
                DateAdded = recipe.CreatedAt,
                AddedBy = recipe.UserDisplayName ?? "Unknown",
                IsArchived = recipe.IsArchived,
                ArchivedDate = null,
                ArchivedBy = null,
                GlobalRecipeId = recipe.GlobalRecipeId,
                GlobalRecipeName = recipe.GlobalRecipeName,
                IsForked = false,
                PersonalNotes = recipe.PersonalNotes,
                IsPublic = recipe.Visibility == "public",
                PrepTimeMinutes = recipe.PrepTimeMinutes,
                CookTimeMinutes = recipe.CookTimeMinutes,
                Servings = recipe.Servings,
                Difficulty = recipe.Difficulty,
                Cuisine = recipe.Cuisine,
                Ingredients = recipe.Ingredients,
                NutritionData = recipe.NutritionData
            };
        }
    }
}
