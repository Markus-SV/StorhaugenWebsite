using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;
using StorhaugenEats.API.Models;

namespace StorhaugenEats.API.Services;

public class DataMigrationService : IDataMigrationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<DataMigrationService> _logger;

    public DataMigrationService(AppDbContext context, ILogger<DataMigrationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<MigrationResult> MigrateHouseholdRecipesToUserRecipesAsync(bool dryRun = true)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new MigrationResult { WasDryRun = dryRun };

        try
        {
            _logger.LogInformation("Starting household recipe migration (DryRun: {DryRun})", dryRun);

            // Get all household recipes that haven't been migrated yet
            var householdRecipes = await _context.HouseholdRecipes
                .Include(hr => hr.Household)
                .Where(hr => hr.AddedByUserId != null)
                .ToListAsync();

            result.ItemsProcessed = householdRecipes.Count;

            // Get existing user recipe IDs to check for duplicates
            var existingUserRecipeIds = await _context.UserRecipes
                .Select(ur => ur.Id)
                .ToHashSetAsync();

            foreach (var hr in householdRecipes)
            {
                try
                {
                    // Skip if already migrated (same ID exists in user_recipes)
                    if (existingUserRecipeIds.Contains(hr.Id))
                    {
                        result.ItemsSkipped++;
                        result.Warnings.Add($"Recipe {hr.Id} already exists in user_recipes, skipping");
                        continue;
                    }

                    // Map visibility
                    var visibility = hr.IsPublic ? "public" : "household";

                    var userRecipe = new UserRecipe
                    {
                        Id = hr.Id, // Preserve the same ID for rating references
                        UserId = hr.AddedByUserId!.Value,
                        GlobalRecipeId = hr.GlobalRecipeId,
                        LocalTitle = hr.LocalTitle,
                        LocalDescription = hr.LocalDescription,
                        LocalIngredients = hr.LocalIngredients,
                        LocalImageUrl = hr.LocalImageUrl,
                        LocalImageUrls = hr.LocalImageUrl != null ? $"[\"{hr.LocalImageUrl}\"]" : "[]",
                        PersonalNotes = hr.PersonalNotes,
                        Visibility = visibility,
                        IsArchived = hr.IsArchived,
                        ArchivedDate = hr.ArchivedDate,
                        CreatedAt = hr.CreatedAt,
                        UpdatedAt = hr.UpdatedAt
                    };

                    if (!dryRun)
                    {
                        _context.UserRecipes.Add(userRecipe);
                    }

                    result.ItemsMigrated++;
                }
                catch (Exception ex)
                {
                    result.ItemsFailed++;
                    result.Errors.Add($"Failed to migrate recipe {hr.Id}: {ex.Message}");
                    _logger.LogError(ex, "Failed to migrate household recipe {RecipeId}", hr.Id);
                }
            }

            if (!dryRun && result.ItemsMigrated > 0)
            {
                await _context.SaveChangesAsync();
            }

            result.Success = result.ItemsFailed == 0;
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            _logger.LogInformation(
                "Household recipe migration completed. Processed: {Processed}, Migrated: {Migrated}, Skipped: {Skipped}, Failed: {Failed}",
                result.ItemsProcessed, result.ItemsMigrated, result.ItemsSkipped, result.ItemsFailed);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Migration failed: {ex.Message}");
            _logger.LogError(ex, "Household recipe migration failed");
        }

        return result;
    }

    public async Task<MigrationResult> MigrateRatingsAsync(bool dryRun = true)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new MigrationResult { WasDryRun = dryRun };

        try
        {
            _logger.LogInformation("Starting ratings migration (DryRun: {DryRun})", dryRun);

            // Get all ratings that have household_recipe_id but not user_recipe_id
            var ratingsToMigrate = await _context.Ratings
                .Where(r => r.HouseholdRecipeId != null && r.UserRecipeId == null)
                .ToListAsync();

            result.ItemsProcessed = ratingsToMigrate.Count;

            // Get valid user recipe IDs
            var userRecipeIds = await _context.UserRecipes
                .Select(ur => ur.Id)
                .ToHashSetAsync();

            foreach (var rating in ratingsToMigrate)
            {
                try
                {
                    // Check if a user recipe exists with the same ID
                    if (userRecipeIds.Contains(rating.HouseholdRecipeId!.Value))
                    {
                        if (!dryRun)
                        {
                            rating.UserRecipeId = rating.HouseholdRecipeId;
                        }
                        result.ItemsMigrated++;
                    }
                    else
                    {
                        result.ItemsSkipped++;
                        result.Warnings.Add($"Rating {rating.Id}: No matching user recipe for household recipe {rating.HouseholdRecipeId}");
                    }
                }
                catch (Exception ex)
                {
                    result.ItemsFailed++;
                    result.Errors.Add($"Failed to migrate rating {rating.Id}: {ex.Message}");
                    _logger.LogError(ex, "Failed to migrate rating {RatingId}", rating.Id);
                }
            }

            if (!dryRun && result.ItemsMigrated > 0)
            {
                await _context.SaveChangesAsync();
            }

            result.Success = result.ItemsFailed == 0;
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            _logger.LogInformation(
                "Ratings migration completed. Processed: {Processed}, Migrated: {Migrated}, Skipped: {Skipped}, Failed: {Failed}",
                result.ItemsProcessed, result.ItemsMigrated, result.ItemsSkipped, result.ItemsFailed);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Migration failed: {ex.Message}");
            _logger.LogError(ex, "Ratings migration failed");
        }

        return result;
    }

    public async Task<MigrationResult> MigrateHouseholdFriendshipsAsync(bool dryRun = true)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new MigrationResult { WasDryRun = dryRun };

        try
        {
            _logger.LogInformation("Starting household friendships migration (DryRun: {DryRun})", dryRun);

            // Get all household friendships
            var householdFriendships = await _context.HouseholdFriendships
                .Include(hf => hf.RequesterHousehold)
                .Include(hf => hf.TargetHousehold)
                .Where(hf => hf.Status == "accepted")
                .ToListAsync();

            result.ItemsProcessed = householdFriendships.Count;

            // Get household leaders (created_by is the leader)
            var householdLeaders = await _context.Households
                .ToDictionaryAsync(h => h.Id, h => h.CreatedById);

            // Get existing user friendships to avoid duplicates
            var existingFriendships = await _context.UserFriendships
                .Select(uf => new { uf.RequesterUserId, uf.TargetUserId })
                .ToListAsync();

            var existingPairs = existingFriendships
                .Select(f => (Math.Min(f.RequesterUserId.GetHashCode(), f.TargetUserId.GetHashCode()),
                              Math.Max(f.RequesterUserId.GetHashCode(), f.TargetUserId.GetHashCode())))
                .ToHashSet();

            foreach (var hf in householdFriendships)
            {
                try
                {
                    if (!householdLeaders.TryGetValue(hf.RequesterHouseholdId, out var requesterId) ||
                        !householdLeaders.TryGetValue(hf.TargetHouseholdId, out var targetId))
                    {
                        result.ItemsSkipped++;
                        result.Warnings.Add($"Friendship {hf.Id}: Could not find household leaders");
                        continue;
                    }

                    // Skip self-friendships
                    if (requesterId == targetId)
                    {
                        result.ItemsSkipped++;
                        continue;
                    }

                    // Check for duplicates (in either direction)
                    var pairHash = (Math.Min(requesterId.GetHashCode(), targetId.GetHashCode()),
                                   Math.Max(requesterId.GetHashCode(), targetId.GetHashCode()));

                    if (existingPairs.Contains(pairHash))
                    {
                        result.ItemsSkipped++;
                        result.Warnings.Add($"Friendship between users {requesterId} and {targetId} already exists");
                        continue;
                    }

                    var userFriendship = new UserFriendship
                    {
                        Id = Guid.NewGuid(),
                        RequesterUserId = requesterId,
                        TargetUserId = targetId,
                        Status = hf.Status,
                        Message = hf.Message,
                        CreatedAt = hf.CreatedAt,
                        RespondedAt = hf.RespondedAt
                    };

                    if (!dryRun)
                    {
                        _context.UserFriendships.Add(userFriendship);
                    }

                    existingPairs.Add(pairHash);
                    result.ItemsMigrated++;
                }
                catch (Exception ex)
                {
                    result.ItemsFailed++;
                    result.Errors.Add($"Failed to migrate friendship {hf.Id}: {ex.Message}");
                    _logger.LogError(ex, "Failed to migrate household friendship {FriendshipId}", hf.Id);
                }
            }

            if (!dryRun && result.ItemsMigrated > 0)
            {
                await _context.SaveChangesAsync();
            }

            result.Success = result.ItemsFailed == 0;
            stopwatch.Stop();
            result.Duration = stopwatch.Elapsed;

            _logger.LogInformation(
                "Household friendships migration completed. Processed: {Processed}, Migrated: {Migrated}, Skipped: {Skipped}, Failed: {Failed}",
                result.ItemsProcessed, result.ItemsMigrated, result.ItemsSkipped, result.ItemsFailed);
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.Errors.Add($"Migration failed: {ex.Message}");
            _logger.LogError(ex, "Household friendships migration failed");
        }

        return result;
    }

    public async Task<CompleteMigrationResult> RunCompleteMigrationAsync(bool dryRun = true)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new CompleteMigrationResult { WasDryRun = dryRun };

        _logger.LogInformation("Starting complete migration (DryRun: {DryRun})", dryRun);

        // Step 1: Migrate recipes
        result.RecipesMigration = await MigrateHouseholdRecipesToUserRecipesAsync(dryRun);

        // Step 2: Migrate ratings (depends on recipes)
        result.RatingsMigration = await MigrateRatingsAsync(dryRun);

        // Step 3: Migrate friendships
        result.FriendshipsMigration = await MigrateHouseholdFriendshipsAsync(dryRun);

        stopwatch.Stop();
        result.TotalDuration = stopwatch.Elapsed;
        result.Success = result.RecipesMigration.Success &&
                        result.RatingsMigration.Success &&
                        result.FriendshipsMigration.Success;

        _logger.LogInformation(
            "Complete migration finished. Success: {Success}, Duration: {Duration}",
            result.Success, result.TotalDuration);

        return result;
    }

    public async Task<MigrationStats> GetMigrationStatsAsync()
    {
        var stats = new MigrationStats();

        // Count household recipes
        stats.TotalHouseholdRecipes = await _context.HouseholdRecipes.CountAsync();

        // Count user recipes
        stats.TotalUserRecipes = await _context.UserRecipes.CountAsync();

        // Count household recipes not yet migrated (no matching user recipe)
        var userRecipeIds = await _context.UserRecipes.Select(ur => ur.Id).ToListAsync();
        stats.HouseholdRecipesNotMigrated = await _context.HouseholdRecipes
            .Where(hr => hr.AddedByUserId != null && !userRecipeIds.Contains(hr.Id))
            .CountAsync();

        // Count ratings by reference type
        stats.RatingsWithHouseholdRecipeId = await _context.Ratings
            .Where(r => r.HouseholdRecipeId != null)
            .CountAsync();

        stats.RatingsWithUserRecipeId = await _context.Ratings
            .Where(r => r.UserRecipeId != null)
            .CountAsync();

        // Count friendships
        stats.TotalHouseholdFriendships = await _context.HouseholdFriendships
            .Where(hf => hf.Status == "accepted")
            .CountAsync();

        stats.TotalUserFriendships = await _context.UserFriendships
            .Where(uf => uf.Status == "accepted")
            .CountAsync();

        // Check if migration is complete
        stats.MigrationComplete = stats.HouseholdRecipesNotMigrated == 0 &&
                                  stats.RatingsWithHouseholdRecipeId == stats.RatingsWithUserRecipeId;

        return stats;
    }
}
