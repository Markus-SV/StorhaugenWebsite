using MudBlazor;
using StorhaugenWebsite.Models;

namespace StorhaugenWebsite.Services
{
    public class ThemeService : IThemeService
    {
        private readonly IDeviceStateService _deviceState;
        private Dictionary<string, MudTheme> _themes = new();
        private string? _systemPreference;

        public bool IsDarkMode { get; private set; }
        public event Action? OnThemeChanged;

        public ThemeService(IDeviceStateService deviceState)
        {
            _deviceState = deviceState;
        }

        public async Task InitializeAsync()
        {
            _systemPreference = await _deviceState.GetSystemThemePreferenceAsync();

            _themes["System"] = _systemPreference == "dark" ? GenerateDarkTheme() : GenerateLightTheme();
            _themes["Light"] = GenerateLightTheme();
            _themes["Dark"] = GenerateDarkTheme();
            _themes["Black"] = GenerateBlackTheme();
            _themes["Material"] = GenerateMaterialTheme();
            _themes["Forest"] = GenerateForestTheme();

            UpdateDarkModeState();
        }

        public MudTheme GetCurrentTheme()
        {
            var themeName = _deviceState.Settings.Theme;
            if (_themes.TryGetValue(themeName, out var theme))
            {
                return theme;
            }
            return _themes["Light"];
        }

        public IEnumerable<string> GetAvailableThemes() => _themes.Keys;

        public string GetCurrentThemeName() => _deviceState.Settings.Theme;

        public async Task SetThemeAsync(string themeName)
        {
            if (!_themes.ContainsKey(themeName)) return;

            await _deviceState.SetThemeAsync(themeName);
            UpdateDarkModeState();
            OnThemeChanged?.Invoke();
        }

        public ThemeColors GetThemeColors(string themeName)
        {
            if (!_themes.TryGetValue(themeName, out var theme))
            {
                theme = _themes["Light"];
            }

            var isDark = IsThemeDark(themeName);

            Palette palette = isDark ? theme.PaletteDark : theme.PaletteLight;

            return new ThemeColors
            {
                Primary = palette.Primary.ToString(),
                Info = palette.Info.ToString(),
                AppbarBackground = palette.AppbarBackground.ToString(),
                Surface = palette.Surface.ToString(),
                Background = palette.Background.ToString(),
                BackgroundGrey = palette.BackgroundGray.ToString(), // Note: MudBlazor v7 uses 'BackgroundGray'
                Divider = palette.Divider.ToString(),
                TextPrimary = palette.TextPrimary.ToString(),
                TextSecondary = palette.TextSecondary.ToString()
            };
        }

        public bool IsThemeDark(string themeName)
        {
            if (themeName == "System")
            {
                return _systemPreference == "dark";
            }
            return themeName != "Light";
        }

        private void UpdateDarkModeState()
        {
            IsDarkMode = IsThemeDark(_deviceState.Settings.Theme);
        }

        // ========== LIGHT THEME (Warm Amber) ==========
        private MudTheme GenerateLightTheme()
        {
            return new MudTheme
            {
                PaletteLight = new PaletteLight
                {
                    Primary = "#E07A2E",
                    PrimaryDarken = "#C56820",
                    PrimaryLighten = "#F5A65B",
                    Secondary = "#2D5A45",
                    Tertiary = "#4A7C9B",
                    Info = "#4A7C9B",
                    Success = "#2D5A45",
                    Warning = "#D4A017",
                    Error = "#C44536",
                    Background = "#FBF8F4",
                    Surface = "#FFFFFF",
                    AppbarBackground = "#FFFFFF",
                    AppbarText = "#2C2416",
                    DrawerBackground = "#FFFFFF",
                    TextPrimary = "#2C2416",
                    TextSecondary = "#6B5D4D",
                    ActionDefault = "#6B5D4D",
                    Divider = "#E8E2D9",
                    DividerLight = "#F0EBE4",
                    BackgroundGray = "#F5F0E8"
                },
                Typography = GetTypography(),
                LayoutProperties = new LayoutProperties { DefaultBorderRadius = "16px" }
            };
        }

        // ========== DARK THEME (Warm Dark) ==========
        private MudTheme GenerateDarkTheme()
        {
            return new MudTheme
            {
                PaletteDark = new PaletteDark
                {
                    Primary = "#F5A65B",
                    PrimaryDarken = "#E07A2E",
                    PrimaryLighten = "#FFCF9E",
                    Secondary = "#5DBE8A",
                    Tertiary = "#6BA3C7",
                    Info = "#6BA3C7",
                    Success = "#5DBE8A",
                    Warning = "#F5C842",
                    Error = "#E86B5B",
                    Background = "#1A1612",
                    Surface = "#2C2416",
                    AppbarBackground = "#2C2416",
                    AppbarText = "#F5F0E8",
                    DrawerBackground = "#2C2416",
                    TextPrimary = "#F5F0E8",
                    TextSecondary = "#A89B8A",
                    ActionDefault = "#A89B8A",
                    Divider = "#3D3428",
                    DividerLight = "#4A3F32",
                    BackgroundGray = "#241F19"
                },
                Typography = GetTypography(),
                LayoutProperties = new LayoutProperties { DefaultBorderRadius = "16px" }
            };
        }

