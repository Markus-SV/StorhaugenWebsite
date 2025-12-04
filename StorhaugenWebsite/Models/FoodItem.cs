namespace StorhaugenWebsite.Models
{
    public class FoodItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; } = DateTime.Now;
        public bool IsArchived { get; set; } = false;

        // List of image URLs from Firebase Storage
        public List<string> ImageUrls { get; set; } = new();

        // List of ratings from specific users
        public List<UserRating> Ratings { get; set; } = new();

        // Helper to get average
        public double AverageRating => Ratings.Any() ? Ratings.Average(r => r.Score) : 0;
    }

    public class UserRating
    {
        public string UserEmail { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty; // "Markus", "Siv", "Elias"
        public int Score { get; set; } // 0-10
    }
}