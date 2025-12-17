using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;
using StorhaugenEats.API.Models;
using StorhaugenWebsite.Shared.DTOs;

namespace StorhaugenEats.API.Services;

/// <summary>
/// Service for managing recipe collections.
/// </summary>
public class CollectionService : ICollectionService
{
    private readonly AppDbContext _context;

    public CollectionService(AppDbContext context)
    {
        _context = context;
    }

    #region Collection CRUD

    public async Task<List<CollectionDto>> GetUserCollectionsAsync(Guid userId)
    {
        // Get collections user owns or is a member of
        var collections = await _context.Collections
            .Include(c => c.Owner)
            .Include(c => c.Members)
                .ThenInclude(m => m.User)
            .Include(c => c.UserRecipeCollections)
            .Where(c => c.OwnerId == userId || c.Members.Any(m => m.UserId == userId))
            .OrderBy(c => c.Name)
            .ToListAsync();

        return collections.Select(c => MapToDto(c, userId)).ToList();
    }

    public async Task<CollectionDto?> GetCollectionAsync(Guid collectionId, Guid userId)
    {
        var collection = await _context.Collections
            .Include(c => c.Owner)
            .Include(c => c.Members)
                .ThenInclude(m => m.User)
            .Include(c => c.UserRecipeCollections)
            .FirstOrDefaultAsync(c => c.Id == collectionId);

        if (collection == null)
            return null;

        // Check access
        if (!IsMember(collection, userId))
            return null;

        return MapToDto(collection, userId);
    }

    public async Task<CollectionDto?> GetCollectionByShareCodeAsync(string shareCode, Guid? userId)
    {
        if (string.IsNullOrWhiteSpace(shareCode))
            return null;

        var collection = await _context.Collections
            .Include(c => c.Owner)
            .Include(c => c.Members)
                .ThenInclude(m => m.User)
            .Include(c => c.UserRecipeCollections)
            .FirstOrDefaultAsync(c => c.ShareCode == shareCode.ToUpper());

        if (collection == null)
            return null;

        // Check visibility rules
        if (collection.Visibility == "private")
            return null; // Private collections cannot be accessed via share code

        if (collection.Visibility == "friends" && userId.HasValue)
        {
            // Check if the requesting user is a friend of the owner
            var areFriends = await _context.UserFriendships
                .AnyAsync(f =>
                    f.Status == "accepted" &&
                    ((f.RequesterUserId == collection.OwnerId && f.TargetUserId == userId.Value) ||
                     (f.TargetUserId == collection.OwnerId && f.RequesterUserId == userId.Value)));

            if (!areFriends && collection.OwnerId != userId.Value && !collection.Members.Any(m => m.UserId == userId.Value))
                return null;
        }

        return MapToDto(collection, userId ?? Guid.Empty);
    }

