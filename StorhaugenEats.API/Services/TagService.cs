using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;
using StorhaugenEats.API.Models;
using StorhaugenWebsite.Shared.DTOs;

namespace StorhaugenEats.API.Services;

/// <summary>
/// Service for managing personal recipe tags.
/// </summary>
public class TagService : ITagService
{
    private readonly AppDbContext _context;

    public TagService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<List<TagDto>> GetUserTagsAsync(Guid userId)
    {
        var tags = await _context.RecipeTags
            .Where(t => t.UserId == userId)
            .Select(t => new TagDto
            {
                Id = t.Id,
                Name = t.Name,
                Color = t.Color,
                Icon = t.Icon,
                RecipeCount = t.UserRecipeTags.Count,
                CreatedAt = t.CreatedAt
            })
            .OrderBy(t => t.Name)
            .ToListAsync();

        return tags;
    }

    public async Task<TagDto?> GetTagAsync(Guid tagId, Guid userId)
    {
        var tag = await _context.RecipeTags
            .Where(t => t.Id == tagId && t.UserId == userId)
            .Select(t => new TagDto
            {
                Id = t.Id,
                Name = t.Name,
                Color = t.Color,
                Icon = t.Icon,
                RecipeCount = t.UserRecipeTags.Count,
                CreatedAt = t.CreatedAt
            })
            .FirstOrDefaultAsync();

        return tag;
    }

    public async Task<TagDto> CreateTagAsync(Guid userId, CreateTagDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Tag name is required");

        // Check for duplicate name
        var exists = await _context.RecipeTags
            .AnyAsync(t => t.UserId == userId && t.Name.ToLower() == dto.Name.ToLower());

        if (exists)
            throw new InvalidOperationException($"A tag with name '{dto.Name}' already exists");

        var tag = new RecipeTag
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = dto.Name.Trim(),
            Color = dto.Color,
            Icon = dto.Icon,
            CreatedAt = DateTime.UtcNow
        };

        _context.RecipeTags.Add(tag);
        await _context.SaveChangesAsync();

        return new TagDto
        {
            Id = tag.Id,
            Name = tag.Name,
            Color = tag.Color,
            Icon = tag.Icon,
            RecipeCount = 0,
            CreatedAt = tag.CreatedAt
        };
    }

    public async Task<TagDto> UpdateTagAsync(Guid tagId, Guid userId, UpdateTagDto dto)
    {
        var tag = await _context.RecipeTags
            .Include(t => t.UserRecipeTags)
            .FirstOrDefaultAsync(t => t.Id == tagId && t.UserId == userId);

        if (tag == null)
            throw new InvalidOperationException("Tag not found");

        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            // Check for duplicate name (excluding current tag)
            var exists = await _context.RecipeTags
                .AnyAsync(t => t.UserId == userId && t.Id != tagId && t.Name.ToLower() == dto.Name.ToLower());

            if (exists)
                throw new InvalidOperationException($"A tag with name '{dto.Name}' already exists");

            tag.Name = dto.Name.Trim();
        }

        if (dto.Color != null)
            tag.Color = dto.Color;

        if (dto.Icon != null)
            tag.Icon = dto.Icon;

        await _context.SaveChangesAsync();

        return new TagDto
        {
            Id = tag.Id,
            Name = tag.Name,
            Color = tag.Color,
            Icon = tag.Icon,
            RecipeCount = tag.UserRecipeTags.Count,
            CreatedAt = tag.CreatedAt
        };
    }

    public async Task DeleteTagAsync(Guid tagId, Guid userId)
    {
        var tag = await _context.RecipeTags
            .FirstOrDefaultAsync(t => t.Id == tagId && t.UserId == userId);

        if (tag == null)
            throw new InvalidOperationException("Tag not found");

        _context.RecipeTags.Remove(tag);
        await _context.SaveChangesAsync();
    }

    public async Task<List<TagReferenceDto>> GetRecipeTagsAsync(Guid recipeId, Guid userId)
    {
        // Verify recipe belongs to user
        var recipe = await _context.UserRecipes
            .FirstOrDefaultAsync(r => r.Id == recipeId && r.UserId == userId);

        if (recipe == null)
            throw new InvalidOperationException("Recipe not found");

        var tags = await _context.UserRecipeTags
            .Where(rt => rt.UserRecipeId == recipeId)
            .Select(rt => new TagReferenceDto
            {
                Id = rt.Tag.Id,
                Name = rt.Tag.Name,
                Color = rt.Tag.Color
            })
            .ToListAsync();

        return tags;
    }

    public async Task SetRecipeTagsAsync(Guid recipeId, Guid userId, List<Guid> tagIds)
    {
        // Verify recipe belongs to user
        var recipe = await _context.UserRecipes
            .Include(r => r.UserRecipeTags)
            .FirstOrDefaultAsync(r => r.Id == recipeId && r.UserId == userId);

        if (recipe == null)
            throw new InvalidOperationException("Recipe not found");

        // Verify all tags belong to user
        var validTagIds = await _context.RecipeTags
            .Where(t => t.UserId == userId && tagIds.Contains(t.Id))
            .Select(t => t.Id)
            .ToListAsync();

        // Remove existing tags
        _context.UserRecipeTags.RemoveRange(recipe.UserRecipeTags);

        // Add new tags
        foreach (var tagId in validTagIds)
        {
            recipe.UserRecipeTags.Add(new UserRecipeTag
            {
                Id = Guid.NewGuid(),
                UserRecipeId = recipeId,
                TagId = tagId,
                CreatedAt = DateTime.UtcNow
            });
        }

        await _context.SaveChangesAsync();
    }

    public async Task AddTagToRecipeAsync(Guid recipeId, Guid tagId, Guid userId)
    {
        // Verify recipe belongs to user
        var recipe = await _context.UserRecipes
            .FirstOrDefaultAsync(r => r.Id == recipeId && r.UserId == userId);

        if (recipe == null)
            throw new InvalidOperationException("Recipe not found");

        // Verify tag belongs to user
        var tag = await _context.RecipeTags
            .FirstOrDefaultAsync(t => t.Id == tagId && t.UserId == userId);

        if (tag == null)
            throw new InvalidOperationException("Tag not found");

        // Check if already exists
        var exists = await _context.UserRecipeTags
            .AnyAsync(rt => rt.UserRecipeId == recipeId && rt.TagId == tagId);

        if (exists)
            return; // Already tagged

        _context.UserRecipeTags.Add(new UserRecipeTag
        {
            Id = Guid.NewGuid(),
            UserRecipeId = recipeId,
            TagId = tagId,
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    public async Task RemoveTagFromRecipeAsync(Guid recipeId, Guid tagId, Guid userId)
    {
        // Verify recipe belongs to user
        var recipe = await _context.UserRecipes
            .FirstOrDefaultAsync(r => r.Id == recipeId && r.UserId == userId);

        if (recipe == null)
            throw new InvalidOperationException("Recipe not found");

        var recipeTag = await _context.UserRecipeTags
            .FirstOrDefaultAsync(rt => rt.UserRecipeId == recipeId && rt.TagId == tagId);

        if (recipeTag != null)
        {
            _context.UserRecipeTags.Remove(recipeTag);
            await _context.SaveChangesAsync();
        }
    }
}
