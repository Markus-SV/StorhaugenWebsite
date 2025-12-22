using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;
using StorhaugenEats.API.Helpers;
using StorhaugenEats.API.Models;
using StorhaugenWebsite.Shared.DTOs;
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
                .ThenInclude(rt => rt.User)
            .Include(r => r.UserRecipeTags)
                .ThenInclude(rt => rt.Tag)
            .Where(r => r.UserId == userId);

        if (!query.IncludeArchived)
        {
            queryable = queryable.Where(r => !r.IsArchived);
        }

        if (!string.IsNullOrEmpty(query.Visibility) && query.Visibility != "all")
        {
            queryable = queryable.Where(r => r.Visibility == query.Visibility);
        }

        // Search filter
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var searchLower = query.Search.ToLower();
            queryable = queryable.Where(r =>
                (r.LocalTitle != null && r.LocalTitle.ToLower().Contains(searchLower)) ||
                (r.GlobalRecipe != null && r.GlobalRecipe.Title.ToLower().Contains(searchLower)) ||
                (r.LocalDescription != null && r.LocalDescription.ToLower().Contains(searchLower)));
        }

        // Tag filter
        if (query.TagIds != null && query.TagIds.Count > 0)
        {
            queryable = queryable.Where(r => r.UserRecipeTags.Any(rt => query.TagIds.Contains(rt.TagId)));
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
            .Include(r => r.UserRecipeTags)
                .ThenInclude(rt => rt.Tag)
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
                LocalNutritionDataJson = JsonHelper.ObjectToJson(dto.NutritionData),
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

            // 5) Optional: initial ratings (creator can include collection members during creation)
            if (dto.MemberRatings != null && dto.MemberRatings.Any())
            {
                var allowedMemberIds = await GetAllowedMemberIdsForInitialRatingsAsync(userId, dto.CollectionIds);

                var requestedRatings = dto.MemberRatings
                    .Where(kvp => allowedMemberIds.Contains(kvp.Key))
                    .ToList();

                if (requestedRatings.Any())
                {
                    var targetUserIds = requestedRatings.Select(kvp => kvp.Key).ToList();

                    var existingRatings = recipe.GlobalRecipeId.HasValue
                        ? await _context.Ratings
                            .Where(r => r.GlobalRecipeId == recipe.GlobalRecipeId && targetUserIds.Contains(r.UserId))
                            .ToListAsync()
                        : await _context.Ratings
                            .Where(r => r.UserRecipeId == recipe.Id && targetUserIds.Contains(r.UserId))
                            .ToListAsync();

                    foreach (var kvp in requestedRatings)
                    {
                        var clampedScore = Math.Clamp(kvp.Value, 0m, 10m);
                        var existing = existingRatings.FirstOrDefault(r => r.UserId == kvp.Key);

                        if (existing != null)
                        {
                            existing.UserRecipeId = recipe.Id;
                            if (recipe.GlobalRecipeId.HasValue)
                                existing.GlobalRecipeId = recipe.GlobalRecipeId;

                            existing.Score = clampedScore;
                            existing.UpdatedAt = now;
                        }
                        else
                        {
                            _context.Ratings.Add(new Rating
                            {
                                Id = Guid.NewGuid(),
                                UserRecipeId = recipe.Id,
                                GlobalRecipeId = recipe.GlobalRecipeId,
                                UserId = kvp.Key,
                                Score = clampedScore,
                                CreatedAt = now,
                                UpdatedAt = now
                            });
                        }
                    }

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
        if (dto.NutritionData != null) recipe.LocalNutritionDataJson = JsonHelper.ObjectToJson(dto.NutritionData);
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
            NutritionData = recipe.DisplayNutritionDataJson,

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
            recipe.LocalNutritionDataJson ??= recipe.GlobalRecipe.NutritionData;
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
        var visibility = (recipe.Visibility ?? "private").Trim().ToLowerInvariant();

        if (visibility == "public") return true;

        if (visibility == "friends")
        {
            return await _friendshipService.AreFriendsAsync(recipe.UserId, requestingUserId);
        }

        // Treat anything else as private/legacy => allow if accessible through a viewable collection
        return await _collectionService.CanUserViewRecipeViaCollectionAsync(recipeId, requestingUserId);
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
        var imageUrls = JsonHelper.JsonToList(recipe.DisplayImageUrls);

        var myRating = recipe.Ratings?.FirstOrDefault(r => r.UserId == requestingUserId);

        var memberRatings = recipe.Ratings?
            .Where(r => r.User != null && r.UserId != requestingUserId)
            .ToDictionary(r => r.User!.DisplayName, r => (decimal?)r.Score)
            ?? new Dictionary<string, decimal?>();

        var recipeTags = JsonHelper.JsonToList(recipe.GlobalRecipe?.Tags);
        var nutritionData = JsonHelper.JsonToObject(recipe.DisplayNutritionDataJson);

        object? ingredients = null;
        if (!string.IsNullOrWhiteSpace(recipe.LocalIngredients))
            ingredients = JsonHelper.JsonToObject(recipe.LocalIngredients);
        else if (!string.IsNullOrWhiteSpace(recipe.GlobalRecipe?.Ingredients))
            ingredients = JsonHelper.JsonToObject(recipe.GlobalRecipe.Ingredients);

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
            ImageUrls = imageUrls,
            Ingredients = ingredients,

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
            AverageRating = recipe.Ratings?.Any() == true ? (double)recipe.Ratings.Average(r => r.Score) : 0,
            RatingCount = recipe.Ratings?.Count ?? 0,
            MemberRatings = memberRatings,

            Tags = tags
        };
    }

    private async Task<HashSet<Guid>> GetAllowedMemberIdsForInitialRatingsAsync(
        Guid creatorUserId,
        List<Guid>? collectionIds)
    {
        var allowed = new HashSet<Guid> { creatorUserId };

        if (collectionIds == null || collectionIds.Count == 0)
            return allowed;

        var creatorCollections = await _context.CollectionMembers
            .Where(cm => collectionIds.Contains(cm.CollectionId) && cm.UserId == creatorUserId)
            .Select(cm => cm.CollectionId)
            .ToListAsync();

        if (!creatorCollections.Any())
            return allowed;

        var memberIds = await _context.CollectionMembers
            .Where(cm => creatorCollections.Contains(cm.CollectionId))
            .Select(cm => cm.UserId)
            .ToListAsync();

        foreach (var memberId in memberIds)
            allowed.Add(memberId);

        return allowed;
    }

}
