namespace StorhaugenWebsite.Shared.DTOs;

public class HelloFreshRawResponse
{
    public PageProps PageProps { get; set; }
}

public class PageProps
{
    public SsrPayload SsrPayload { get; set; }
}

public class SsrPayload
{
    public List<HfCourse> Courses { get; set; } = new();
}

public class HfCourse
{
    public HfRecipe Recipe { get; set; }
}

public class HfRecipe
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Headline { get; set; }
    public string ImageLink { get; set; } // e.g. https://d3hvwccx09j84u.cloudfront.net/...
    public string PrepTime { get; set; }  // e.g. PT20M
    public List<HfIngredient> Ingredients { get; set; }
}

public class HfIngredient
{
    public string Name { get; set; }
    public string ImagePath { get; set; }
}

public class HelloFreshSyncResult
{
    public string Message { get; set; } = string.Empty;
    public int RecipesAdded { get; set; }
    public int RecipesUpdated { get; set; }
}

public class HelloFreshSyncStatus
{
    public DateTime? LastSync { get; set; }
    public string? Status { get; set; }
    public int RecipesAdded { get; set; }
    public int RecipesUpdated { get; set; }
    public string? BuildId { get; set; }
    public string? WeeksSynced { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Message { get; set; }
}