using Supabase;

namespace StorhaugenEats.API.Services;

public class SupabaseStorageService : IStorageService
{
    private readonly Client _supabaseClient;
    private readonly HttpClient _httpClient;
    private const string BucketName = "recipe-images";

    public SupabaseStorageService(Client supabaseClient, HttpClient httpClient)
    {
        _supabaseClient = supabaseClient;
        _httpClient = httpClient;
    }

    public async Task<string> UploadImageAsync(Stream imageStream, string fileName, string folder = "recipes")
    {
        var path = $"{folder}/{Guid.NewGuid()}_{fileName}";

        // Convert stream to byte array
        using var memoryStream = new MemoryStream();
        await imageStream.CopyToAsync(memoryStream);
        var bytes = memoryStream.ToArray();

        // Upload to Supabase Storage
        await _supabaseClient.Storage
            .From(BucketName)
            .Upload(bytes, path);

        // Get public URL
        var publicUrl = _supabaseClient.Storage
            .From(BucketName)
            .GetPublicUrl(path);

        return publicUrl;
    }

    public async Task<string> UploadImageFromUrlAsync(string imageUrl, string fileName, string folder = "recipes")
    {
        // Download image from URL
        var response = await _httpClient.GetAsync(imageUrl);
        response.EnsureSuccessStatusCode();

        using var stream = await response.Content.ReadAsStreamAsync();
        return await UploadImageAsync(stream, fileName, folder);
    }

    public async Task<bool> DeleteImageAsync(string imageUrl)
    {
        try
        {
            // Extract path from public URL
            var uri = new Uri(imageUrl);
            var pathSegments = uri.AbsolutePath.Split('/');
            var bucketIndex = Array.IndexOf(pathSegments, BucketName);

            if (bucketIndex == -1) return false;

            var path = string.Join("/", pathSegments.Skip(bucketIndex + 1));

            await _supabaseClient.Storage
                .From(BucketName)
                .Remove(path);

            return true;
        }
        catch
        {
            return false;
        }
    }
}
