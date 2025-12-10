using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StorhaugenWebsite.Shared.DTOs;
using StorhaugenEats.API.Services;

namespace StorhaugenEats.API.Controllers;

[ApiController]
[Route("api/feed")]
[Authorize]
public class FeedController : ControllerBase
{
    private readonly IActivityFeedService _activityFeedService;
    private readonly ICurrentUserService _currentUserService;

    public FeedController(
        IActivityFeedService activityFeedService,
        ICurrentUserService currentUserService)
    {
        _activityFeedService = activityFeedService;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// Get the activity feed (friend activities).
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ActivityFeedPagedResult>> GetFeed([FromQuery] ActivityFeedQuery query)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var result = await _activityFeedService.GetFeedAsync(userId, query);
        return Ok(result);
    }

    /// <summary>
    /// Get the current user's activity history.
    /// </summary>
    [HttpGet("my-activity")]
    public async Task<ActionResult<ActivityFeedPagedResult>> GetMyActivity([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var result = await _activityFeedService.GetUserActivityAsync(userId, page, pageSize);
        return Ok(result);
    }

    /// <summary>
    /// Get a summary of the current user's activity.
    /// </summary>
    [HttpGet("summary")]
    public async Task<ActionResult<ActivitySummaryDto>> GetActivitySummary()
    {
        var userId = await _currentUserService.GetOrCreateUserIdAsync();
        var summary = await _activityFeedService.GetActivitySummaryAsync(userId);
        return Ok(summary);
    }
}
