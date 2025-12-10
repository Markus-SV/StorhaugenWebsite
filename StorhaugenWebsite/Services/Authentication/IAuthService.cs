namespace StorhaugenWebsite.Services
{
    public interface IAuthService
    {
        event Action? OnAuthStateChanged;
        bool IsAuthenticated { get; }
        bool IsAuthorized { get; }
        string? CurrentUserEmail { get; }
        string? CurrentUserName { get; }

        Task<(bool success, string? errorMessage)> LoginAsync();
        Task LogoutAsync();
        Task InitializeAsync();
        Task<string?> GetAccessTokenAsync();

        // Add this missing line:
        void UpdateCachedDisplayName(string? displayName);
    }
}