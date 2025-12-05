using StorhaugenWebsite.Brokers;
using StorhaugenWebsite.Models;

namespace StorhaugenWebsite.Services
{
    public class FoodService : IFoodService
    {
        private readonly IFirebaseBroker _firebaseBroker;
        private readonly IAuthService _authService; 

        public FoodService(IFirebaseBroker firebaseBroker, IAuthService authService)
        {
            _firebaseBroker = firebaseBroker;
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

            return await _firebaseBroker.GetFoodItemsAsync(includeArchived);
        }

        public async Task<FoodItem?> GetFoodByIdAsync(string id)
        {
            return await _firebaseBroker.GetFoodItemByIdAsync(id);
        }


        public async Task<string> AddFoodAsync(FoodItem food)
        {
            ValidateAuthorization(); 

            if (string.IsNullOrWhiteSpace(food.Name))
                throw new ArgumentException("Food name cannot be empty.");

            return await _firebaseBroker.AddFoodItemAsync(food);
        }

        public async Task UpdateFoodAsync(FoodItem food)
        {
            ValidateAuthorization(); 

            if (string.IsNullOrWhiteSpace(food.Id)) throw new ArgumentException("ID required.");
            if (string.IsNullOrWhiteSpace(food.Name)) throw new ArgumentException("Name required.");

            await _firebaseBroker.UpdateFoodItemAsync(food);
        }

        public async Task ArchiveFoodAsync(string id, string archivedBy)
        {
            ValidateAuthorization(); 
            await _firebaseBroker.ArchiveFoodItemAsync(id, archivedBy);
        }

        public async Task RestoreFoodAsync(string id)
        {
            ValidateAuthorization(); 
            await _firebaseBroker.RestoreFoodItemAsync(id);
        }

        public async Task UpdateRatingAsync(string foodId, string personName, int rating)
        {
            ValidateAuthorization(); 

            if (rating < 0 || rating > 10) throw new ArgumentException("Invalid rating.");

            var food = await _firebaseBroker.GetFoodItemByIdAsync(foodId);
            if (food == null) throw new ArgumentException("Food item not found.");

            food.Ratings[personName] = rating;
            await _firebaseBroker.UpdateFoodItemAsync(food);
        }

        public async Task<string> UploadImageAsync(byte[] imageData, string fileName)
        {
            ValidateAuthorization(); 
            
            if (imageData == null || imageData.Length == 0)
                throw new ArgumentException("Image data is required.");

            return await _firebaseBroker.UploadImageAsync(imageData, fileName);
        }
    }
}