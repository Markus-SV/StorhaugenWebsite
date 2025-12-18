using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;
using StorhaugenWebsite.Shared.DTOs;
using StorhaugenEats.API.Models;
using System.Text.Json;

namespace StorhaugenEats.API.Services;

public class UserRecipeService : IUserRecipeService
{
    private readonly AppDbContext _context;
    private readonly IActivityFeedService _activityFeedService;
    private readonly IUserFriendshipService _friendshipService;
    private readonly ICollectionService _collectionService;

    public UserRecipeService(
        AppDbContext context,
        IActivityFeedService activityFeedService,
        IUserFriendshipService friendshipService,
        ICollectionService collectionService)
    {
        _context = context;
        _activityFeedService = activityFeedService;
        _friendshipService = friendshipService;
        _collectionService = collectionService;
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
        // 1) Validate user exists
        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
        if (!userExists)
            throw new InvalidOperationException("User not found");

        // 2) Idempotency: if adding a GlobalRecipe, return existing (or restore if archived)
        if (dto.GlobalRecipeId.HasValue)
        {
            var existing = await _context.UserRecipes
                .Include(r => r.User)
                .Include(r => r.GlobalRecipe)
                .Include(r => r.Ratings).ThenInclude(rt => rt.User)
                .FirstOrDefaultAsync(r => r.UserId == userId && r.GlobalRecipeId == dto.GlobalRecipeId.Value);

            if (existing != null)
            {
                // If it exists but is archived, restore it (treat as "added back")
                if (existing.IsArchived)
                {
                    existing.IsArchived = false;
                    existing.ArchivedDate = null;
                    existing.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    // record activity (optional, but usually nice UX)
                    var img = dto.ImageUrls?.FirstOrDefault()
                              ?? existing.GlobalRecipe?.ImageUrl
                              ?? existing.LocalImageUrl;

                    await _activityFeedService.RecordAddedRecipeActivityAsync(
                        userId, existing.Id, existing.DisplayTitle, img);
                }

                return MapToDto(existing, userId);
            }
        }

        var now = DateTime.UtcNow;

        await using var tx = await _context.Database.BeginTransactionAsync();
        try
        {
            // 3) Create new user recipe
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

                // Local metadata fields
                LocalPrepTimeMinutes = dto.PrepTimeMinutes,
                LocalCookTimeMinutes = dto.CookTimeMinutes,
                LocalServings = dto.Servings,
                LocalDifficulty = dto.Difficulty,
                LocalCuisine = dto.Cuisine,

                CreatedAt = now,
                UpdatedAt = now
            };

            _context.UserRecipes.Add(recipe);

            // 4) Increment global usage count (atomic) ONLY if we are actually inserting successfully
            if (dto.GlobalRecipeId.HasValue)
            {
                var affected = await _context.Database.ExecuteSqlInterpolatedAsync($@"
                UPDATE global_recipes
                SET total_times_added = total_times_added + 1
                WHERE id = {dto.GlobalRecipeId.Value};
            ");

                if (affected == 0)
                    throw new InvalidOperationException("Global recipe not found");
            }

            await _context.SaveChangesAsync();

            // 5) Optional: initial rating from creator only (keeps your existing rule)
            if (dto.MemberRatings != null && dto.MemberRatings.Any())
            {
                var ratingsToAdd = dto.MemberRatings
                    .Where(kvp => kvp.Key == userId)
                    .Select(kvp => new Rating
                    {
                        Id = Guid.NewGuid(),
                        UserRecipeId = recipe.Id,
                        GlobalRecipeId = recipe.GlobalRecipeId,
                        UserId = kvp.Key,
                        Score = Math.Clamp(kvp.Value, 0m, 10m),
                        CreatedAt = now,
                        UpdatedAt = now
                    })
                    .ToList();

                if (ratingsToAdd.Any())
                {
                    _context.Ratings.AddRange(ratingsToAdd);
                    await _context.SaveChangesAsync();

                    if (recipe.GlobalRecipeId.HasValue)
                    {
                        await UpdateGlobalRecipeRatingAsync(recipe.GlobalRecipeId.Value);
                        await _context.SaveChangesAsync(); // <-- important if UpdateGlobalRecipeRatingAsync sets fields
                    }
                }
            }

            await tx.CommitAsync();

            // 6) Load with includes for DTO + activity text/image
            var createdRecipe = await _context.UserRecipes
                .Include(r => r.User)
                .Include(r => r.GlobalRecipe)
                .Include(r => r.Ratings).ThenInclude(rt => rt.User)
                .FirstAsync(r => r.Id == recipe.Id);

            var imageForActivity = dto.ImageUrls?.FirstOrDefault()
                                  ?? createdRecipe.GlobalRecipe?.ImageUrl
                                  ?? createdRecipe.LocalImageUrl;

            await _activityFeedService.RecordAddedRecipeActivityAsync(
                userId, createdRecipe.Id, createdRecipe.DisplayTitle, imageForActivity);

            return MapToDto(createdRecipe, userId);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is Npgsql.PostgresException pg
                  && pg.SqlState == Npgsql.PostgresErrorCodes.UniqueViolation)
        {
            await tx.RollbackAsync();

            // If the insert raced / double-clicked, return the existing row (idempotent behavior)
            if (dto.GlobalRecipeId.HasValue)
            {
                var existing = await _context.UserRecipes
                    .Include(r => r.User)
                    .Include(r => r.GlobalRecipe)
                    .Include(r => r.Ratings).ThenInclude(rt => rt.User)
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.GlobalRecipeId == dto.GlobalRecipeId.Value);

                if (existing != null)
                    return MapToDto(existing, userId);
            }

            throw;
        }
        catch
        {
            await tx.RollbackAsync();
            throw;
        }
    }


    public async Task<UserRecipeDto> UpdateRecipeAsync(Guid recipeId, Guid userId, UpdateUserRecipeDto dto)
    {
        var recipe = await _context.UserRecipes
            .Include(r => r.User)
            .Include(r => r.GlobalRecipe)
            .FirstOrDefaultAsync(r => r.Id == recipeId && r.UserId == userId)
            ?? throw new InvalidOperationException("Recipe not found or you don't have permission to edit it");

        // Check if recipe is published - published recipes cannot be edited (only deleted)
        bool isPublished = recipe.GlobalRecipe?.PublishedFromUserRecipeId == recipe.Id;
        if (isPublished)
        {
            // Only allow personal notes and visibility changes on published recipes
            if (dto.PersonalNotes != null) recipe.PersonalNotes = dto.PersonalNotes;
            if (dto.Visibility != null) recipe.Visibility = dto.Visibility;
            recipe.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return MapToDto(recipe, userId);
        }

        // Check if recipe is HelloFresh - HelloFresh recipes have restrictions
        bool isHelloFresh = recipe.GlobalRecipe?.IsHellofresh ?? false;

        // Basic fields - always editable for non-published recipes
        if (dto.Name != null) recipe.LocalTitle = dto.Name;
        if (dto.Description != null) recipe.LocalDescription = dto.Description;
        if (dto.Ingredients != null) recipe.LocalIngredients = JsonSerializer.Serialize(dto.Ingredients);
        if (dto.ImageUrls != null) recipe.LocalImageUrls = JsonSerializer.Serialize(dto.ImageUrls);
        if (dto.PersonalNotes != null) recipe.PersonalNotes = dto.PersonalNotes;
        if (dto.Visibility != null) recipe.Visibility = dto.Visibility;

        // Metadata fields - only editable for non-HelloFresh recipes
        if (!isHelloFresh)
        {
            if (dto.PrepTimeMinutes.HasValue) recipe.LocalPrepTimeMinutes = dto.PrepTimeMinutes;
            if (dto.CookTimeMinutes.HasValue) recipe.LocalCookTimeMinutes = dto.CookTimeMinutes;
            if (dto.Servings.HasValue) recipe.LocalServings = dto.Servings;
            if (dto.Difficulty != null) recipe.LocalDifficulty = dto.Difficulty;
            if (dto.Cuisine != null) recipe.LocalCuisine = dto.Cuisine;
        }

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

        var globalRecipe = new GlobalRecipe
        {
            Id = Guid.NewGuid(),
            Title = recipe.LocalTitle ?? "Untitled Recipe",
            Description = recipe.LocalDescription,
            ImageUrl = recipe.LocalImageUrl,
            ImageUrls = recipe.LocalImageUrls ?? "[]",
            Ingredients = recipe.LocalIngredients ?? "[]",

            // Metadata fields
            PrepTimeMinutes = recipe.LocalPrepTimeMinutes,
            CookTimeMinutes = recipe.LocalCookTimeMinutes,
            TotalTimeMinutes = (recipe.LocalPrepTimeMinutes ?? 0) + (recipe.LocalCookTimeMinutes ?? 0) > 0
                ? (recipe.LocalPrepTimeMinutes ?? 0) + (recipe.LocalCookTimeMinutes ?? 0)
                : null,
            Servings = recipe.LocalServings,
            Difficulty = recipe.LocalDifficulty,
            Cuisine = recipe.LocalCuisine,

            CreatedByUserId = userId,
            IsPublic = true,
            IsHellofresh = false,
            IsEditable = false,
            PublishedFromUserRecipeId = recipe.Id,
            TotalTimesAdded = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.GlobalRecipes.Add(globalRecipe);

        recipe.GlobalRecipeId = globalRecipe.Id;
        recipe.Visibility = "public";
        recipe.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

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

        recipe.GlobalRecipeId = null;
        recipe.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var updatedRecipe = await _context.UserRecipes
            .Include(r => r.User)
            .FirstAsync(r => r.Id == recipeId);

        return MapToDto(updatedRecipe, userId);
    }

    public async Task<UserRecipeDto> RateRecipeAsync(Guid recipeId, Guid userId, decimal rating, string? comment = null)
    {
        var recipe = await _context.UserRecipes
            .Include(r => r.User)
            .Include(r => r.GlobalRecipe)
            .Include(r => r.Ratings)
            .FirstOrDefaultAsync(r => r.Id == recipeId)
            ?? throw new InvalidOperationException("Recipe not found");

        if (!await CanUserViewRecipeAsync(recipeId, userId))
            throw new InvalidOperationException("You don't have permission to rate this recipe");

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

        if (recipe.GlobalRecipeId.HasValue)
        {
            await UpdateGlobalRecipeRatingAsync(recipe.GlobalRecipeId.Value);
        }

        await _context.SaveChangesAsync();

        await _activityFeedService.RecordRatingActivityAsync(
            userId,
            recipeId,
            recipe.DisplayTitle,
            rating,
            recipe.LocalImageUrl);

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

        if (recipe.UserId == requestingUserId) return true;

        // Check visibility
        if (recipe.Visibility == "public") return true;
        if (recipe.Visibility == "friends" && await _friendshipService.AreFriendsAsync(recipe.UserId, requestingUserId))
            return true;

        // Legacy "household" visibility: treat as collection-based access
        // Also check collection membership for private recipes shared via collections
        if (recipe.Visibility == "household" || recipe.Visibility == "private")
        {
            if (await _collectionService.CanUserViewRecipeViaCollectionAsync(recipeId, requestingUserId))
                return true;
        }

        return false;
    }

    public async Task<UserRecipePagedResult> GetFriendsRecipesAsync(Guid userId, GetUserRecipesQuery query)
    {
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

        var queryable = _context.UserRecipes
            .Include(r => r.User)
            .Include(r => r.GlobalRecipe)
            .Include(r => r.Ratings)
                .ThenInclude(rt => rt.User)
            .Where(r => friendIds.Contains(r.UserId))
            .Where(r => r.Visibility == "friends" || r.Visibility == "public")
            .Where(r => !r.IsArchived);

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

        var memberRatings = recipe.Ratings?
            .Where(r => r.User != null)
            .ToDictionary(r => r.User!.DisplayName, r => (decimal?)r.Score)
            ?? new Dictionary<string, decimal?>();

        // Parse recipe tags from GlobalRecipe
        var recipeTags = new List<string>();
        if (recipe.GlobalRecipe?.Tags != null)
        {
            try { recipeTags = JsonSerializer.Deserialize<List<string>>(recipe.GlobalRecipe.Tags) ?? new(); }
            catch { }
        }

        // Parse nutrition data from GlobalRecipe
        object? nutritionData = null;
        if (recipe.GlobalRecipe?.NutritionData != null)
        {
            try { nutritionData = JsonSerializer.Deserialize<object>(recipe.GlobalRecipe.NutritionData); }
            catch { }
        }

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

            // Metadata - use local values if available, otherwise fall back to GlobalRecipe
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
            Visibility = recipe.Visibility,
            PersonalNotes = recipe.PersonalNotes,
            IsArchived = recipe.IsArchived,
            CreatedAt = recipe.CreatedAt,
            UpdatedAt = recipe.UpdatedAt,
            MyRating = myRating?.Score,
            AverageRating = recipe.Ratings?.Any() == true ? recipe.Ratings.Average(r => r.Score) : 0,
            RatingCount = recipe.Ratings?.Count ?? 0,
            MemberRatings = memberRatings
        };
    }
}