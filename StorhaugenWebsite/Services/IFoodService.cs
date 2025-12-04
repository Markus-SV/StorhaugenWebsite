using StorhaugenWebsite.Models;

namespace StorhaugenWebsite.Services
{
    public interface IFoodService
    {
        Task<string> LoginAsync();
        Task SubmitFoodReviewAsync(FoodItem food);
        Task<List<FoodItem>> RetrieveAllFoodsAsync();
    }
}