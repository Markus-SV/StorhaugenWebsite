using MudBlazor;
using StorhaugenWebsite.Models;

namespace StorhaugenWebsite.Services
{
    public interface IThemeService
    {
        bool IsDarkMode { get; }
        event Action? OnThemeChanged;

        Task InitializeAsync();
        MudTheme GetCurrentTheme();
        IEnumerable<string> GetAvailableThemes();
        string GetCurrentThemeName();
        Task SetThemeAsync(string themeName);
        ThemeColors GetThemeColors(string themeName);
        bool IsThemeDark(string themeName);
    }
}