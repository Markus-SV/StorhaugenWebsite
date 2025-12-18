namespace StorhaugenEats.API.Services;

public interface IHelloFreshScraperService
{
    Task<(int added, int updated)> SyncRecipesAsync();
    Task<(int added, int updated)> SyncWeekAsync(string week);
    Task<string> GetBuildIdAsync();
    Task<bool> ShouldRunSyncAsync();
    List<string> GenerateAvailableWeeks(int count = 8);
}
