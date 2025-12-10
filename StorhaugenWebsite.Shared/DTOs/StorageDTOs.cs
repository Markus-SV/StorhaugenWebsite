namespace StorhaugenWebsite.Shared.DTOs;

public class UploadImageDto
{
    public required string FileName { get; set; }
    public required string Base64Data { get; set; }
    public string Bucket { get; set; } = "recipe-images";
}

public class UploadImageResultDto
{
    public required string Url { get; set; }
    public required string FileName { get; set; }
}

public class DeleteImageDto
{
    public required string FileName { get; set; }
    public string Bucket { get; set; } = "recipe-images";
}
