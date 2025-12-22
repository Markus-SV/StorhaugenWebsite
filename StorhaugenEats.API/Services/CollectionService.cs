using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;
using StorhaugenEats.API.Helpers;
using StorhaugenEats.API.Models;
using StorhaugenWebsite.Shared.DTOs;
using System.Text.Json;

namespace StorhaugenEats.API.Services;

/// <summary>
/// Service for managing recipe collections.
/// </summary>
public class CollectionService : ICollectionService
{
    private readonly AppDbContext _context;
    private readonly IUserFriendshipService _friendshipService;

    public CollectionService(AppDbContext context, IUserFriendshipService friendshipService)
    {
        _context = context;
        _friendshipService = friendshipService;
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
            .Where(c => c.OwnerUserId == userId || c.Members.Any(m => m.UserId == userId))
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

        // Check access - members, friends (for friend visibility), or public
        if (!await CanUserViewCollectionAsync(collection, userId))
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
            .FirstOrDefaultAsync(c => c.UniqueShareId == shareCode.ToUpper());

        if (collection == null)
            return null;

        // Only shared collections can be accessed via share code
        if (!collection.IsShared)
            return null;

        return MapToDto(collection, userId ?? Guid.Empty);
    }

    public async Task<CollectionDto> CreateCollectionAsync(Guid userId, CreateCollectionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
            throw new InvalidOperationException("Collection name is required");

        var visibility = dto.Visibility ?? "private";
        var isShared = visibility != "private";

        var collection = new Collection
        {
            Id = Guid.NewGuid(),
            OwnerUserId = userId,
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim(),
            IsShared = isShared,
            Visibility = visibility,
            UniqueShareId = isShared ? GenerateShareCode() : null,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Collections.Add(collection);

        // Add owner as member with role=owner
        _context.CollectionMembers.Add(new CollectionMember
        {
            Id = Guid.NewGuid(),
            CollectionId = collection.Id,
            UserId = userId,
            Role = "owner",
            JoinedAt = DateTime.UtcNow
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

        if (collection.OwnerUserId != userId)
            throw new InvalidOperationException("Only the owner can update this collection");

        if (!string.IsNullOrWhiteSpace(dto.Name))
            collection.Name = dto.Name.Trim();

        if (dto.Description != null)
            collection.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();

        if (!string.IsNullOrWhiteSpace(dto.Visibility))
        {
            var newVisibility = dto.Visibility;
            var newIsShared = newVisibility != "private";
            var wasPrivate = !collection.IsShared;
            var isNowPrivate = !newIsShared;

            collection.IsShared = newIsShared;
            collection.Visibility = newVisibility;

            // Generate share code when becoming shared, clear when becoming private
            if (wasPrivate && !isNowPrivate && string.IsNullOrEmpty(collection.UniqueShareId))
            {
                collection.UniqueShareId = GenerateShareCode();
            }
            else if (isNowPrivate)
            {
                collection.UniqueShareId = null;
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

        if (collection.OwnerUserId != userId)
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

        if (!await CanUserViewCollectionAsync(collection, userId))
            throw new InvalidOperationException("You don't have access to this collection");

        var recipesQuery = _context.UserRecipeCollections
        .Where(urc => urc.CollectionId == collectionId)
        .Include(urc => urc.UserRecipe)
            .ThenInclude(ur => ur.User)
        .Include(urc => urc.UserRecipe)
            .ThenInclude(ur => ur.GlobalRecipe)
        .Include(urc => urc.UserRecipe)
            .ThenInclude(ur => ur.Ratings)
                .ThenInclude(r => r.User)
        .Include(urc => urc.UserRecipe)
            .ThenInclude(ur => ur.UserRecipeTags)
                .ThenInclude(rt => rt.Tag)
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
                ? recipesQuery.OrderByDescending(urc => urc.AddedAt)
                : recipesQuery.OrderBy(urc => urc.AddedAt)
        };

        // Pagination
        var recipes = await recipesQuery
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(urc => urc.UserRecipe)
            .ToListAsync();

        var recipeDtos = recipes.Select(r => MapRecipeToDto(r, userId)).ToList();

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
            Id = Guid.NewGuid(),
            UserRecipeId = dto.UserRecipeId,
            CollectionId = collectionId,
            AddedAt = DateTime.UtcNow,
            AddedByUserId = userId
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
            IsOwner = m.Role == "owner",
            CreatedAt = m.JoinedAt
        }).ToList();
    }

    public async Task AddMemberAsync(Guid collectionId, Guid userId, AddCollectionMemberDto dto)
    {
        var collection = await _context.Collections
            .FirstOrDefaultAsync(c => c.Id == collectionId);

        if (collection == null)
            throw new InvalidOperationException("Collection not found");

        // ? Allow any existing member (owner is also stored in collection_members)
        var inviterIsMember = await _context.CollectionMembers
            .AnyAsync(cm => cm.CollectionId == collectionId && cm.UserId == userId);

        if (!inviterIsMember)
            throw new InvalidOperationException("Only collection members can add members");

        if (string.IsNullOrWhiteSpace(dto.UserIdentifier))
            throw new InvalidOperationException("User identifier is required");

        var identifier = dto.UserIdentifier.Trim();

        // 1) Try GUID -> user.Id
        User? userToAdd = null;
        if (Guid.TryParse(identifier, out var parsedUserId))
        {
            userToAdd = await _context.Users.FirstOrDefaultAsync(u => u.Id == parsedUserId);
        }

        // 2) Otherwise: match ShareId or Email (case-insensitive)
        var identLower = identifier.ToLower();
        userToAdd ??= await _context.Users.FirstOrDefaultAsync(u =>
            u.UniqueShareId.ToLower() == identLower ||
            u.Email.ToLower() == identLower
        );

        if (userToAdd == null)
            throw new InvalidOperationException("User not found with that identifier");

        // (Optional) prevent adding yourself
        if (userToAdd.Id == userId)
            throw new InvalidOperationException("You are already a member of this collection");

        var alreadyMember = await _context.CollectionMembers
            .AnyAsync(cm => cm.CollectionId == collectionId && cm.UserId == userToAdd.Id);

        if (alreadyMember)
            throw new InvalidOperationException("User is already a member of this collection");

        _context.CollectionMembers.Add(new CollectionMember
        {
            Id = Guid.NewGuid(),               // important since your model has Guid Id without DB generation
            CollectionId = collectionId,
            UserId = userToAdd.Id,
            Role = "member",                   //  matches your entity model
            JoinedAt = DateTime.UtcNow,
            InvitedByUserId = userId           //  track who invited
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

        if (collection.OwnerUserId != userId)
            throw new InvalidOperationException("Only the owner can remove members");

        var member = collection.Members.FirstOrDefault(m => m.UserId == memberId);
        if (member == null)
            throw new InvalidOperationException("Member not found");

        if (member.Role == "owner")
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

        if (member.Role == "owner")
            throw new InvalidOperationException("Cannot leave your own collection. Delete it instead.");

        _context.CollectionMembers.Remove(member);
        await _context.SaveChangesAsync();
    }

    #endregion

    #region Visibility

    public async Task<bool> CanUserViewRecipeViaCollectionAsync(Guid recipeId, Guid userId)
    {
        // Find collections that contain the recipe + minimal access info
        var collectionInfos = await _context.UserRecipeCollections
            .Where(urc => urc.UserRecipeId == recipeId)
            .Select(urc => new
            {
                OwnerUserId = urc.Collection.OwnerUserId,
                Visibility = urc.Collection.Visibility ?? (urc.Collection.IsShared ? "friends" : "private"),
                IsMember = urc.Collection.Members.Any(m => m.UserId == userId)
            })
            .ToListAsync();

        if (collectionInfos.Count == 0)
            return false;

        // Owner or member => allowed
        if (collectionInfos.Any(c => c.OwnerUserId == userId || c.IsMember))
            return true;

        // Public collection => allowed
        if (collectionInfos.Any(c => string.Equals(c.Visibility, "public", StringComparison.OrdinalIgnoreCase)))
            return true;

        // Friends collection => allowed if user is friends with the owner of ANY such collection
        var friendOwners = collectionInfos
            .Where(c => string.Equals(c.Visibility, "friends", StringComparison.OrdinalIgnoreCase))
            .Select(c => c.OwnerUserId)
            .Distinct()
            .ToList();

        if (friendOwners.Count == 0)
            return false;

        return await _context.UserFriendships.AnyAsync(f =>
            f.Status == "accepted" &&
            (
                (f.RequesterUserId == userId && friendOwners.Contains(f.TargetUserId)) ||
                (f.TargetUserId == userId && friendOwners.Contains(f.RequesterUserId))
            ));
    }



    public async Task<List<CollectionDto>> GetFriendSharedCollectionsAsync(Guid friendUserId, Guid currentUserId)
    {
        // Check if they are actually friends
        var areFriends = await _context.UserFriendships
            .AnyAsync(f =>
                f.Status == "accepted" &&
                ((f.RequesterUserId == currentUserId && f.TargetUserId == friendUserId) ||
                 (f.RequesterUserId == friendUserId && f.TargetUserId == currentUserId)));

        if (!areFriends)
            return new List<CollectionDto>();

        // Get collections owned by the friend that are visible to friends or public
        var collections = await _context.Collections
            .Include(c => c.Owner)
            .Include(c => c.Members)
                .ThenInclude(m => m.User)
            .Include(c => c.UserRecipeCollections)
            .Where(c => c.OwnerUserId == friendUserId &&
                       (c.Visibility == "friends" || c.Visibility == "public"))
            .OrderBy(c => c.Name)
            .ToListAsync();

        return collections.Select(c => MapToDto(c, currentUserId)).ToList();
    }

    #endregion

    #region Private Helpers

    private bool IsMember(Collection collection, Guid userId)
    {
        return collection.OwnerUserId == userId ||
               collection.Members.Any(m => m.UserId == userId);
    }

    private async Task<bool> CanUserViewCollectionAsync(Collection collection, Guid userId)
    {
        // Members can always view
        if (IsMember(collection, userId))
            return true;

        // Public collections can be viewed by anyone
        if (collection.Visibility == "public")
            return true;

        // Friends visibility - check if user is a friend of the owner
        if (collection.Visibility == "friends")
        {
            return await _friendshipService.AreFriendsAsync(collection.OwnerUserId, userId);
        }

        return false;
    }

    private CollectionDto MapToDto(Collection collection, Guid userId)
    {
        return new CollectionDto
        {
            Id = collection.Id,
            Name = collection.Name,
            Description = collection.Description,
            Visibility = collection.Visibility ?? (collection.IsShared ? "friends" : "private"),
            ShareCode = collection.UniqueShareId,
            OwnerId = collection.OwnerUserId,
            OwnerDisplayName = collection.Owner?.DisplayName ?? "Unknown",
            OwnerAvatarUrl = collection.Owner?.AvatarUrl,
            RecipeCount = collection.UserRecipeCollections?.Count ?? 0,
            MemberCount = collection.Members?.Count ?? 0,
            IsOwner = collection.OwnerUserId == userId,
            IsMember = IsMember(collection, userId),
            CreatedAt = collection.CreatedAt,
            UpdatedAt = collection.UpdatedAt,
            Members = collection.Members?.Select(m => new CollectionMemberDto
            {
                UserId = m.UserId,
                DisplayName = m.User?.DisplayName ?? "Unknown",
                AvatarUrl = m.User?.AvatarUrl,
                IsOwner = m.Role == "owner",
                CreatedAt = m.JoinedAt
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

    private UserRecipeDto MapRecipeToDto(UserRecipe recipe, Guid requestingUserId)
    {
        var myRating = recipe.Ratings?.FirstOrDefault(r => r.UserId == requestingUserId);

        var avgRating = recipe.Ratings?.Any() == true
            ? recipe.Ratings.Average(r => r.Score)
            : 0;

        var memberRatings = recipe.Ratings?
            .Where(r => r.User != null && r.UserId != requestingUserId)
            .ToDictionary(r => r.User!.DisplayName, r => (decimal?)r.Score)
            ?? new Dictionary<string, decimal?>();

        // Ingredients (local wins over global)
        object? ingredients = null;
        if (!string.IsNullOrWhiteSpace(recipe.LocalIngredients))
            ingredients = JsonHelper.JsonToObject(recipe.LocalIngredients);
        else if (!string.IsNullOrWhiteSpace(recipe.GlobalRecipe?.Ingredients))
            ingredients = JsonHelper.JsonToObject(recipe.GlobalRecipe.Ingredients);

        // Global tags + nutrition
        var recipeTags = JsonHelper.JsonToList(recipe.GlobalRecipe?.Tags);
        var nutritionData = JsonHelper.JsonToObject(
            !string.IsNullOrWhiteSpace(recipe.LocalNutritionDataJson)
                ? recipe.LocalNutritionDataJson
                : recipe.GlobalRecipe?.NutritionData);

        // Personal organization tags (from user_recipe_tags)
        var tags = recipe.UserRecipeTags?
            .Where(rt => rt.Tag != null)
            .Select(rt => new TagReferenceDto
            {
                Id = rt.TagId,
                Name = rt.Tag!.Name,
                Color = rt.Tag!.Color
            })
            .OrderBy(t => t.Name)
            .ToList()
            ?? new List<TagReferenceDto>();

        return new UserRecipeDto
        {
            Id = recipe.Id,
            UserId = recipe.UserId,
            UserDisplayName = recipe.User?.DisplayName ?? "Unknown",
            UserAvatarUrl = recipe.User?.AvatarUrl,

            Name = recipe.LocalTitle ?? recipe.GlobalRecipe?.Title ?? "Untitled",
            Description = recipe.LocalDescription ?? recipe.GlobalRecipe?.Description,
            ImageUrls = GetImageUrls(recipe),
            Ingredients = ingredients,

            // Metadata (local wins over global)
            PrepTimeMinutes = recipe.LocalPrepTimeMinutes ?? recipe.GlobalRecipe?.PrepTimeMinutes,
            CookTimeMinutes = recipe.LocalCookTimeMinutes ?? recipe.GlobalRecipe?.CookTimeMinutes,
            Servings = recipe.LocalServings ?? recipe.GlobalRecipe?.Servings,
            Difficulty = recipe.LocalDifficulty ?? recipe.GlobalRecipe?.Difficulty,
            Cuisine = recipe.LocalCuisine ?? recipe.GlobalRecipe?.Cuisine,
            RecipeTags = recipeTags,
            NutritionData = nutritionData,

            IsHellofresh = recipe.GlobalRecipe?.IsHellofresh ?? false,
            HellofreshWeek = recipe.GlobalRecipe?.HellofreshWeek,

            GlobalRecipeId = recipe.GlobalRecipeId,
            GlobalRecipeName = recipe.GlobalRecipe?.Title,
            IsPublished = recipe.GlobalRecipe?.PublishedFromUserRecipeId == recipe.Id,

            Visibility = string.IsNullOrWhiteSpace(recipe.Visibility) ? "private" : recipe.Visibility,
            PersonalNotes = recipe.UserId == requestingUserId ? recipe.PersonalNotes : null,

            IsArchived = recipe.IsArchived,
            CreatedAt = recipe.CreatedAt,
            UpdatedAt = recipe.UpdatedAt,

            MyRating = myRating?.Score,
            AverageRating = (double)avgRating,
            RatingCount = recipe.Ratings?.Count ?? 0,
            MemberRatings = memberRatings,

            Tags = tags
        };
    }


    private static List<string> GetImageUrls(UserRecipe recipe)
    {
        // 1) Local single
        if (!string.IsNullOrWhiteSpace(recipe.LocalImageUrl) && recipe.LocalImageUrl != "null")
            return new List<string> { recipe.LocalImageUrl };

        // 2) Local list
        if (!string.IsNullOrWhiteSpace(recipe.LocalImageUrls) &&
            recipe.LocalImageUrls != "[]" &&
            recipe.LocalImageUrls != "null")
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(recipe.LocalImageUrls) ?? new();
            }
            catch { /* ignore */ }
        }

        // 3) Global list (this is the important fix for HF cases)
        if (!string.IsNullOrWhiteSpace(recipe.GlobalRecipe?.ImageUrls) &&
            recipe.GlobalRecipe.ImageUrls != "[]" &&
            recipe.GlobalRecipe.ImageUrls != "null")
        {
            try
            {
                return JsonSerializer.Deserialize<List<string>>(recipe.GlobalRecipe.ImageUrls) ?? new();
            }
            catch { /* ignore */ }
        }

        // 4) Global single fallback
        if (!string.IsNullOrWhiteSpace(recipe.GlobalRecipe?.ImageUrl) && recipe.GlobalRecipe.ImageUrl != "null")
            return new List<string> { recipe.GlobalRecipe.ImageUrl };

        return new();
    }


    #endregion
}
