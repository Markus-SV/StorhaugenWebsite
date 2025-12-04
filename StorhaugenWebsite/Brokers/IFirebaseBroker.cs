using StorhaugenWebsite.Models;

namespace StorhaugenWebsite.Brokers
{
    public interface IFirebaseBroker
    {
        Task<string?> LoginWithGoogleAsync();
        Task<string?> GetCurrentUserEmailAsync();
        Task SignOutAsync();
        Task<string> AddFoodItemAsync(FoodItem foodItem);
        Task UpdateFoodItemAsync(FoodItem foodItem);
        Task<List<FoodItem>> GetFoodItemsAsync(bool includeArchived = false);
        Task<FoodItem?> GetFoodItemByIdAsync(string id);
        Task ArchiveFoodItemAsync(string id, string archivedBy);
        Task RestoreFoodItemAsync(string id);
        Task<string> UploadImageAsync(byte[] imageData, string fileName);
        Task DeleteImageAsync(string imageUrl);
    }
}