    public async Task<CollectionDto> CreateCollectionAsync(Guid userId, CreateCollectionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Collection name is required");

        var visibility = ValidateVisibility(dto.Visibility);

        var collection = new Collection
        {
            Id = Guid.NewGuid(),
            OwnerId = userId,
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            Visibility = visibility,
            ShareCode = visibility != "private" ? GenerateShareCode() : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Collections.Add(collection);

        // Add owner as member with IsOwner=true
        _context.CollectionMembers.Add(new CollectionMember
        {
            CollectionId = collection.Id,
            UserId = userId,
            IsOwner = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();

        return (await GetCollectionAsync(collection.Id, userId))!;
    }

    public async Task<CollectionDto> UpdateCollectionAsync(Guid collectionId, Guid userId, UpdateCollectionDto dto)
    {
        var collection = await _context.Collections
            .FirstOrDefaultAsync(c => c.Id == collectionId);

        if (collection == null)
            throw new InvalidOperationException("Collection not found");

        if (collection.OwnerId != userId)
            throw new InvalidOperationException("Only the owner can update this collection");

        if (!string.IsNullOrWhiteSpace(dto.Name))
            collection.Name = dto.Name.Trim();

        if (dto.Description != null)
            collection.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

        if (!string.IsNullOrWhiteSpace(dto.Visibility))
        {
            var newVisibility = ValidateVisibility(dto.Visibility);
            var wasPrivate = collection.Visibility == "private";
            var isNowPrivate = newVisibility == "private";

            collection.Visibility = newVisibility;

            // Generate share code when becoming non-private, clear when becoming private
            if (wasPrivate && !isNowPrivate && string.IsNullOrEmpty(collection.ShareCode))
            {
                collection.ShareCode = GenerateShareCode();
            }
            else if (isNowPrivate)
            {
                collection.ShareCode = null;
            }
        }

        collection.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return (await GetCollectionAsync(collectionId, userId))!;
    }

    public async Task DeleteCollectionAsync(Guid collectionId, Guid userId)
    {
        var collection = await _context.Collections
            .FirstOrDefaultAsync(c => c.Id == collectionId);

        if (collection == null)
            throw new InvalidOperationException("Collection not found");

        if (collection.OwnerId != userId)
            throw new InvalidOperationException("Only the owner can delete this collection");

        // Recipes are not deleted, just the collection and links (cascade)
        _context.Collections.Remove(collection);
        await _context.SaveChangesAsync();
    }

    #endregion

    #region Recipe-Collection Management

    public async Task<CollectionRecipesResult> GetCollectionRecipesAsync(Guid collectionId, Guid userId, GetCollectionRecipesQuery? query = null)
    {
        query ??= new GetCollectionRecipesQuery();

        var collection = await _context.Collections
            .Include(c => c.Owner)
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == collectionId);

        if (collection == null)
            throw new InvalidOperationException("Collection not found");

        if (!IsMember(collection, userId))
            throw new InvalidOperationException("You don't have access to this collection");

        var recipesQuery = _context.UserRecipeCollections
            .Where(urc => urc.CollectionId == collectionId)
            .Include(urc => urc.UserRecipe)
                .ThenInclude(ur => ur.User)
            .Include(urc => urc.UserRecipe)
                .ThenInclude(ur => ur.GlobalRecipe)
            .Include(urc => urc.UserRecipe)
                .ThenInclude(ur => ur.Ratings)
            .AsQueryable();

        // Search filter
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchLower = query.Search.ToLower();
            recipesQuery = recipesQuery.Where(urc =>
                (urc.UserRecipe.LocalTitle != null && urc.UserRecipe.LocalTitle.ToLower().Contains(searchLower)) ||
                (urc.UserRecipe.GlobalRecipe != null && urc.UserRecipe.GlobalRecipe.Title.ToLower().Contains(searchLower)));
        }

        // Count before pagination
        var totalCount = await recipesQuery.CountAsync();

        // Sorting
        recipesQuery = query.SortBy.ToLower() switch
        {
            "name" => query.SortDescending
                ? recipesQuery.OrderByDescending(urc => urc.UserRecipe.LocalTitle ?? urc.UserRecipe.GlobalRecipe!.Title)
                : recipesQuery.OrderBy(urc => urc.UserRecipe.LocalTitle ?? urc.UserRecipe.GlobalRecipe!.Title),
            "rating" => query.SortDescending
                ? recipesQuery.OrderByDescending(urc => urc.UserRecipe.Ratings.Any() ? urc.UserRecipe.Ratings.Average(r => r.Score) : 0)
                : recipesQuery.OrderBy(urc => urc.UserRecipe.Ratings.Any() ? urc.UserRecipe.Ratings.Average(r => r.Score) : 0),
            "date" => query.SortDescending
                ? recipesQuery.OrderByDescending(urc => urc.UserRecipe.CreatedAt)
                : recipesQuery.OrderBy(urc => urc.UserRecipe.CreatedAt),
            _ => query.SortDescending
                ? recipesQuery.OrderByDescending(urc => urc.CreatedAt)
                : recipesQuery.OrderBy(urc => urc.CreatedAt)
        };

        // Pagination
        var recipes = await recipesQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(urc => urc.UserRecipe)
            .ToListAsync();

        var recipeDtos = recipes.Select(r => MapUserRecipeToDto(r, userId)).ToList();

        return new CollectionRecipesResult
        {
            Collection = MapToDto(collection, userId),
            Recipes = recipeDtos,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task AddRecipeToCollectionAsync(Guid collectionId, Guid userId, AddRecipeToCollectionDto dto)
    {
        var collection = await _context.Collections
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == collectionId);

        if (collection == null)
            throw new InvalidOperationException("Collection not found");

        if (!IsMember(collection, userId))
            throw new InvalidOperationException("You must be a member to add recipes to this collection");

        // Verify recipe exists
        var recipe = await _context.UserRecipes
            .FirstOrDefaultAsync(r => r.Id == dto.UserRecipeId);

        if (recipe == null)
            throw new InvalidOperationException("Recipe not found");

        // Check if already in collection
        var exists = await _context.UserRecipeCollections
            .AnyAsync(urc => urc.CollectionId == collectionId && urc.UserRecipeId == dto.UserRecipeId);

        if (exists)
            return; // Already in collection

        _context.UserRecipeCollections.Add(new UserRecipeCollection
        {
            UserRecipeId = dto.UserRecipeId,
            CollectionId = collectionId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    public async Task RemoveRecipeFromCollectionAsync(Guid collectionId, Guid recipeId, Guid userId)
    {
        var collection = await _context.Collections
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == collectionId);

        if (collection == null)
            throw new InvalidOperationException("Collection not found");

        if (!IsMember(collection, userId))
            throw new InvalidOperationException("You must be a member to remove recipes from this collection");

        var link = await _context.UserRecipeCollections
            .FirstOrDefaultAsync(urc => urc.CollectionId == collectionId && urc.UserRecipeId == recipeId);

        if (link != null)
        {
            _context.UserRecipeCollections.Remove(link);
            await _context.SaveChangesAsync();
        }
    }

    #endregion

    #region Collection Membership

    public async Task<List<CollectionMemberDto>> GetCollectionMembersAsync(Guid collectionId, Guid userId)
    {
        var collection = await _context.Collections
            .Include(c => c.Members)
                .ThenInclude(m => m.User)
            .FirstOrDefaultAsync(c => c.Id == collectionId);

        if (collection == null)
            throw new InvalidOperationException("Collection not found");

        if (!IsMember(collection, userId))
            throw new InvalidOperationException("You don't have access to this collection");

        return collection.Members.Select(m => new CollectionMemberDto
        {
            UserId = m.UserId,
            DisplayName = m.User.DisplayName,
            AvatarUrl = m.User.AvatarUrl,
            IsOwner = m.IsOwner,
            CreatedAt = m.CreatedAt
        }).ToList();
    }

    public async Task AddMemberAsync(Guid collectionId, Guid userId, AddCollectionMemberDto dto)
    {
        var collection = await _context.Collections
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == collectionId);

        if (collection == null)
            throw new InvalidOperationException("Collection not found");

        if (collection.OwnerId != userId)
            throw new InvalidOperationException("Only the owner can add members");

        // Find user by share ID or email
        var targetUser = await _context.Users
            .FirstOrDefaultAsync(u => u.UniqueShareId == dto.UserIdentifier || u.Email == dto.UserIdentifier);

        if (targetUser == null)
            throw new InvalidOperationException("User not found");

        // Check if already a member
        if (collection.Members.Any(m => m.UserId == targetUser.Id))
            throw new InvalidOperationException("User is already a member of this collection");

        _context.CollectionMembers.Add(new CollectionMember
        {
            CollectionId = collectionId,
            UserId = targetUser.Id,
            IsOwner = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
    }

    public async Task RemoveMemberAsync(Guid collectionId, Guid memberId, Guid userId)
    {
        var collection = await _context.Collections
            .Include(c => c.Members)
            .FirstOrDefaultAsync(c => c.Id == collectionId);

        if (collection == null)
            throw new InvalidOperationException("Collection not found");

        if (collection.OwnerId != userId)
            throw new InvalidOperationException("Only the owner can remove members");

        var member = collection.Members.FirstOrDefault(m => m.UserId == memberId);
        if (member == null)
            throw new InvalidOperationException("Member not found");

        if (member.IsOwner)
            throw new InvalidOperationException("Cannot remove the owner");

        _context.CollectionMembers.Remove(member);
        await _context.SaveChangesAsync();
    }

    public async Task LeaveCollectionAsync(Guid collectionId, Guid userId)
    {
        var member = await _context.CollectionMembers
            .Include(m => m.Collection)
            .FirstOrDefaultAsync(m => m.CollectionId == collectionId && m.UserId == userId);

        if (member == null)
            throw new InvalidOperationException("You are not a member of this collection");

        if (member.IsOwner)
            throw new InvalidOperationException("Cannot leave your own collection. Delete it instead.");

        _context.CollectionMembers.Remove(member);
        await _context.SaveChangesAsync();
    }

    #endregion

    #region Visibility

    public async Task<bool> CanUserViewRecipeViaCollectionAsync(Guid recipeId, Guid userId)
    {
        // Check if there's any collection containing this recipe where the user is a member
        return await _context.UserRecipeCollections
            .AnyAsync(urc =>
                urc.UserRecipeId == recipeId &&
                urc.Collection.Members.Any(m => m.UserId == userId));
    }

    #endregion

    #region Private Helpers

    private bool IsMember(Collection collection, Guid userId)
    {
        return collection.OwnerId == userId ||
               collection.Members.Any(m => m.UserId == userId);
    }

    private CollectionDto MapToDto(Collection collection, Guid userId)
    {
        return new CollectionDto
        {
            Id = collection.Id,
            Name = collection.Name,
            Description = collection.Description,
            Visibility = collection.Visibility,
            ShareCode = collection.ShareCode,
            OwnerId = collection.OwnerId,
            OwnerDisplayName = collection.Owner?.DisplayName ?? "Unknown",
            OwnerAvatarUrl = collection.Owner?.AvatarUrl,
            RecipeCount = collection.UserRecipeCollections?.Count ?? 0,
            MemberCount = collection.Members?.Count ?? 0,
            IsOwner = collection.OwnerId == userId,
            IsMember = IsMember(collection, userId),
            CreatedAt = collection.CreatedAt,
            UpdatedAt = collection.UpdatedAt,
            Members = collection.Members?.Select(m => new CollectionMemberDto
            {
                UserId = m.UserId,
                DisplayName = m.User?.DisplayName ?? "Unknown",
                AvatarUrl = m.User?.AvatarUrl,
                IsOwner = m.IsOwner,
                CreatedAt = m.CreatedAt
            }).ToList() ?? new List<CollectionMemberDto>()
        };
    }

    private static string ValidateVisibility(string visibility)
    {
        var valid = new[] { "private", "friends", "public" };
        var normalized = visibility?.ToLower() ?? "private";
        return valid.Contains(normalized) ? normalized : "private";
    }

    private static string GenerateShareCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // Avoid ambiguous chars like O/0, I/1
        var random = new Random();
        return new string(Enumerable.Range(0, 8).Select(_ => chars[random.Next(chars.Length)]).ToArray());
    }

    private UserRecipeDto MapUserRecipeToDto(UserRecipe recipe, Guid userId)
    {
        var myRating = recipe.Ratings?.FirstOrDefault(r => r.UserId == userId);
        var avgRating = recipe.Ratings?.Any() == true ? recipe.Ratings.Average(r => r.Score) : 0;

        return new UserRecipeDto
        {
            Id = recipe.Id,
            UserId = recipe.UserId,
            UserDisplayName = recipe.User?.DisplayName ?? "Unknown",
            UserAvatarUrl = recipe.User?.AvatarUrl,
            Name = recipe.LocalTitle ?? recipe.GlobalRecipe?.Title ?? "Untitled",
            Description = recipe.LocalDescription ?? recipe.GlobalRecipe?.Description,
            ImageUrls = GetImageUrls(recipe),
            GlobalRecipeId = recipe.GlobalRecipeId,
            GlobalRecipeName = recipe.GlobalRecipe?.Title,
            Visibility = recipe.Visibility,
            PersonalNotes = recipe.UserId == userId ? recipe.PersonalNotes : null,
            IsArchived = recipe.IsArchived,
            CreatedAt = recipe.CreatedAt,
            UpdatedAt = recipe.UpdatedAt,
            MyRating = myRating?.Score,
            AverageRating = avgRating,
            RatingCount = recipe.Ratings?.Count ?? 0,
            IsHellofresh = recipe.GlobalRecipe?.IsHellofresh ?? false,
            HellofreshWeek = recipe.GlobalRecipe?.HellofreshWeek
        };
    }

    private List<string> GetImageUrls(UserRecipe recipe)
    {
        if (!string.IsNullOrEmpty(recipe.LocalImageUrl))
            return new List<string> { recipe.LocalImageUrl };

        if (!string.IsNullOrEmpty(recipe.LocalImageUrls) && recipe.LocalImageUrls != "[]")
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<string>>(recipe.LocalImageUrls) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        if (!string.IsNullOrEmpty(recipe.GlobalRecipe?.ImageUrl))
            return new List<string> { recipe.GlobalRecipe.ImageUrl };

        return new List<string>();
    }

    #endregion
}
