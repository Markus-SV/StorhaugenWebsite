using StorhaugenWebsite.Models;

namespace StorhaugenWebsite.Services
{
    public interface IDeviceStateService
    {
        DeviceSettings Settings { get; }
        event Action? OnSettingsChanged;

        Task InitializeAsync();
        Task SetThemeAsync(string theme);
        Task SetViewModeAsync(string viewMode);
        Task<string> GetSystemThemePreferenceAsync();
        Task SetSortAsync(string sortBy, bool descending);
        Task SetCookbookFiltersAsync(List<Guid> collectionIds, bool personalActive);
    }
}