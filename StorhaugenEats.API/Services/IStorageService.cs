namespace StorhaugenEats.API.Services;

public interface IStorageService
{
    Task<string> UploadImageAsync(Stream imageStream, string fileName, string folder = "recipes");
    Task<string> UploadImageFromUrlAsync(string imageUrl, string fileName, string folder = "recipes");
    Task<bool> DeleteImageAsync(string imageUrl);
}
