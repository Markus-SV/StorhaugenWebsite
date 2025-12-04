using StorhaugenWebsite.Brokers;
using StorhaugenWebsite.Models;

namespace StorhaugenWebsite.Services
{
    public class FoodService : IFoodService
    {
        private readonly IFirebaseBroker _firebaseBroker;

        public FoodService(IFirebaseBroker firebaseBroker)
        {
            _firebaseBroker = firebaseBroker;
        }

        public async Task<string> AddFoodAsync(FoodItem food)
        {
            if (string.IsNullOrWhiteSpace(food.Name))
                throw new ArgumentException("Food name cannot be empty.");

            return await _firebaseBroker.AddFoodItemAsync(food);
        }

        public async Task UpdateFoodAsync(FoodItem food)
        {
            if (string.IsNullOrWhiteSpace(food.Id))
                throw new ArgumentException("Food ID is required for update.");

            if (string.IsNullOrWhiteSpace(food.Name))
                throw new ArgumentException("Food name cannot be empty.");

            await _firebaseBroker.UpdateFoodItemAsync(food);
        }

        public async Task<List<FoodItem>> GetAllFoodsAsync(bool includeArchived = false)
        {
            return await _firebaseBroker.GetFoodItemsAsync(includeArchived);
        }

        public async Task<FoodItem?> GetFoodByIdAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return null;

            return await _firebaseBroker.GetFoodItemByIdAsync(id);
        }

        public async Task ArchiveFoodAsync(string id, string archivedBy)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Food ID is required.");

            await _firebaseBroker.ArchiveFoodItemAsync(id, archivedBy);
        }

        public async Task RestoreFoodAsync(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                throw new ArgumentException("Food ID is required.");

            await _firebaseBroker.RestoreFoodItemAsync(id);
        }

        public async Task UpdateRatingAsync(string foodId, string personName, int rating)
        {
            if (rating < 0 || rating > 10)
                throw new ArgumentException("Rating must be between 0 and 10.");

            var food = await _firebaseBroker.GetFoodItemByIdAsync(foodId);
            if (food == null)
                throw new ArgumentException("Food item not found.");

            food.Ratings[personName] = rating;
            await _firebaseBroker.UpdateFoodItemAsync(food);
        }

        public async Task<string> UploadImageAsync(byte[] imageData, string fileName)
        {
            if (imageData == null || imageData.Length == 0)
                throw new ArgumentException("Image data is required.");

            return await _firebaseBroker.UploadImageAsync(imageData, fileName);
        }
    }
}