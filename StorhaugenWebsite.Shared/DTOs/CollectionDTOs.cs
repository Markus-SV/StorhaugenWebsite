namespace StorhaugenWebsite.Shared.DTOs;

// ==========================================
// COLLECTION DTOs - Recipe Organization
// ==========================================

/// <summary>
/// DTO for returning collection data.
/// </summary>
public class CollectionDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>
    /// Visibility: "private" (members only), "friends" (owner's friends can view), "public" (anyone can view)
    /// </summary>
    public string Visibility { get; set; } = "private";

    /// <summary>
    /// Share code for public/friends collections. Can be used to generate share links.
    /// </summary>
    public string? ShareCode { get; set; }

    public Guid OwnerId { get; set; }
    public string OwnerDisplayName { get; set; } = string.Empty;
    public string? OwnerAvatarUrl { get; set; }

    public int RecipeCount { get; set; }
    public int MemberCount { get; set; }

    /// <summary>
    /// Whether the current user is the owner of this collection.
    /// </summary>
    public bool IsOwner { get; set; }

    /// <summary>
    /// Whether the current user is a member of this collection.
    /// </summary>
    public bool IsMember { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    /// <summary>
    /// Members of the collection.
    /// </summary>
    public List<CollectionMemberDto> Members { get; set; } = new();
}

/// <summary>
/// Simplified collection reference for use in recipe DTOs.
/// </summary>
public class CollectionReferenceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

/// <summary>
/// DTO for collection members.
/// </summary>
public class CollectionMemberDto
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public bool IsOwner { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// DTO for creating a new collection.
/// </summary>
public class CreateCollectionDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Visibility { get; set; } = "private";
}

/// <summary>
/// DTO for updating a collection.
/// </summary>
public class UpdateCollectionDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public string? Visibility { get; set; }
}

/// <summary>
/// DTO for adding a recipe to a collection.
/// </summary>
public class AddRecipeToCollectionDto
{
    public Guid UserRecipeId { get; set; }
}

/// <summary>
/// DTO for adding a member to a collection.
/// </summary>
public class AddCollectionMemberDto
{
    /// <summary>
    /// The user's unique share ID or email.
    /// </summary>
    public string UserIdentifier { get; set; } = string.Empty;
}

/// <summary>
/// Query parameters for getting recipes in a collection.
/// </summary>
public class GetCollectionRecipesQuery
{
    public string SortBy { get; set; } = "added"; // "added", "name", "rating", "date"
    public bool SortDescending { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? Search { get; set; }
}

/// <summary>
/// Result for collection recipes with pagination.
/// </summary>
public class CollectionRecipesResult
{
    public CollectionDto Collection { get; set; } = null!;
    public List<UserRecipeDto> Recipes { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
