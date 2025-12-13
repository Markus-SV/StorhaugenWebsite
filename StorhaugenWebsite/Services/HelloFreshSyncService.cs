using StorhaugenWebsite.ApiClient;

namespace StorhaugenWebsite.Services;

public interface IHelloFreshSyncService
{
    Task TryTriggerSyncAsync();
    bool HasCheckedThisSession { get; }
}

/// <summary>
/// Service that handles automatic HelloFresh sync triggering.
/// Attempts to sync once per session if the server-side check determines it's needed.
/// </summary>
public class HelloFreshSyncService : IHelloFreshSyncService
{
    private readonly IApiClient _apiClient;
    private readonly ILogger<HelloFreshSyncService> _logger;
    private bool _hasCheckedThisSession = false;
    private readonly SemaphoreSlim _syncLock = new(1, 1);

    public bool HasCheckedThisSession => _hasCheckedThisSession;

    public HelloFreshSyncService(IApiClient apiClient, ILogger<HelloFreshSyncService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    /// <summary>
    /// Attempts to trigger a HelloFresh sync if needed.
    /// This is a fire-and-forget operation that runs in the background.
    /// Only runs once per session to avoid repeated API calls.
    /// </summary>
    public async Task TryTriggerSyncAsync()
    {
        // Only check once per session
        if (_hasCheckedThisSession)
            return;

        // Use a lock to prevent multiple simultaneous sync attempts
        if (!await _syncLock.WaitAsync(0))
            return;

        try
        {
            _hasCheckedThisSession = true;

            _logger.LogInformation("Checking if HelloFresh sync is needed...");

            // Call the sync endpoint without force - the server will check if sync is needed
            var result = await _apiClient.TriggerHelloFreshSyncAsync(force: false);

            if (result.RecipesAdded > 0 || result.RecipesUpdated > 0)
            {
                _logger.LogInformation(
                    "HelloFresh sync completed: {Added} added, {Updated} updated",
                    result.RecipesAdded, result.RecipesUpdated);
            }
            else
            {
                _logger.LogInformation("HelloFresh sync: {Message}", result.Message);
            }
        }
        catch (Exception ex)
        {
            // Log but don't throw - this is a background operation
            _logger.LogWarning(ex, "HelloFresh sync check failed (non-critical)");
        }
        finally
        {
            _syncLock.Release();
        }
    }
}
