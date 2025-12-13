using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;
using StorhaugenEats.API.Services;
using StorhaugenWebsite.Shared.DTOs;
using System.Security.Claims;

namespace StorhaugenEats.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RatingsController : ControllerBase
{
    private readonly IRatingService _ratingService;
    private readonly IUserFriendshipService _friendshipService;
    private readonly ICurrentUserService _currentUserService; // <--- 1. Add Service
    private readonly AppDbContext _context;

    public RatingsController(
        IRatingService ratingService,
        IUserFriendshipService friendshipService,
        ICurrentUserService currentUserService, // <--- 2. Inject here
        AppDbContext context)
    {
        _ratingService = ratingService;
        _friendshipService = friendshipService;
        _currentUserService = currentUserService; // <--- 3. Assign here
        _context = context;
    }

    // REMOVE THIS METHOD COMPLETELY
    // private Guid GetCurrentUserId() { ... } 

    // Public: ratings for a recipe
    [HttpGet("recipe/{globalRecipeId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> GetRatingsForRecipe(Guid globalRecipeId)
    {
        var ratings = await _ratingService.GetRatingsForRecipeAsync(globalRecipeId);
        return Ok(ratings);
    }

    // Private: my rating for a recipe
    [HttpGet("recipe/{globalRecipeId:guid}/my-rating")]
    public async Task<IActionResult> GetMyRating(Guid globalRecipeId)
    {
        // FIX: Use service
        var userId = await _currentUserService.GetOrCreateUserIdAsync();

        var rating = await _ratingService.GetUserRatingForRecipeAsync(userId, globalRecipeId);
        if (rating == null)
            return NotFound();

        return Ok(rating);
    }

    // Private: upsert my rating
    [HttpPost]
    public async Task<IActionResult> UpsertRating([FromBody] UpsertRatingRequest request)
    {
        if (request.Score < 0 || request.Score > 10)
            return BadRequest(new { message = "Score must be between 0 and 10." });

        // FIX: Use service
        var userId = await _currentUserService.GetOrCreateUserIdAsync();

        var rating = await _ratingService.UpsertRatingAsync(
            userId,
            request.GlobalRecipeId,
            request.Score,
            request.Comment
        );

        return Ok(rating);
    }

    // Private: delete my rating
    [HttpDelete("recipe/{globalRecipeId:guid}")]
    public async Task<IActionResult> DeleteRating(Guid globalRecipeId)
    {
        // FIX: Use service
        var userId = await _currentUserService.GetOrCreateUserIdAsync();

        var success = await _ratingService.DeleteRatingAsync(userId, globalRecipeId);

        if (!success)
            return NotFound();

        return NoContent();
    }


    [HttpGet("user/{profileUserId:guid}")]
    public async Task<ActionResult<List<UserRatingDto>>> GetUserRatings(
    Guid profileUserId,
    [FromQuery] int skip = 0,
    [FromQuery] int take = 50)
    {
        var requestingUserId = await _currentUserService.GetOrCreateUserIdAsync();
        bool areFriends = false;

        if (requestingUserId != profileUserId)
        {
            areFriends = await _friendshipService.AreFriendsAsync(requestingUserId, profileUserId);
            if (!areFriends) return Forbid();
        }

        if (skip < 0) skip = 0;
        if (take < 1) take = 50;

        // Fetch ratings with includes
        var ratings = await _context.Ratings
            .AsNoTracking()
            .Include(r => r.GlobalRecipe)
            .Include(r => r.UserRecipe)
            .Where(r => r.UserId == profileUserId)
            .OrderByDescending(r => r.UpdatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();

        var result = new List<UserRatingDto>();

        foreach (var r in ratings)
        {
            // 1. Global Recipe Logic (Always visible)
            if (r.GlobalRecipe != null)
            {
                result.Add(new UserRatingDto
                {
                    GlobalRecipeId = r.GlobalRecipeId,
                    RecipeTitle = r.GlobalRecipe.Title,
                    ImageUrl = r.GlobalRecipe.ImageUrl, // Or verify ImageUrls logic
                    Score = r.Score,
                    Comment = r.Comment,
                    RatedAt = r.UpdatedAt
                });
            }
            // 2. User Recipe Logic (Check Visibility)
            else if (r.UserRecipe != null)
            {
                bool isVisible = r.UserRecipe.Visibility == "public" ||
                                 (r.UserRecipe.Visibility == "friends" && areFriends) ||
                                 requestingUserId == profileUserId; // Own profile

                if (isVisible)
                {
                    result.Add(new UserRatingDto
                    {
                        UserRecipeId = r.UserRecipeId,
                        RecipeTitle = r.UserRecipe.DisplayTitle, // Use display helper
                        ImageUrl = r.UserRecipe.DisplayImageUrls != "[]"
                            ? StorhaugenEats.API.Helpers.JsonHelper.JsonToList(r.UserRecipe.DisplayImageUrls).FirstOrDefault()
                            : null,
                        Score = r.Score,
                        Comment = r.Comment,
                        RatedAt = r.UpdatedAt
                    });
                }
            }
        }

        return Ok(result);
    }

}

public record UpsertRatingRequest(Guid GlobalRecipeId, int Score, string? Comment);
