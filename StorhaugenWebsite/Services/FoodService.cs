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

        public async Task<string> LoginAsync()
        {
            // You could add logging here
            return await _firebaseBroker.LoginWithGoogleAsync();
        }

        public async Task SubmitFoodReviewAsync(FoodItem food)
        {
            // Validation Logic
            if (string.IsNullOrWhiteSpace(food.Name))
                throw new ArgumentException("Food name cannot be empty.");

            if (food.Rating < 1 || food.Rating > 5)
                throw new ArgumentException("Rating must be between 1 and 5.");

            await _firebaseBroker.AddFoodItemAsync(food);
        }

        public async Task<List<FoodItem>> RetrieveAllFoodsAsync()
        {
            return await _firebaseBroker.GetFoodItemsAsync();
        }
    }
}