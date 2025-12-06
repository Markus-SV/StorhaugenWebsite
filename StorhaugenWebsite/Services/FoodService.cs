using StorhaugenWebsite.ApiClient;
using StorhaugenWebsite.Models;
using StorhaugenWebsite.DTOs;

namespace StorhaugenWebsite.Services
{
    public class FoodService : IFoodService
    {
        private readonly IApiClient _apiClient;
        private readonly IAuthService _authService;

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

            var recipes = await _apiClient.GetRecipesAsync(includeArchived);
            return recipes.Select(MapToFoodItem).ToList();
        }

        public async Task<FoodItem?> GetFoodByIdAsync(string id)
        {
            if (!Guid.TryParse(id, out var recipeId))
                return null;

            var recipe = await _apiClient.GetRecipeAsync(recipeId);
            return recipe != null ? MapToFoodItem(recipe) : null;
        }

        public async Task<string> AddFoodAsync(FoodItem food)
        {
            ValidateAuthorization();

            if (string.IsNullOrWhiteSpace(food.Name))
                throw new ArgumentException("Food name cannot be empty.");

            var dto = new CreateHouseholdRecipeDto
            {
                Name = food.Name,
                Description = food.Description,
                ImageUrls = food.ImageUrls,
                PersonalNotes = null
            };

            var created = await _apiClient.CreateRecipeAsync(dto);
            return created.Id.ToString();
        }

        public async Task UpdateFoodAsync(FoodItem food)
        {
            ValidateAuthorization();

            if (string.IsNullOrWhiteSpace(food.Id)) throw new ArgumentException("ID required.");
            if (string.IsNullOrWhiteSpace(food.Name)) throw new ArgumentException("Name required.");

            if (!Guid.TryParse(food.Id, out var recipeId))
                throw new ArgumentException("Invalid ID format.");

            var dto = new UpdateHouseholdRecipeDto
            {
                Name = food.Name,
                Description = food.Description,
                ImageUrls = food.ImageUrls,
                PersonalNotes = food.PersonalNotes
            };

            await _apiClient.UpdateRecipeAsync(recipeId, dto);
        }

        public async Task ArchiveFoodAsync(string id, string archivedBy)
        {
            ValidateAuthorization();

            if (!Guid.TryParse(id, out var recipeId))
                throw new ArgumentException("Invalid ID format.");

            await _apiClient.ArchiveRecipeAsync(recipeId);
        }

        public async Task RestoreFoodAsync(string id)
        {
            ValidateAuthorization();

            if (!Guid.TryParse(id, out var recipeId))
                throw new ArgumentException("Invalid ID format.");

            await _apiClient.RestoreRecipeAsync(recipeId);
        }

        public async Task UpdateRatingAsync(string foodId, string personName, int rating)
        {
            ValidateAuthorization();

            if (rating < 0 || rating > 10) throw new ArgumentException("Invalid rating.");

            if (!Guid.TryParse(foodId, out var recipeId))
                throw new ArgumentException("Invalid ID format.");

            await _apiClient.RateRecipeAsync(recipeId, rating);
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

            await _apiClient.ForkRecipeAsync(recipeId);
        }

        // Map HouseholdRecipeDto to FoodItem for backward compatibility
        private FoodItem MapToFoodItem(HouseholdRecipeDto recipe)
        {
            return new FoodItem
            {
                Id = recipe.Id.ToString(),
                Name = recipe.Name,
                Description = recipe.Description,
                ImageUrls = recipe.ImageUrls,
                Ratings = recipe.Ratings,
                DateAdded = recipe.DateAdded,
                AddedBy = recipe.AddedByName ?? "Unknown",
                IsArchived = recipe.IsArchived,
                ArchivedDate = recipe.ArchivedDate,
                ArchivedBy = recipe.ArchivedByName,
                GlobalRecipeId = recipe.GlobalRecipeId,
                GlobalRecipeName = recipe.GlobalRecipeName,
                IsForked = recipe.IsForked,
                PersonalNotes = recipe.PersonalNotes
            };
        }
    }
}
