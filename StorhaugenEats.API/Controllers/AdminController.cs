using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StorhaugenEats.API.Services;

namespace StorhaugenEats.API.Controllers;

/// <summary>
/// Admin endpoints for system management tasks like data migration.
/// These endpoints should be protected and only accessible by administrators.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AdminController : ControllerBase
{
    private readonly IDataMigrationService _migrationService;
    private readonly ILogger<AdminController> _logger;

    public AdminController(IDataMigrationService migrationService, ILogger<AdminController> logger)
    {
        _migrationService = migrationService;
        _logger = logger;
    }

    /// <summary>
    /// Gets the current migration statistics.
    /// </summary>
    [HttpGet("migration/stats")]
    public async Task<ActionResult<MigrationStats>> GetMigrationStats()
    {
        try
        {
            var stats = await _migrationService.GetMigrationStatsAsync();
            return Ok(stats);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get migration stats");
            return StatusCode(500, new { error = "Failed to get migration statistics" });
        }
    }

    /// <summary>
    /// Runs the complete migration from household-centric to user-centric model.
    /// </summary>
    /// <param name="dryRun">If true (default), only simulates the migration without making changes.</param>
    [HttpPost("migration/run")]
    public async Task<ActionResult<CompleteMigrationResult>> RunMigration([FromQuery] bool dryRun = true)
    {
        try
        {
            _logger.LogInformation("Migration requested by user. DryRun: {DryRun}", dryRun);

            var result = await _migrationService.RunCompleteMigrationAsync(dryRun);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(500, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Migration failed");
            return StatusCode(500, new { error = "Migration failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Migrates only household recipes to user recipes.
    /// </summary>
    /// <param name="dryRun">If true (default), only simulates the migration without making changes.</param>
    [HttpPost("migration/recipes")]
    public async Task<ActionResult<MigrationResult>> MigrateRecipes([FromQuery] bool dryRun = true)
    {
        try
        {
            _logger.LogInformation("Recipe migration requested. DryRun: {DryRun}", dryRun);

            var result = await _migrationService.MigrateHouseholdRecipesToUserRecipesAsync(dryRun);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(500, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Recipe migration failed");
            return StatusCode(500, new { error = "Recipe migration failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Migrates ratings to reference user recipes.
    /// </summary>
    /// <param name="dryRun">If true (default), only simulates the migration without making changes.</param>
    [HttpPost("migration/ratings")]
    public async Task<ActionResult<MigrationResult>> MigrateRatings([FromQuery] bool dryRun = true)
    {
        try
        {
            _logger.LogInformation("Ratings migration requested. DryRun: {DryRun}", dryRun);

            var result = await _migrationService.MigrateRatingsAsync(dryRun);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(500, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ratings migration failed");
            return StatusCode(500, new { error = "Ratings migration failed", message = ex.Message });
        }
    }

    /// <summary>
    /// Migrates household friendships to user friendships.
    /// </summary>
    /// <param name="dryRun">If true (default), only simulates the migration without making changes.</param>
    [HttpPost("migration/friendships")]
    public async Task<ActionResult<MigrationResult>> MigrateFriendships([FromQuery] bool dryRun = true)
    {
        try
        {
            _logger.LogInformation("Friendships migration requested. DryRun: {DryRun}", dryRun);

            var result = await _migrationService.MigrateHouseholdFriendshipsAsync(dryRun);

            if (result.Success)
            {
                return Ok(result);
            }
            else
            {
                return StatusCode(500, result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Friendships migration failed");
            return StatusCode(500, new { error = "Friendships migration failed", message = ex.Message });
        }
    }
}
