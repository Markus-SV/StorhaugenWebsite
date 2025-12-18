namespace StorhaugenWebsite.Services;

/// <summary>
/// Service that generates consistent colors for users based on their username.
/// This ensures the same user always gets the same color across the entire app.
/// </summary>
public interface IUserColorService
{
    /// <summary>
    /// Gets a consistent hex color for a username.
    /// </summary>
    string GetUserColor(string? username);

    /// <summary>
    /// Gets a consistent hex color for a user ID.
    /// </summary>
    string GetUserColorById(Guid userId);

    /// <summary>
    /// Gets the CSS style string for background-color based on username.
    /// </summary>
    string GetUserColorStyle(string? username);
}

public class UserColorService : IUserColorService
{
    // A pleasing palette of distinct, accessible colors
    private static readonly string[] ColorPalette = {
        "#2E7D32", // Green
        "#00695C", // Teal
        "#0277BD", // Light Blue
        "#1565C0", // Blue
        "#283593", // Indigo
        "#4527A0", // Deep Purple
        "#6A1B9A", // Purple
        "#AD1457", // Pink
        "#C62828", // Red
        "#D84315", // Deep Orange
        "#EF6C00", // Orange
        "#F9A825", // Yellow/Amber
        "#558B2F", // Light Green
        "#00838F", // Cyan
        "#5D4037", // Brown
        "#455A64"  // Blue Grey
    };

    /// <inheritdoc />
    public string GetUserColor(string? username)
    {
        if (string.IsNullOrEmpty(username))
            return "#9E9E9E"; // Grey for unknown/empty usernames

        // Use a simple hash that's consistent across sessions
        int hash = GetStableHash(username.ToLowerInvariant());
        int index = Math.Abs(hash) % ColorPalette.Length;
        return ColorPalette[index];
    }

    /// <inheritdoc />
    public string GetUserColorById(Guid userId)
    {
        if (userId == Guid.Empty)
            return "#9E9E9E";

        int hash = GetStableHash(userId.ToString());
        int index = Math.Abs(hash) % ColorPalette.Length;
        return ColorPalette[index];
    }

    /// <inheritdoc />
    public string GetUserColorStyle(string? username)
    {
        return $"background-color: {GetUserColor(username)}";
    }

    /// <summary>
    /// Generates a stable hash code that's consistent across app sessions.
    /// Unlike string.GetHashCode(), this is deterministic.
    /// </summary>
    private static int GetStableHash(string str)
    {
        unchecked
        {
            int hash = 17;
            foreach (char c in str)
            {
                hash = hash * 31 + c;
            }
            return hash;
        }
    }
}
