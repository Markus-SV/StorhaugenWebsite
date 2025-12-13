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