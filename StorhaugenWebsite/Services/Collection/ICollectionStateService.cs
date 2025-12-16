using StorhaugenWebsite.Shared.DTOs;

namespace StorhaugenWebsite.Services
{
    public interface ICollectionStateService
    {
        /// <summary>
        /// All collections the current user owns or is a member of.
        /// </summary>
        List<CollectionDto> UserCollections { get; }

        /// <summary>
        /// Currently selected collection for filtering/adding recipes.
        /// </summary>
        CollectionDto? ActiveCollection { get; }

        /// <summary>
        /// Members of the currently active collection.
        /// </summary>
        List<CollectionMemberDto> ActiveCollectionMembers { get; }

        /// <summary>
        /// Collections currently active in filter (for CookBook multi-select).
        /// </summary>
        List<Guid> ActiveCollectionFilters { get; }

        /// <summary>
        /// Whether user has any collections.
        /// </summary>
        bool HasCollections { get; }

        /// <summary>
        /// Event raised when collections or active collection changes.
        /// </summary>
        event Action? OnStateChanged;

        /// <summary>
        /// Event raised when active collection filters change.
        /// </summary>
        event Action? OnActiveFiltersChanged;

        /// <summary>
        /// Initialize the service and load user's collections.
        /// </summary>
        Task InitializeAsync();

        /// <summary>
        /// Refresh collections from the API.
        /// </summary>
        Task RefreshCollectionsAsync();

        /// <summary>
        /// Set the active collection for filtering/operations.
        /// </summary>
        Task SetActiveCollectionAsync(Guid? collectionId);

        /// <summary>
        /// Create a new collection and add it to the list.
        /// </summary>
        Task<CollectionDto> CreateCollectionAsync(string name);

        /// <summary>
        /// Add a recipe to a collection.
        /// </summary>
        Task AddRecipeToCollectionAsync(Guid collectionId, Guid recipeId);

        /// <summary>
        /// Remove a recipe from a collection.
        /// </summary>
        Task RemoveRecipeFromCollectionAsync(Guid collectionId, Guid recipeId);

        /// <summary>
        /// Get members of a specific collection.
        /// </summary>
        Task<List<CollectionMemberDto>> GetCollectionMembersAsync(Guid collectionId);

        /// <summary>
        /// Check if the current user is the owner of a collection.
        /// </summary>
        bool IsOwnerOf(Guid collectionId);

        /// <summary>
        /// Check if a collection is active in the filter.
        /// </summary>
        bool IsCollectionActive(Guid collectionId);

        /// <summary>
        /// Toggle a collection in the active filter list.
        /// </summary>
        void ToggleCollectionFilter(Guid collectionId);

        /// <summary>
        /// Clear state (e.g., on logout).
        /// </summary>
        void ClearState();
    }
}
