namespace StorhaugenEats.API.Services;

public interface IHelloFreshScraperService
{
    Task<(int added, int updated)> SyncRecipesAsync();
    Task<string> GetBuildIdAsync();
    Task<bool> ShouldRunSyncAsync();
}
