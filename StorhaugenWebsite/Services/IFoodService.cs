using StorhaugenWebsite.Models;

namespace StorhaugenWebsite.Services
{
    public interface IFoodService
    {
        Task<string> AddFoodAsync(FoodItem food);
        Task UpdateFoodAsync(FoodItem food);
        Task<List<FoodItem>> GetAllFoodsAsync(bool includeArchived = false);
        Task<FoodItem?> GetFoodByIdAsync(string id);
        Task ArchiveFoodAsync(string id, string archivedBy);
        Task RestoreFoodAsync(string id);
        Task UpdateRatingAsync(string foodId, string personName, int rating);
        Task<string> UploadImageAsync(byte[] imageData, string fileName);
    }
}