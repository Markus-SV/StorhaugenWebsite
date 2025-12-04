using StorhaugenWebsite.Models;

namespace StorhaugenWebsite.Brokers
{
    public interface IFirebaseBroker
    {
        Task<string> LoginWithGoogleAsync();
        Task AddFoodItemAsync(FoodItem foodItem);
        Task<List<FoodItem>> GetFoodItemsAsync();
    }
}