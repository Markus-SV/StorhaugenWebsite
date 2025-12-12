using StorhaugenWebsite.Shared.DTOs;

namespace StorhaugenEats.API.Services;

/// <summary>
/// Service for managing personal recipe tags.
/// </summary>
public interface ITagService
{
    // Tag CRUD
    Task<List<TagDto>> GetUserTagsAsync(Guid userId);
    Task<TagDto?> GetTagAsync(Guid tagId, Guid userId);
    Task<TagDto> CreateTagAsync(Guid userId, CreateTagDto dto);
    Task<TagDto> UpdateTagAsync(Guid tagId, Guid userId, UpdateTagDto dto);
    Task DeleteTagAsync(Guid tagId, Guid userId);

    // Recipe-Tag management
    Task<List<TagReferenceDto>> GetRecipeTagsAsync(Guid recipeId, Guid userId);
    Task SetRecipeTagsAsync(Guid recipeId, Guid userId, List<Guid> tagIds);
    Task AddTagToRecipeAsync(Guid recipeId, Guid tagId, Guid userId);
    Task RemoveTagFromRecipeAsync(Guid recipeId, Guid tagId, Guid userId);
}
