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
    [AllowAnonymous] // Allow unauthenticated access for testing
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

    [HttpGet("test-proxy")]
    [AllowAnonymous]
    public async Task<IActionResult> TestProxy()
    {
        // The specific URL you found (Note: The ID '7.98.24323' changes when HelloFresh updates their site)
        var url = "https://www.hellofresh.no/_next/data/7.98.24323/menus/2026-W01.json?week=2026-W01";

        using var client = new HttpClient();

        // Mimic a real browser to avoid getting blocked
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");

        try
        {
            var json = await client.GetStringAsync(url);
            // We return the raw JSON string to the frontend to let it handle deserialization
            return Content(json, "application/json");
        }
        catch (Exception ex)
        {
            return BadRequest(new { message = "Failed to fetch from HelloFresh", error = ex.Message });
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

    [HttpGet("weeks")]
    [AllowAnonymous]
    public async Task<IActionResult> GetAvailableWeeks()
    {
        // Get distinct weeks from HelloFresh recipes, ordered descending (newest first)
        var weeks = await _context.GlobalRecipes
            .Where(gr => gr.IsHellofresh && gr.HellofreshWeek != null)
            .Select(gr => gr.HellofreshWeek!)
            .Distinct()
            .OrderByDescending(w => w)
            .ToListAsync();

        return Ok(weeks);
    }
}
