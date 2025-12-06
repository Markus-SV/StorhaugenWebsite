namespace StorhaugenEats.API.DTOs;

public class HouseholdRecipeDto
{
    public int Id { get; set; }
    public int HouseholdId { get; set; }
    public required string Name { get; set; }
    public string? Description { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public Dictionary<string, int?> Ratings { get; set; } = new();
    public double AverageRating { get; set; }
    public DateTime DateAdded { get; set; }
    public int AddedByUserId { get; set; }
    public string? AddedByName { get; set; }
    public bool IsArchived { get; set; }
    public DateTime? ArchivedDate { get; set; }
    public int? ArchivedByUserId { get; set; }
    public string? ArchivedByName { get; set; }

    // If linked to global recipe
    public int? GlobalRecipeId { get; set; }
    public string? GlobalRecipeName { get; set; }
    public bool IsForked { get; set; }
    public string? PersonalNotes { get; set; }
}

public class CreateHouseholdRecipeDto
{
    public required string Name { get; set; }
    public string? Description { get; set; }
    public List<string> ImageUrls { get; set; } = new();
    public string? PersonalNotes { get; set; }

    // Optional: Link to global recipe
    public int? GlobalRecipeId { get; set; }
    public bool Fork { get; set; } = false; // If true, copy recipe; if false, link to it
}

public class UpdateHouseholdRecipeDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string>? ImageUrls { get; set; }
    public string? PersonalNotes { get; set; }
}

public class RateRecipeDto
{
    public int Rating { get; set; } // 0-10
}
