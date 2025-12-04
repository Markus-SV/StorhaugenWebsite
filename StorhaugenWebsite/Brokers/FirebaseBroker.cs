using Microsoft.JSInterop;
using StorhaugenWebsite.Models;

namespace StorhaugenWebsite.Brokers
{
    public class FirebaseBroker : IFirebaseBroker, IAsyncDisposable
    {
        private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

        public FirebaseBroker(IJSRuntime jsRuntime)
        {
            // We use Lazy loading to ensure the JS file is imported only when needed
            // and only once.
            _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./js/firebaseInterop.js").AsTask());
        }

        public async Task<string> LoginWithGoogleAsync()
        {
            var module = await _moduleTask.Value;
            return await module.InvokeAsync<string>("loginWithGoogle");
        }

        public async Task AddFoodItemAsync(FoodItem foodItem)
        {
            var module = await _moduleTask.Value;
            // We pass the individual properties as expected by your JS function
            await module.InvokeVoidAsync("addFoodItem", foodItem.Name, foodItem.Rating);
        }

        public async Task<List<FoodItem>> GetFoodItemsAsync()
        {
            var module = await _moduleTask.Value;
            return await module.InvokeAsync<List<FoodItem>>("getFoodItems");
        }

        public async ValueTask DisposeAsync()
        {
            if (_moduleTask.IsValueCreated)
            {
                var module = await _moduleTask.Value;
                await module.DisposeAsync();
            }
        }
    }
}