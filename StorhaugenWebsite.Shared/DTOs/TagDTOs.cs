namespace StorhaugenWebsite.Shared.DTOs;

// ==========================================
// TAG DTOs - Personal Recipe Organization
// ==========================================

/// <summary>
/// DTO representing a recipe tag/category.
/// </summary>
public class TagDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Icon { get; set; }
    public int RecipeCount { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for creating a new tag.
/// </summary>
public class CreateTagDto
{
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string? Icon { get; set; }
}

/// <summary>
/// DTO for updating a tag.
/// </summary>
public class UpdateTagDto
{
    public string? Name { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }
}

/// <summary>
/// DTO for adding/removing tags from a recipe.
/// </summary>
public class UpdateRecipeTagsDto
{
    /// <summary>
    /// List of tag IDs to set on the recipe (replaces existing tags).
    /// </summary>
    public List<Guid> TagIds { get; set; } = new();
}

/// <summary>
/// Simplified tag reference for use in recipe DTOs.
/// </summary>
public class TagReferenceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
}
