namespace StorhaugenEats.API.Services;

public interface ICurrentUserService
{
    /// <summary>
    /// Gets the current authenticated user's email from JWT
    /// </summary>
    string? GetUserEmail();

    /// <summary>
    /// Gets the current authenticated user's Supabase UUID from JWT
    /// </summary>
    string? GetUserSubject();

    /// <summary>
    /// Gets or creates the user in the database and returns their ID
    /// </summary>
    Task<int> GetOrCreateUserIdAsync();

    /// <summary>
    /// Checks if user is authenticated
    /// </summary>
    bool IsAuthenticated();
}
