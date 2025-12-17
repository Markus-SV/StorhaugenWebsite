namespace StorhaugenWebsite.Shared.DTOs;

public class UserRatingDto
{
    public Guid? GlobalRecipeId { get; set; }
    public Guid? UserRecipeId { get; set; }
    public string RecipeTitle { get; set; } = "";
    public string? ImageUrl { get; set; }
    public decimal Score { get; set; }
    public string? Comment { get; set; }
    public DateTime RatedAt { get; set; }
}
