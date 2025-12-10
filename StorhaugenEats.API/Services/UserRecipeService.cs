using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;
using StorhaugenEats.API.DTOs;
using StorhaugenEats.API.Models;
using System.Text.Json;

namespace StorhaugenEats.API.Services;

public class UserRecipeService : IUserRecipeService
{
    private readonly AppDbContext _context;
    private readonly IActivityFeedService _activityFeedService;
    private readonly IUserFriendshipService _friendshipService;

    public UserRecipeService(
        AppDbContext context,
        IActivityFeedService activityFeedService,
        IUserFriendshipService friendshipService)
    {
        _context = context;
        _activityFeedService = activityFeedService;
        _friendshipService = friendshipService;
    }

    public async Task<UserRecipePagedResult> GetUserRecipesAsync(Guid userId, GetUserRecipesQuery query)
    {
        var queryable = _context.UserRecipes
            .Include(r => r.User)
            .Include(r => r.GlobalRecipe)
            .Include(r => r.Ratings)
            .Where(r => r.UserId == userId);

        if (!query.IncludeArchived)
        {
            queryable = queryable.Where(r => !r.IsArchived);
        }

        if (!string.IsNullOrEmpty(query.Visibility) && query.Visibility != "all")
        {
            queryable = queryable.Where(r => r.Visibility == query.Visibility);
        }

        // Sorting
        queryable = query.SortBy.ToLower() switch
        {
            "name" => query.SortDescending
                ? queryable.OrderByDescending(r => r.LocalTitle ?? r.GlobalRecipe!.Title)
                : queryable.OrderBy(r => r.LocalTitle ?? r.GlobalRecipe!.Title),
            "rating" => query.SortDescending
                ? queryable.OrderByDescending(r => r.Ratings.Any() ? r.Ratings.Average(rt => rt.Score) : 0)
                : queryable.OrderBy(r => r.Ratings.Any() ? r.Ratings.Average(rt => rt.Score) : 0),
            _ => query.SortDescending
                ? queryable.OrderByDescending(r => r.CreatedAt)
                : queryable.OrderBy(r => r.CreatedAt)
        };

        var totalCount = await queryable.CountAsync();
        var recipes = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new UserRecipePagedResult
        {
            Recipes = recipes.Select(r => MapToDto(r, userId)).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<UserRecipeDto?> GetRecipeAsync(Guid recipeId, Guid requestingUserId)
    {
        var recipe = await _context.UserRecipes
            .Include(r => r.User)
            .Include(r => r.GlobalRecipe)
            .Include(r => r.Ratings)
                .ThenInclude(rt => rt.User)
            .FirstOrDefaultAsync(r => r.Id == recipeId);

        if (recipe == null) return null;

        // Check visibility
        if (!await CanUserViewRecipeAsync(recipeId, requestingUserId))
            return null;

        return MapToDto(recipe, requestingUserId);
    }

    public async Task<UserRecipeDto> CreateRecipeAsync(Guid userId, CreateUserRecipeDto dto)
    {
        var user = await _context.Users.FindAsync(userId)
            ?? throw new InvalidOperationException("User not found");

        var recipe = new UserRecipe
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            GlobalRecipeId = dto.GlobalRecipeId,
            LocalTitle = dto.Name,
            LocalDescription = dto.Description,
            LocalIngredients = dto.Ingredients != null ? JsonSerializer.Serialize(dto.Ingredients) : null,
            LocalImageUrls = dto.ImageUrls != null ? JsonSerializer.Serialize(dto.ImageUrls) : "[]",
            PersonalNotes = dto.PersonalNotes,
            Visibility = dto.Visibility ?? "private",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        // If linking to a global recipe, increment its TotalTimesAdded
        if (dto.GlobalRecipeId.HasValue)
        {
            var globalRecipe = await _context.GlobalRecipes.FindAsync(dto.GlobalRecipeId.Value);
            if (globalRecipe != null)
            {
                globalRecipe.TotalTimesAdded++;
            }
        }

        _context.UserRecipes.Add(recipe);
        await _context.SaveChangesAsync();

        // Record activity
        var imageUrls = dto.ImageUrls?.FirstOrDefault();
        await _activityFeedService.RecordAddedRecipeActivityAsync(
            userId,
            recipe.Id,
            recipe.DisplayTitle,
            imageUrls);

        // Reload with includes
        var createdRecipe = await _context.UserRecipes
            .Include(r => r.User)
            .Include(r => r.GlobalRecipe)
            .FirstAsync(r => r.Id == recipe.Id);

        return MapToDto(createdRecipe, userId);
    }

    public async Task<UserRecipeDto> UpdateRecipeAsync(Guid recipeId, Guid userId, UpdateUserRecipeDto dto)
    {
        var recipe = await _context.UserRecipes
            .Include(r => r.User)
            .Include(r => r.GlobalRecipe)
            .FirstOrDefaultAsync(r => r.Id == recipeId && r.UserId == userId)
            ?? throw new InvalidOperationException("Recipe not found or you don't have permission to edit it");

        if (dto.Name != null) recipe.LocalTitle = dto.Name;
        if (dto.Description != null) recipe.LocalDescription = dto.Description;
        if (dto.Ingredients != null) recipe.LocalIngredients = JsonSerializer.Serialize(dto.Ingredients);
        if (dto.ImageUrls != null) recipe.LocalImageUrls = JsonSerializer.Serialize(dto.ImageUrls);
        if (dto.PersonalNotes != null) recipe.PersonalNotes = dto.PersonalNotes;
        if (dto.Visibility != null) recipe.Visibility = dto.Visibility;

        recipe.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(recipe, userId);
    }

    public async Task DeleteRecipeAsync(Guid recipeId, Guid userId)
    {
        var recipe = await _context.UserRecipes
            .FirstOrDefaultAsync(r => r.Id == recipeId && r.UserId == userId)
            ?? throw new InvalidOperationException("Recipe not found or you don't have permission to delete it");

        _context.UserRecipes.Remove(recipe);
        await _context.SaveChangesAsync();
    }

    public async Task<PublishRecipeResultDto> PublishRecipeAsync(Guid recipeId, Guid userId)
    {
        var recipe = await _context.UserRecipes
            .Include(r => r.User)
            .Include(r => r.GlobalRecipe)
            .FirstOrDefaultAsync(r => r.Id == recipeId && r.UserId == userId)
            ?? throw new InvalidOperationException("Recipe not found or you don't have permission");

        if (recipe.GlobalRecipeId.HasValue)
            throw new InvalidOperationException("This recipe is already linked to a global recipe");

        // Create global recipe from local data
        var globalRecipe = new GlobalRecipe
        {
            Id = Guid.NewGuid(),
            Title = recipe.LocalTitle ?? "Untitled Recipe",
            Description = recipe.LocalDescription,
            ImageUrl = recipe.LocalImageUrl,
            ImageUrls = recipe.LocalImageUrls,
            Ingredients = recipe.LocalIngredients ?? "[]",
            CreatedByUserId = userId,
            IsPublic = true,
            IsHellofresh = false,
            IsEditable = false, // Published recipes are not directly editable
            PublishedFromUserRecipeId = recipe.Id,
            TotalTimesAdded = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.GlobalRecipes.Add(globalRecipe);

        // Link the user recipe to the new global recipe
        recipe.GlobalRecipeId = globalRecipe.Id;
        recipe.Visibility = "public";
        recipe.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Record activity
        await _activityFeedService.RecordPublishedActivityAsync(
            userId,
            globalRecipe.Id,
            globalRecipe.Title,
            globalRecipe.ImageUrl);

        return new PublishRecipeResultDto
        {
            UserRecipe = MapToDto(recipe, userId),
            GlobalRecipeId = globalRecipe.Id,
            Message = "Recipe published successfully!"
        };
    }

    public async Task<UserRecipeDto> DetachRecipeAsync(Guid recipeId, Guid userId)
    {
        var recipe = await _context.UserRecipes
            .Include(r => r.GlobalRecipe)
            .FirstOrDefaultAsync(r => r.Id == recipeId && r.UserId == userId)
            ?? throw new InvalidOperationException("Recipe not found or you don't have permission");

        if (!recipe.GlobalRecipeId.HasValue)
            throw new InvalidOperationException("This recipe is not linked to a global recipe");

        // Copy global recipe data to local fields before detaching
        if (recipe.GlobalRecipe != null)
        {
            recipe.LocalTitle ??= recipe.GlobalRecipe.Title;
            recipe.LocalDescription ??= recipe.GlobalRecipe.Description;
            recipe.LocalIngredients ??= recipe.GlobalRecipe.Ingredients;
            recipe.LocalImageUrl ??= recipe.GlobalRecipe.ImageUrl;
            recipe.LocalImageUrls = string.IsNullOrEmpty(recipe.LocalImageUrls) || recipe.LocalImageUrls == "[]"
                ? recipe.GlobalRecipe.ImageUrls
                : recipe.LocalImageUrls;
        }

        // Detach from global recipe
        recipe.GlobalRecipeId = null;
        recipe.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var updatedRecipe = await _context.UserRecipes
            .Include(r => r.User)
            .FirstAsync(r => r.Id == recipeId);

        return MapToDto(updatedRecipe, userId);
    }

    public async Task<UserRecipeDto> RateRecipeAsync(Guid recipeId, Guid userId, int rating, string? comment = null)
    {
        var recipe = await _context.UserRecipes
            .Include(r => r.User)
            .Include(r => r.GlobalRecipe)
            .Include(r => r.Ratings)
            .FirstOrDefaultAsync(r => r.Id == recipeId)
            ?? throw new InvalidOperationException("Recipe not found");

        // Check if user can view this recipe
        if (!await CanUserViewRecipeAsync(recipeId, userId))
            throw new InvalidOperationException("You don't have permission to rate this recipe");

        // Find existing rating or create new
        var existingRating = await _context.Ratings
            .FirstOrDefaultAsync(r => r.UserRecipeId == recipeId && r.UserId == userId);

        if (existingRating != null)
        {
            existingRating.Score = rating;
            existingRating.Comment = comment;
            existingRating.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            var newRating = new Rating
            {
                Id = Guid.NewGuid(),
                UserRecipeId = recipeId,
                GlobalRecipeId = recipe.GlobalRecipeId,
                UserId = userId,
                Score = rating,
                Comment = comment,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.Ratings.Add(newRating);
        }

        // Update global recipe average if linked
        if (recipe.GlobalRecipeId.HasValue)
        {
            await UpdateGlobalRecipeRatingAsync(recipe.GlobalRecipeId.Value);
        }

        await _context.SaveChangesAsync();

        // Record activity
        await _activityFeedService.RecordRatingActivityAsync(
            userId,
            recipeId,
            recipe.DisplayTitle,
            rating,
            recipe.LocalImageUrl);

        // Reload with includes
        var updatedRecipe = await _context.UserRecipes
            .Include(r => r.User)
            .Include(r => r.GlobalRecipe)
            .Include(r => r.Ratings)
                .ThenInclude(rt => rt.User)
            .FirstAsync(r => r.Id == recipeId);

        return MapToDto(updatedRecipe, userId);
    }

    public async Task RemoveRatingAsync(Guid recipeId, Guid userId)
    {
        var rating = await _context.Ratings
            .FirstOrDefaultAsync(r => r.UserRecipeId == recipeId && r.UserId == userId);

        if (rating != null)
        {
            _context.Ratings.Remove(rating);

            var recipe = await _context.UserRecipes.FindAsync(recipeId);
            if (recipe?.GlobalRecipeId.HasValue == true)
            {
                await UpdateGlobalRecipeRatingAsync(recipe.GlobalRecipeId.Value);
            }

            await _context.SaveChangesAsync();
        }
    }

    public async Task<UserRecipeDto> ArchiveRecipeAsync(Guid recipeId, Guid userId)
    {
        var recipe = await _context.UserRecipes
            .Include(r => r.User)
            .Include(r => r.GlobalRecipe)
            .FirstOrDefaultAsync(r => r.Id == recipeId && r.UserId == userId)
            ?? throw new InvalidOperationException("Recipe not found");

        recipe.IsArchived = true;
        recipe.ArchivedDate = DateTime.UtcNow;
        recipe.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(recipe, userId);
    }

    public async Task<UserRecipeDto> RestoreRecipeAsync(Guid recipeId, Guid userId)
    {
        var recipe = await _context.UserRecipes
            .Include(r => r.User)
            .Include(r => r.GlobalRecipe)
            .FirstOrDefaultAsync(r => r.Id == recipeId && r.UserId == userId)
            ?? throw new InvalidOperationException("Recipe not found");

        recipe.IsArchived = false;
        recipe.ArchivedDate = null;
        recipe.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return MapToDto(recipe, userId);
    }

    public async Task<bool> CanUserViewRecipeAsync(Guid recipeId, Guid requestingUserId)
    {
        var recipe = await _context.UserRecipes
            .FirstOrDefaultAsync(r => r.Id == recipeId);

        if (recipe == null) return false;

        // Owner can always view
        if (recipe.UserId == requestingUserId) return true;

        return recipe.Visibility switch
        {
            "public" => true,
            "friends" => await _friendshipService.AreFriendsAsync(recipe.UserId, requestingUserId),
            "household" => await AreInSameHouseholdAsync(recipe.UserId, requestingUserId),
            _ => false // private
        };
    }

    public async Task<AggregatedRecipePagedResult> GetHouseholdRecipesAsync(
        Guid householdId,
        Guid requestingUserId,
        GetCombinedRecipesQuery query)
    {
        // Get all member IDs of the household
        var memberIds = await _context.HouseholdMembers
            .Where(hm => hm.HouseholdId == householdId)
            .Select(hm => hm.UserId)
            .ToListAsync();

        // Check if requesting user is a member
        if (!memberIds.Contains(requestingUserId))
            throw new InvalidOperationException("You are not a member of this household");

        // Get recipes from all members that are visible to household
        var queryable = _context.UserRecipes
            .Include(r => r.User)
            .Include(r => r.GlobalRecipe)
            .Include(r => r.Ratings)
                .ThenInclude(rt => rt.User)
            .Where(r => memberIds.Contains(r.UserId))
            .Where(r => r.Visibility == "household" || r.Visibility == "public" || r.Visibility == "friends")
            .Where(r => !r.IsArchived);

        // Filter by specific members
        if (query.FilterByMembers?.Any() == true)
        {
            queryable = queryable.Where(r => query.FilterByMembers.Contains(r.UserId));
        }

        // Search
        if (!string.IsNullOrEmpty(query.Search))
        {
            queryable = queryable.Where(r =>
                (r.LocalTitle != null && r.LocalTitle.Contains(query.Search)) ||
                (r.GlobalRecipe != null && r.GlobalRecipe.Title.Contains(query.Search)));
        }

        // Sorting
        queryable = query.SortBy.ToLower() switch
        {
            "name" => query.SortDescending
                ? queryable.OrderByDescending(r => r.LocalTitle ?? r.GlobalRecipe!.Title)
                : queryable.OrderBy(r => r.LocalTitle ?? r.GlobalRecipe!.Title),
            "rating" => query.SortDescending
                ? queryable.OrderByDescending(r => r.Ratings.Any() ? r.Ratings.Average(rt => rt.Score) : 0)
                : queryable.OrderBy(r => r.Ratings.Any() ? r.Ratings.Average(rt => rt.Score) : 0),
            "owner" => query.SortDescending
                ? queryable.OrderByDescending(r => r.User.DisplayName)
                : queryable.OrderBy(r => r.User.DisplayName),
            _ => query.SortDescending
                ? queryable.OrderByDescending(r => r.CreatedAt)
                : queryable.OrderBy(r => r.CreatedAt)
        };

        var totalCount = await queryable.CountAsync();
        var recipes = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new AggregatedRecipePagedResult
        {
            Recipes = recipes.Select(r => MapToAggregatedDto(r, memberIds)).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<List<CommonFavoriteDto>> GetCommonFavoritesAsync(
        Guid householdId,
        Guid requestingUserId,
        GetCommonFavoritesQuery query)
    {
        var memberIds = await _context.HouseholdMembers
            .Where(hm => hm.HouseholdId == householdId)
            .Select(hm => hm.UserId)
            .ToListAsync();

        if (!memberIds.Contains(requestingUserId))
            throw new InvalidOperationException("You are not a member of this household");

        // Find global recipes that multiple household members have rated highly
        var commonFavorites = await _context.Ratings
            .Include(r => r.GlobalRecipe)
            .Include(r => r.User)
            .Where(r => r.GlobalRecipeId.HasValue)
            .Where(r => memberIds.Contains(r.UserId))
            .GroupBy(r => r.GlobalRecipeId)
            .Where(g => g.Count() >= query.MinMembers)
            .Where(g => g.Average(r => r.Score) >= query.MinAverageRating)
            .Select(g => new
            {
                GlobalRecipeId = g.Key!.Value,
                Ratings = g.ToList(),
                AverageRating = g.Average(r => r.Score),
                MemberCount = g.Count()
            })
            .OrderByDescending(x => x.AverageRating)
            .ThenByDescending(x => x.MemberCount)
            .Take(query.Limit)
            .ToListAsync();

        var globalRecipeIds = commonFavorites.Select(cf => cf.GlobalRecipeId).ToList();
        var globalRecipes = await _context.GlobalRecipes
            .Where(gr => globalRecipeIds.Contains(gr.Id))
            .ToDictionaryAsync(gr => gr.Id);

        // Check which ones are in the requesting user's collection
        var userRecipeGlobalIds = await _context.UserRecipes
            .Where(ur => ur.UserId == requestingUserId && ur.GlobalRecipeId.HasValue)
            .Select(ur => ur.GlobalRecipeId!.Value)
            .ToListAsync();

        return commonFavorites.Select(cf =>
        {
            var globalRecipe = globalRecipes.GetValueOrDefault(cf.GlobalRecipeId);
            return new CommonFavoriteDto
            {
                GlobalRecipeId = cf.GlobalRecipeId,
                Name = globalRecipe?.Title ?? "Unknown",
                ImageUrl = globalRecipe?.ImageUrl,
                Description = globalRecipe?.Description,
                MemberRatings = cf.Ratings.Select(r => new MemberRatingDto
                {
                    UserId = r.UserId,
                    DisplayName = r.User?.DisplayName ?? "Unknown",
                    AvatarUrl = r.User?.AvatarUrl,
                    Rating = r.Score,
                    Comment = r.Comment,
                    RatedAt = r.CreatedAt
                }).ToList(),
                AverageRating = cf.AverageRating,
                MembersWhoRated = cf.MemberCount,
                IsInMyCollection = userRecipeGlobalIds.Contains(cf.GlobalRecipeId)
            };
        }).ToList();
    }

    // Private helpers

    public async Task<UserRecipePagedResult> GetFriendsRecipesAsync(Guid userId, GetUserRecipesQuery query)
    {
        // Get all friend user IDs
        var friendIds = await _friendshipService.GetFriendIdsAsync(userId);

        if (!friendIds.Any())
        {
            return new UserRecipePagedResult
            {
                Recipes = new List<UserRecipeDto>(),
                TotalCount = 0,
                Page = query.Page,
                PageSize = query.PageSize,
            };
        }

        // Get recipes from friends that are visible to the requesting user
        // Friends can see: public, friends visibility recipes
        var queryable = _context.UserRecipes
            .Include(r => r.User)
            .Include(r => r.GlobalRecipe)
            .Include(r => r.Ratings)
                .ThenInclude(rt => rt.User)
            .Where(r => friendIds.Contains(r.UserId))
            .Where(r => r.Visibility == "friends" || r.Visibility == "public")
            .Where(r => !r.IsArchived);

        // Sort by date by default
        queryable = queryable.OrderByDescending(r => r.CreatedAt);

        var totalCount = await queryable.CountAsync();
        var recipes = await queryable
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        return new UserRecipePagedResult
        {
            Recipes = recipes.Select(r => MapToDto(r, userId)).ToList(),
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize,
        };
    }

    private async Task<bool> AreInSameHouseholdAsync(Guid userId1, Guid userId2)
    {
        var user1Households = await _context.HouseholdMembers
            .Where(hm => hm.UserId == userId1)
            .Select(hm => hm.HouseholdId)
            .ToListAsync();

        return await _context.HouseholdMembers
            .AnyAsync(hm => hm.UserId == userId2 && user1Households.Contains(hm.HouseholdId));
    }

    private async Task UpdateGlobalRecipeRatingAsync(Guid globalRecipeId)
    {
        var ratings = await _context.Ratings
            .Where(r => r.GlobalRecipeId == globalRecipeId)
            .ToListAsync();

        var globalRecipe = await _context.GlobalRecipes.FindAsync(globalRecipeId);
        if (globalRecipe != null)
        {
            globalRecipe.RatingCount = ratings.Count;
            globalRecipe.TotalRatings = ratings.Count;
            globalRecipe.AverageRating = ratings.Any()
                ? (decimal)ratings.Average(r => r.Score)
                : 0;
        }
    }

    private UserRecipeDto MapToDto(UserRecipe recipe, Guid requestingUserId)
    {
        var imageUrls = new List<string>();
        try
        {
            if (!string.IsNullOrEmpty(recipe.LocalImageUrl))
            {
                imageUrls.Add(recipe.LocalImageUrl);
            }
            else if (!string.IsNullOrEmpty(recipe.LocalImageUrls) && recipe.LocalImageUrls != "[]")
            {
                imageUrls = JsonSerializer.Deserialize<List<string>>(recipe.LocalImageUrls) ?? new();
            }
            else if (recipe.GlobalRecipe != null && !string.IsNullOrEmpty(recipe.GlobalRecipe.ImageUrls))
            {
                imageUrls = JsonSerializer.Deserialize<List<string>>(recipe.GlobalRecipe.ImageUrls) ?? new();
            }
        }
        catch { }

        var myRating = recipe.Ratings?.FirstOrDefault(r => r.UserId == requestingUserId);

        var householdRatings = recipe.Ratings?
            .Where(r => r.User != null)
            .ToDictionary(r => r.User!.DisplayName, r => (int?)r.Score)
            ?? new Dictionary<string, int?>();

        return new UserRecipeDto
        {
            Id = recipe.Id,
            UserId = recipe.UserId,
            UserDisplayName = recipe.User?.DisplayName ?? "Unknown",
            UserAvatarUrl = recipe.User?.AvatarUrl,
            Name = recipe.LocalTitle ?? recipe.GlobalRecipe?.Title ?? "Untitled",
            Description = recipe.LocalDescription ?? recipe.GlobalRecipe?.Description,
            ImageUrls = imageUrls,
            Ingredients = recipe.LocalIngredients != null
                ? JsonSerializer.Deserialize<object>(recipe.LocalIngredients)
                : (recipe.GlobalRecipe?.Ingredients != null
                    ? JsonSerializer.Deserialize<object>(recipe.GlobalRecipe.Ingredients)
                    : null),
            GlobalRecipeId = recipe.GlobalRecipeId,
            GlobalRecipeName = recipe.GlobalRecipe?.Title,
            IsPublished = recipe.GlobalRecipe?.PublishedFromUserRecipeId == recipe.Id,
            Visibility = recipe.Visibility,
            PersonalNotes = recipe.PersonalNotes,
            IsArchived = recipe.IsArchived,
            CreatedAt = recipe.CreatedAt,
            UpdatedAt = recipe.UpdatedAt,
            MyRating = myRating?.Score,
            AverageRating = recipe.Ratings?.Any() == true ? recipe.Ratings.Average(r => r.Score) : 0,
            RatingCount = recipe.Ratings?.Count ?? 0,
            HouseholdRatings = householdRatings
        };
    }

    private AggregatedRecipeDto MapToAggregatedDto(UserRecipe recipe, List<Guid> householdMemberIds)
    {
        var imageUrls = new List<string>();
        try
        {
            if (!string.IsNullOrEmpty(recipe.LocalImageUrl))
            {
                imageUrls.Add(recipe.LocalImageUrl);
            }
            else if (!string.IsNullOrEmpty(recipe.LocalImageUrls) && recipe.LocalImageUrls != "[]")
            {
                imageUrls = JsonSerializer.Deserialize<List<string>>(recipe.LocalImageUrls) ?? new();
            }
            else if (recipe.GlobalRecipe != null && !string.IsNullOrEmpty(recipe.GlobalRecipe.ImageUrls))
            {
                imageUrls = JsonSerializer.Deserialize<List<string>>(recipe.GlobalRecipe.ImageUrls) ?? new();
            }
        }
        catch { }

        // Only include ratings from household members
        var householdRatings = recipe.Ratings?
            .Where(r => householdMemberIds.Contains(r.UserId) && r.User != null)
            .ToDictionary(r => r.User!.DisplayName, r => (int?)r.Score)
            ?? new Dictionary<string, int?>();

        var householdRatingValues = householdRatings.Values.Where(v => v.HasValue).Select(v => v!.Value).ToList();

        return new AggregatedRecipeDto
        {
            UserRecipeId = recipe.Id,
            OwnerUserId = recipe.UserId,
            OwnerDisplayName = recipe.User?.DisplayName ?? "Unknown",
            OwnerAvatarUrl = recipe.User?.AvatarUrl,
            Name = recipe.LocalTitle ?? recipe.GlobalRecipe?.Title ?? "Untitled",
            Description = recipe.LocalDescription ?? recipe.GlobalRecipe?.Description,
            ImageUrls = imageUrls,
            GlobalRecipeId = recipe.GlobalRecipeId,
            HouseholdRatings = householdRatings,
            HouseholdAverageRating = householdRatingValues.Any() ? householdRatingValues.Average() : 0,
            HouseholdRatingCount = householdRatingValues.Count,
            CreatedAt = recipe.CreatedAt
        };
    }
}