        // ========== BLACK THEME ==========
        private MudTheme GenerateBlackTheme()
        {
            return new MudTheme
            {
                PaletteDark = new PaletteDark
                {
                    Primary = "#5e48d4",
                    PrimaryDarken = "#4a38b0",
                    PrimaryLighten = "#7a66e8",
                    Secondary = "#8b5cf6",
                    Tertiary = "#a78bfa",
                    Info = "#5e48d4",
                    Success = "#22c55e",
                    Warning = "#eab308",
                    Error = "#ef4444",
                    Background = "#000000",
                    Surface = "#161515",
                    AppbarBackground = "#0e0e0e",
                    AppbarText = "#ffffff",
                    DrawerBackground = "#161515",
                    TextPrimary = "#ffffff",
                    TextSecondary = "#a1a1aa",
                    ActionDefault = "#a1a1aa",
                    Divider = "#27272a",
                    DividerLight = "#3f3f46",
                    BackgroundGray = "#201f1f"
                },
                Typography = GetTypography(),
                LayoutProperties = new LayoutProperties { DefaultBorderRadius = "16px" }
            };
        }

        // ========== MATERIAL THEME ==========
        private MudTheme GenerateMaterialTheme()
        {
            return new MudTheme
            {
                PaletteDark = new PaletteDark
                {
                    Primary = "#ff6a6a",
                    PrimaryDarken = "#e05555",
                    PrimaryLighten = "#ff8a8a",
                    Secondary = "#64b5f6",
                    Tertiary = "#81c784",
                    Info = "#ff6a6a",
                    Success = "#81c784",
                    Warning = "#ffb74d",
                    Error = "#e57373",
                    Background = "#141414",
                    Surface = "#1e1d1d",
                    AppbarBackground = "#1e1d1d",
                    AppbarText = "#e1e1e1",
                    DrawerBackground = "#1e1d1d",
                    TextPrimary = "#e1e1e1",
                    TextSecondary = "#9e9e9e",
                    ActionDefault = "#9e9e9e",
                    Divider = "#424242",
                    DividerLight = "#616161",
                    BackgroundGray = "#2f2e2e"
                },
                Typography = GetTypography(),
                LayoutProperties = new LayoutProperties { DefaultBorderRadius = "16px" }
            };
        }

        // ========== FOREST THEME (Nattskog) ==========
        private MudTheme GenerateForestTheme()
        {
            return new MudTheme
            {
                PaletteDark = new PaletteDark
                {
                    Primary = "#348202",
                    PrimaryDarken = "#286601",
                    PrimaryLighten = "#4ca812",
                    Secondary = "#238636",
                    Tertiary = "#58a6ff",
                    Info = "#348202",
                    Success = "#238636",
                    Warning = "#d29922",
                    Error = "#f85149",
                    Background = "#0d1117",
                    Surface = "#161b22",
                    AppbarBackground = "#161b22",
                    AppbarText = "#e6edf3",
                    DrawerBackground = "#161b22",
                    TextPrimary = "#e6edf3",
                    TextSecondary = "#8b949e",
                    ActionDefault = "#8b949e",
                    Divider = "#30363d",
                    DividerLight = "#484f58",
                    BackgroundGray = "#14151c"
                },
                Typography = GetTypography(),
                LayoutProperties = new LayoutProperties { DefaultBorderRadius = "16px" }
            };
        }

        private Typography GetTypography()
        {
            return new Typography
            {
                Default = new DefaultTypography
                {
                    FontFamily = new[] { "DM Sans", "system-ui", "sans-serif" }
                },
                H1 = new H1Typography { FontFamily = new[] { "Fraunces", "Georgia", "serif" }, FontWeight = "600" },
                H2 = new H2Typography { FontFamily = new[] { "Fraunces", "Georgia", "serif" }, FontWeight = "600" },
                H3 = new H3Typography { FontFamily = new[] { "Fraunces", "Georgia", "serif" }, FontWeight = "600" },
                H4 = new H4Typography { FontFamily = new[] { "Fraunces", "Georgia", "serif" }, FontWeight = "600" },
                H5 = new H5Typography { FontFamily = new[] { "Fraunces", "Georgia", "serif" }, FontWeight = "600" },
                H6 = new H6Typography { FontFamily = new[] { "DM Sans", "system-ui", "sans-serif" }, FontWeight = "600" },
                Button = new ButtonTypography { FontFamily = new[] { "DM Sans", "system-ui", "sans-serif" }, FontWeight = "600", TextTransform = "none" }
            };
        }
    }
}