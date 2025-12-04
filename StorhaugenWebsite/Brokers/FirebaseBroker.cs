using Microsoft.JSInterop;
using StorhaugenWebsite.Models;
using System.Text.Json;

namespace StorhaugenWebsite.Brokers
{
    public class FirebaseBroker : IFirebaseBroker, IAsyncDisposable
    {
        private readonly Lazy<Task<IJSObjectReference>> _moduleTask;

        public FirebaseBroker(IJSRuntime jsRuntime)
        {
            _moduleTask = new(() => jsRuntime.InvokeAsync<IJSObjectReference>(
                "import", "./js/firebaseInterop.js").AsTask());
        }

        public async Task<string?> LoginWithGoogleAsync()
        {
            var module = await _moduleTask.Value;
            return await module.InvokeAsync<string?>("loginWithGoogle");
        }

        public async Task<string?> GetCurrentUserEmailAsync()
        {
            var module = await _moduleTask.Value;
            return await module.InvokeAsync<string?>("getCurrentUserEmail");
        }

        public async Task SignOutAsync()
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("signOut");
        }

        public async Task<string> AddFoodItemAsync(FoodItem foodItem)
        {
            var module = await _moduleTask.Value;
            var foodData = new
            {
                name = foodItem.Name,
                description = foodItem.Description ?? "",
                imageUrls = foodItem.ImageUrls,
                ratings = foodItem.Ratings,
                dateAdded = foodItem.DateAdded.ToString("o"),
                addedBy = foodItem.AddedBy,
                isArchived = false
            };
            return await module.InvokeAsync<string>("addFoodItem", foodData);
        }

        public async Task UpdateFoodItemAsync(FoodItem foodItem)
        {
            var module = await _moduleTask.Value;
            var foodData = new
            {
                id = foodItem.Id,
                name = foodItem.Name,
                description = foodItem.Description ?? "",
                imageUrls = foodItem.ImageUrls,
                ratings = foodItem.Ratings,
                dateAdded = foodItem.DateAdded.ToString("o"),
                addedBy = foodItem.AddedBy,
                isArchived = foodItem.IsArchived,
                archivedDate = foodItem.ArchivedDate?.ToString("o"),
                archivedBy = foodItem.ArchivedBy
            };
            await module.InvokeVoidAsync("updateFoodItem", foodData);
        }

        public async Task<List<FoodItem>> GetFoodItemsAsync(bool includeArchived = false)
        {
            var module = await _moduleTask.Value;
            var items = await module.InvokeAsync<List<FoodItemDto>>("getFoodItems", includeArchived);
            return items?.Select(MapToFoodItem).ToList() ?? new List<FoodItem>();
        }

        public async Task<FoodItem?> GetFoodItemByIdAsync(string id)
        {
            var module = await _moduleTask.Value;
            var item = await module.InvokeAsync<FoodItemDto?>("getFoodItemById", id);
            return item != null ? MapToFoodItem(item) : null;
        }

        public async Task ArchiveFoodItemAsync(string id, string archivedBy)
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("archiveFoodItem", id, archivedBy, DateTime.UtcNow.ToString("o"));
        }

        public async Task RestoreFoodItemAsync(string id)
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("restoreFoodItem", id);
        }

        public async Task<string> UploadImageAsync(byte[] imageData, string fileName)
        {
            var module = await _moduleTask.Value;
            var base64 = Convert.ToBase64String(imageData);
            return await module.InvokeAsync<string>("uploadImage", base64, fileName);
        }

        public async Task DeleteImageAsync(string imageUrl)
        {
            var module = await _moduleTask.Value;
            await module.InvokeVoidAsync("deleteImage", imageUrl);
        }

        private FoodItem MapToFoodItem(FoodItemDto dto)
        {
            var ratings = new Dictionary<string, int?>();
            if (dto.Ratings != null)
            {
                foreach (var kvp in dto.Ratings)
                {
                    ratings[kvp.Key] = kvp.Value;
                }
            }
            else
            {
                ratings = new Dictionary<string, int?>
                {
                    { "Markus", null },
                    { "Siv", null },
                    { "Elias", null }
                };
            }

            return new FoodItem
            {
                Id = dto.Id ?? string.Empty,
                Name = dto.Name ?? string.Empty,
                Description = dto.Description,
                ImageUrls = dto.ImageUrls ?? new List<string>(),
                Ratings = ratings,
                DateAdded = DateTime.TryParse(dto.DateAdded, out var date) ? date : DateTime.UtcNow,
                AddedBy = dto.AddedBy ?? string.Empty,
                IsArchived = dto.IsArchived,
                ArchivedDate = DateTime.TryParse(dto.ArchivedDate, out var archDate) ? archDate : null,
                ArchivedBy = dto.ArchivedBy
            };
        }

        public async ValueTask DisposeAsync()
        {
            if (_moduleTask.IsValueCreated)
            {
                var module = await _moduleTask.Value;
                await module.DisposeAsync();
            }
        }

        private class FoodItemDto
        {
            public string? Id { get; set; }
            public string? Name { get; set; }
            public string? Description { get; set; }
            public List<string>? ImageUrls { get; set; }
            public Dictionary<string, int?>? Ratings { get; set; }
            public string? DateAdded { get; set; }
            public string? AddedBy { get; set; }
            public bool IsArchived { get; set; }
            public string? ArchivedDate { get; set; }
            public string? ArchivedBy { get; set; }
        }
    }
}