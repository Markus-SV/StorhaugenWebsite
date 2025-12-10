namespace StorhaugenEats.API.Services;

/// <summary>
/// Service for migrating data from the old household-centric model to the new user-centric model.
/// </summary>
public interface IDataMigrationService
{
    /// <summary>
    /// Migrates all household recipes to user recipes.
    /// </summary>
    /// <param name="dryRun">If true, only simulates the migration without making changes.</param>
    /// <returns>Migration result with counts and any errors.</returns>
    Task<MigrationResult> MigrateHouseholdRecipesToUserRecipesAsync(bool dryRun = true);

    /// <summary>
    /// Updates ratings to reference user recipes instead of household recipes.
    /// </summary>
    /// <param name="dryRun">If true, only simulates the migration without making changes.</param>
    /// <returns>Migration result with counts and any errors.</returns>
    Task<MigrationResult> MigrateRatingsAsync(bool dryRun = true);

    /// <summary>
    /// Migrates household friendships to user friendships (between household leaders).
    /// </summary>
    /// <param name="dryRun">If true, only simulates the migration without making changes.</param>
    /// <returns>Migration result with counts and any errors.</returns>
    Task<MigrationResult> MigrateHouseholdFriendshipsAsync(bool dryRun = true);

    /// <summary>
    /// Runs the complete migration in the correct order.
    /// </summary>
    /// <param name="dryRun">If true, only simulates the migration without making changes.</param>
    /// <returns>Combined migration result.</returns>
    Task<CompleteMigrationResult> RunCompleteMigrationAsync(bool dryRun = true);

    /// <summary>
    /// Gets statistics about the current migration state.
    /// </summary>
    Task<MigrationStats> GetMigrationStatsAsync();
}

public class MigrationResult
{
    public bool Success { get; set; }
    public int ItemsProcessed { get; set; }
    public int ItemsMigrated { get; set; }
    public int ItemsSkipped { get; set; }
    public int ItemsFailed { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public TimeSpan Duration { get; set; }
    public bool WasDryRun { get; set; }
}

public class CompleteMigrationResult
{
    public bool Success { get; set; }
    public MigrationResult RecipesMigration { get; set; } = new();
    public MigrationResult RatingsMigration { get; set; } = new();
    public MigrationResult FriendshipsMigration { get; set; } = new();
    public TimeSpan TotalDuration { get; set; }
    public bool WasDryRun { get; set; }
}

public class MigrationStats
{
    public int TotalHouseholdRecipes { get; set; }
    public int TotalUserRecipes { get; set; }
    public int HouseholdRecipesNotMigrated { get; set; }
    public int RatingsWithHouseholdRecipeId { get; set; }
    public int RatingsWithUserRecipeId { get; set; }
    public int TotalHouseholdFriendships { get; set; }
    public int TotalUserFriendships { get; set; }
    public bool MigrationComplete { get; set; }
    public DateTime? LastMigrationRun { get; set; }
}
