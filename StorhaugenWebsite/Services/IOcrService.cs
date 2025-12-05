namespace StorhaugenWebsite.Services // Adjust namespace if your folder structure is different
{
    public interface IOcrService
    {
        Task<string> RecognizeTextAsync(string imageBase64DataUrl);
    }
}