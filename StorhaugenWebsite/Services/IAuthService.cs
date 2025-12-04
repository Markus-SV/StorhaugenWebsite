using StorhaugenWebsite.Models;

namespace StorhaugenWebsite.Services
{
    public interface IAuthService
    {
        event Action? OnAuthStateChanged;
        bool IsAuthenticated { get; }
        bool IsAuthorized { get; }
        string? CurrentUserEmail { get; }
        FamilyMember? CurrentUser { get; }
        Task<(bool success, string? errorMessage)> LoginAsync();
        Task LogoutAsync();
        Task InitializeAsync();
    }
}