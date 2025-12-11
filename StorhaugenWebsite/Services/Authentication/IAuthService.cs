namespace StorhaugenWebsite.Services
{
    public interface IAuthService
    {
        event Action? OnAuthStateChanged;
        bool IsAuthenticated { get; }
        bool IsAuthorized { get; }
        string? CurrentUserEmail { get; }
        string? CurrentUserName { get; }

        Task<(bool success, string? errorMessage)> LoginAsync(); // Keep for Google
        // --- ADD THESE ---
        Task<(bool success, string? errorMessage)> SignInWithEmailAsync(string email, string password);
        Task<(bool success, string? errorMessage)> SignUpWithEmailAsync(string email, string password, string displayName);
        // -----------------
        Task LogoutAsync();
        Task InitializeAsync();
        Task<string?> GetAccessTokenAsync();
        void UpdateCachedDisplayName(string? displayName);
    }
}