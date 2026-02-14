using Microsoft.Extensions.Options;
using Supabase;

namespace StorhaugenEats.API.Services;

public class SupabaseStorageService : IStorageService
{
    private readonly Client _supabaseClient;
    private readonly HttpClient _httpClient;
    private readonly StorageOptions _options;
    private const string BucketName = "recipe-images";

    private static readonly HashSet<string> AllowedSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        Uri.UriSchemeHttp,
        Uri.UriSchemeHttps
    };

    public SupabaseStorageService(Client supabaseClient, HttpClient httpClient, IOptions<StorageOptions> options)
    {
        _supabaseClient = supabaseClient;
        _httpClient = httpClient;
        _options = options.Value;

        if (_options.DownloadTimeoutSeconds > 0)
        {
            _httpClient.Timeout = TimeSpan.FromSeconds(_options.DownloadTimeoutSeconds);
        }
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
        if (!Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException("Image URL is invalid.");
        }

        if (!AllowedSchemes.Contains(uri.Scheme))
        {
            throw new InvalidOperationException("Image URL must use HTTP or HTTPS.");
        }

        if (_options.AllowedImageHosts.Any() &&
            !_options.AllowedImageHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Image host is not allowed.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, uri);
        var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var contentType = response.Content.Headers.ContentType?.MediaType;
        if (string.IsNullOrWhiteSpace(contentType) || !contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Remote content is not an image.");
        }

        var maxBytes = _options.MaxDownloadBytes;
        if (response.Content.Headers.ContentLength is long contentLength && contentLength > maxBytes)
        {
            throw new InvalidOperationException($"Image exceeds maximum allowed size of {maxBytes} bytes.");
        }

        using var stream = await response.Content.ReadAsStreamAsync();
        using var limitedStream = new MemoryStream();
        var buffer = new byte[81920];
        int bytesRead;
        long totalRead = 0;

        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) > 0)
        {
            totalRead += bytesRead;
            if (totalRead > maxBytes)
            {
                throw new InvalidOperationException($"Image exceeds maximum allowed size of {maxBytes} bytes.");
            }

            await limitedStream.WriteAsync(buffer, 0, bytesRead);
        }

        limitedStream.Position = 0;
        return await UploadImageAsync(limitedStream, fileName, folder);
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
