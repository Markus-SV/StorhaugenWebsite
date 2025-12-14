using StorhaugenWebsite.Shared.DTOs;

namespace StorhaugenEats.API.Services;

/// <summary>
/// Service for managing recipe collections.
/// </summary>
public interface ICollectionService
{
    // Collection CRUD
    Task<List<CollectionDto>> GetUserCollectionsAsync(Guid userId);
    Task<CollectionDto?> GetCollectionAsync(Guid collectionId, Guid userId);
    Task<CollectionDto> CreateCollectionAsync(Guid userId, CreateCollectionDto dto);
    Task<CollectionDto> UpdateCollectionAsync(Guid collectionId, Guid userId, UpdateCollectionDto dto);
    Task DeleteCollectionAsync(Guid collectionId, Guid userId);

    // Recipe-Collection management
    Task<CollectionRecipesResult> GetCollectionRecipesAsync(Guid collectionId, Guid userId, GetCollectionRecipesQuery? query = null);
    Task AddRecipeToCollectionAsync(Guid collectionId, Guid userId, AddRecipeToCollectionDto dto);
    Task RemoveRecipeFromCollectionAsync(Guid collectionId, Guid recipeId, Guid userId);

    // Collection membership
    Task<List<CollectionMemberDto>> GetCollectionMembersAsync(Guid collectionId, Guid userId);
    Task AddMemberAsync(Guid collectionId, Guid userId, AddCollectionMemberDto dto);
    Task RemoveMemberAsync(Guid collectionId, Guid memberId, Guid userId);
    Task LeaveCollectionAsync(Guid collectionId, Guid userId);

    // Visibility check for recipes
    Task<bool> CanUserViewRecipeViaCollectionAsync(Guid recipeId, Guid userId);
}
