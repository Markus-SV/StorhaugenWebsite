namespace StorhaugenEats.API.Services;

public class StorageOptions
{
    public IList<string> AllowedImageHosts { get; set; } = new List<string>();
    public long MaxDownloadBytes { get; set; } = 5 * 1024 * 1024; // 5MB default
    public int DownloadTimeoutSeconds { get; set; } = 10;
}
