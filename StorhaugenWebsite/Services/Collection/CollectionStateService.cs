using StorhaugenWebsite.ApiClient;
using StorhaugenWebsite.Shared.DTOs;

namespace StorhaugenWebsite.Services
{
    public class CollectionStateService : ICollectionStateService
    {
        private readonly IApiClient _apiClient;
        private readonly IAuthService _authService;
        private Guid? _currentUserId;

        public List<CollectionDto> UserCollections { get; private set; } = new();
        public CollectionDto? ActiveCollection { get; private set; }
        public List<CollectionMemberDto> ActiveCollectionMembers { get; private set; } = new();
        public List<Guid> ActiveCollectionFilters { get; private set; } = new();
        public bool HasCollections => UserCollections.Any();

        public event Action? OnStateChanged;
        public event Action? OnActiveFiltersChanged;

        public CollectionStateService(IApiClient apiClient, IAuthService authService)
        {
            _apiClient = apiClient;
            _authService = authService;

            // Subscribe to auth state changes
            _authService.OnAuthStateChanged += HandleAuthStateChanged;
        }

        private async void HandleAuthStateChanged()
        {
            if (_authService.IsAuthenticated)
            {
                await RefreshCollectionsAsync();
            }
            else
            {
                ClearState();
            }
        }

        public async Task InitializeAsync()
        {
            if (_authService.IsAuthenticated)
            {
                await RefreshCollectionsAsync();
            }
        }

        public async Task RefreshCollectionsAsync()
        {
            try
            {
                // Get current user ID
                var profile = await _apiClient.GetMyProfileAsync();
                _currentUserId = profile?.Id;

                // Load collections
                UserCollections = await _apiClient.GetMyCollectionsAsync();

                // If we had an active collection, try to restore it
                if (ActiveCollection != null)
                {
                    var restored = UserCollections.FirstOrDefault(c => c.Id == ActiveCollection.Id);
                    if (restored != null)
                    {
                        ActiveCollection = restored;
                        await LoadActiveCollectionMembersAsync();
                    }
                    else
                    {
                        ActiveCollection = null;
                        ActiveCollectionMembers = new();
                    }
                }

                OnStateChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to refresh collections: {ex.Message}");
                UserCollections = new();
            }
        }

        public async Task SetActiveCollectionAsync(Guid? collectionId)
        {
            if (collectionId == null)
            {
                ActiveCollection = null;
                ActiveCollectionMembers = new();
            }
            else
            {
                ActiveCollection = UserCollections.FirstOrDefault(c => c.Id == collectionId);
                if (ActiveCollection != null)
                {
                    await LoadActiveCollectionMembersAsync();
                }
            }

            OnStateChanged?.Invoke();
        }

        private async Task LoadActiveCollectionMembersAsync()
        {
            if (ActiveCollection == null)
            {
                ActiveCollectionMembers = new();
                return;
            }

            try
            {
                ActiveCollectionMembers = await _apiClient.GetCollectionMembersAsync(ActiveCollection.Id);
            }
            catch
            {
                ActiveCollectionMembers = new();
            }
        }

        public async Task<CollectionDto> CreateCollectionAsync(string name)
        {
            var dto = new CreateCollectionDto { Name = name };
            var created = await _apiClient.CreateCollectionAsync(dto);

            // Add to local list
            UserCollections.Add(created);
            OnStateChanged?.Invoke();

            return created;
        }

        public async Task AddRecipeToCollectionAsync(Guid collectionId, Guid recipeId)
        {
            var dto = new AddRecipeToCollectionDto { UserRecipeId = recipeId };
            await _apiClient.AddRecipeToCollectionAsync(collectionId, dto);
        }

        public async Task RemoveRecipeFromCollectionAsync(Guid collectionId, Guid recipeId)
        {
            await _apiClient.RemoveRecipeFromCollectionAsync(collectionId, recipeId);
        }

        public async Task<List<CollectionMemberDto>> GetCollectionMembersAsync(Guid collectionId)
        {
            return await _apiClient.GetCollectionMembersAsync(collectionId);
        }

        public bool IsOwnerOf(Guid collectionId)
        {
            if (_currentUserId == null) return false;

            var collection = UserCollections.FirstOrDefault(c => c.Id == collectionId);
            return collection?.OwnerId == _currentUserId;
        }

        public bool IsCollectionActive(Guid collectionId)
        {
            return ActiveCollectionFilters.Contains(collectionId);
        }

        public void ToggleCollectionFilter(Guid collectionId)
        {
            if (ActiveCollectionFilters.Contains(collectionId))
            {
                ActiveCollectionFilters.Remove(collectionId);
            }
            else
            {
                ActiveCollectionFilters.Add(collectionId);
            }
            OnActiveFiltersChanged?.Invoke();
        }

        public void ClearState()
        {
            UserCollections = new();
            ActiveCollection = null;
            ActiveCollectionMembers = new();
            ActiveCollectionFilters = new();
            _currentUserId = null;
            OnStateChanged?.Invoke();
        }
    }
}
