namespace StorhaugenWebsite.Models
{
    public class FoodItem
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public List<string> ImageUrls { get; set; } = new();
        public Dictionary<string, int?> Ratings { get; set; } = new();
        public DateTime DateAdded { get; set; } = DateTime.UtcNow;
        public string AddedBy { get; set; } = string.Empty;
        public bool IsArchived { get; set; } = false;
        public DateTime? ArchivedDate { get; set; }
        public string? ArchivedBy { get; set; }

        // Multi-tenant fields
        public Guid? GlobalRecipeId { get; set; }
        public string? GlobalRecipeName { get; set; }
        public bool IsForked { get; set; }
        public string? PersonalNotes { get; set; }

        public double AverageRating
        {
            get
            {
                var validRatings = Ratings.Values.Where(r => r.HasValue).Select(r => r!.Value).ToList();
                return validRatings.Count > 0 ? validRatings.Average() : 0;
            }
        }

        public int GetRatingForPerson(string person)
        {
            return Ratings.TryGetValue(person, out var rating) ? rating ?? 0 : 0;
        }

        // Helper property to check if this recipe is linked to a global recipe
        public bool IsLinkedToGlobal => GlobalRecipeId.HasValue && !IsForked;
    }
}