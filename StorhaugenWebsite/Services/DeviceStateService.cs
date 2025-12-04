using Microsoft.JSInterop;
using StorhaugenWebsite.Models;

namespace StorhaugenWebsite.Services
{
    public class DeviceStateService : IDeviceStateService
    {
        private readonly IJSRuntime _jsRuntime;
        private const string STORAGE_KEY = "storhaugen_settings";

        public DeviceSettings Settings { get; private set; } = new();
        public event Action? OnSettingsChanged;

        public DeviceStateService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public async Task InitializeAsync()
        {
            try
            {
                var json = await _jsRuntime.InvokeAsync<string?>("deviceState.get", STORAGE_KEY);
                if (!string.IsNullOrEmpty(json))
                {
                    var settings = System.Text.Json.JsonSerializer.Deserialize<DeviceSettings>(json);
                    if (settings != null)
                    {
                        Settings = settings;
                    }
                }
            }
            catch
            {
                // Use defaults if localStorage fails
            }
        }

        public async Task SetThemeAsync(string theme)
        {
            Settings.Theme = theme;
            await SaveSettingsAsync();
            OnSettingsChanged?.Invoke();
        }

        public async Task SetViewModeAsync(string viewMode)
        {
            Settings.ViewMode = viewMode;
            await SaveSettingsAsync();
            OnSettingsChanged?.Invoke();
        }

        public async Task<string> GetSystemThemePreferenceAsync()
        {
            try
            {
                return await _jsRuntime.InvokeAsync<string>("deviceState.getPreferredTheme");
            }
            catch
            {
                return "light";
            }
        }

        private async Task SaveSettingsAsync()
        {
            try
            {
                var json = System.Text.Json.JsonSerializer.Serialize(Settings);
                await _jsRuntime.InvokeVoidAsync("deviceState.set", STORAGE_KEY, json);
            }
            catch
            {
                // Silently fail if localStorage is not available
            }
        }
    }
}