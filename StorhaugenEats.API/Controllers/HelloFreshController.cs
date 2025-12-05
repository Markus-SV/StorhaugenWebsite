using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;
using StorhaugenEats.API.Services;

namespace StorhaugenEats.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HelloFreshController : ControllerBase
{
    private readonly IHelloFreshScraperService _scraperService;
    private readonly AppDbContext _context;

    public HelloFreshController(IHelloFreshScraperService scraperService, AppDbContext context)
    {
        _scraperService = scraperService;
        _context = context;
    }

    [HttpPost("sync")]
    [Authorize] // Require authentication for manual sync
    public async Task<IActionResult> TriggerSync([FromQuery] bool force = false)
    {
        try
        {
            if (!force)
            {
                var shouldRun = await _scraperService.ShouldRunSyncAsync();
                if (!shouldRun)
                {
                    return Ok(new { message = "Sync not needed yet. Use ?force=true to force sync." });
                }
            }

            var (added, updated) = await _scraperService.SyncRecipesAsync();

            return Ok(new
            {
                message = "Sync completed successfully",
                recipesAdded = added,
                recipesUpdated = updated
            });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Sync failed", error = ex.Message });
        }
    }

    [HttpGet("sync-status")]
    [AllowAnonymous]
    public async Task<IActionResult> GetSyncStatus()
    {
        var lastSync = await _context.EtlSyncLogs
            .OrderByDescending(log => log.StartedAt)
            .FirstOrDefaultAsync();

        if (lastSync == null)
        {
            return Ok(new { message = "No sync has been performed yet" });
        }

        return Ok(new
        {
            lastSync = lastSync.StartedAt,
            status = lastSync.Status,
            recipesAdded = lastSync.RecipesAdded,
            recipesUpdated = lastSync.RecipesUpdated,
            buildId = lastSync.BuildId,
            weeksSynced = lastSync.WeeksSynced,
            errorMessage = lastSync.ErrorMessage
        });
    }

    [HttpGet("build-id")]
    [AllowAnonymous]
    public async Task<IActionResult> GetBuildId()
    {
        try
        {
            var buildId = await _scraperService.GetBuildIdAsync();
            return Ok(new { buildId });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to get build ID", error = ex.Message });
        }
    }
}
