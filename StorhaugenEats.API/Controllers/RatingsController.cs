using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StorhaugenEats.API.Services;
using System.Security.Claims;

namespace StorhaugenEats.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RatingsController : ControllerBase
{
    private readonly IRatingService _ratingService;

    public RatingsController(IRatingService ratingService)
    {
        _ratingService = ratingService;
    }

    private Guid GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.Parse(userIdClaim ?? throw new UnauthorizedAccessException());
    }

    [HttpGet("recipe/{globalRecipeId}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRatingsForRecipe(Guid globalRecipeId)
    {
        var ratings = await _ratingService.GetRatingsForRecipeAsync(globalRecipeId);
        return Ok(ratings);
    }

    [HttpGet("recipe/{globalRecipeId}/my-rating")]
    public async Task<IActionResult> GetMyRating(Guid globalRecipeId)
    {
        var userId = GetCurrentUserId();
        var rating = await _ratingService.GetUserRatingForRecipeAsync(userId, globalRecipeId);

        if (rating == null)
            return NotFound();

        return Ok(rating);
    }

    [HttpPost]
    public async Task<IActionResult> UpsertRating([FromBody] UpsertRatingRequest request)
    {
        if (request.Score < 0 || request.Score > 10)
            return BadRequest(new { message = "Score must be between 0 and 10" });

        var userId = GetCurrentUserId();

        var rating = await _ratingService.UpsertRatingAsync(
            userId,
            request.GlobalRecipeId,
            request.Score,
            request.Comment
        );

        return Ok(rating);
    }

    [HttpDelete("recipe/{globalRecipeId}")]
    public async Task<IActionResult> DeleteRating(Guid globalRecipeId)
    {
        var userId = GetCurrentUserId();

        var success = await _ratingService.DeleteRatingAsync(userId, globalRecipeId);

        if (!success)
            return NotFound();

        return NoContent();
    }
}

public record UpsertRatingRequest(Guid GlobalRecipeId, int Score, string? Comment);
