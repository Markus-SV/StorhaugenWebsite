using StorhaugenWebsite.ApiClient;
using StorhaugenWebsite.Shared.DTOs;

namespace StorhaugenWebsite.Services;

/// <summary>
/// Manages group (household) state for the current user.
/// Supports multi-group filtering - user can be in multiple groups and filter which ones to view.
/// </summary>
public class HouseholdStateService : IHouseholdStateService
{
    private readonly IApiClient _apiClient;
    private readonly IAuthService _authService;

    public event Action? OnHouseholdChanged;
    public event Action? OnActiveGroupsChanged;

    public List<HouseholdDto> UserHouseholds { get; private set; } = new();
    public List<Guid> ActiveGroupFilters { get; private set; } = new();

    public List<HouseholdDto> ActiveGroups =>
        UserHouseholds.Where(h => ActiveGroupFilters.Contains(h.Id)).ToList();

    public bool HasHousehold => UserHouseholds.Any();
    public bool NeedsHouseholdSetup => _authService.IsAuthenticated && !HasHousehold;

    // [DEPRECATED] For backwards compatibility - returns first active group or first household
    public HouseholdDto? CurrentHousehold =>
        ActiveGroups.FirstOrDefault() ?? UserHouseholds.FirstOrDefault();

    private bool _isInitialized = false;
    private Task? _currentLoadingTask;

    public HouseholdStateService(IApiClient apiClient, IAuthService authService)
    {
        _apiClient = apiClient;
        _authService = authService;
        _authService.OnAuthStateChanged += OnAuthChanged;
    }

    private async void OnAuthChanged()
    {
        if (_authService.IsAuthenticated)
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }
        }
        else
        {
            // Reset state on logout
            UserHouseholds.Clear();
            ActiveGroupFilters.Clear();
            _isInitialized = false;
            OnHouseholdChanged?.Invoke();
            OnActiveGroupsChanged?.Invoke();
        }
    }

    public async Task InitializeAsync(bool forceRefresh = false)
    {
        if (!_authService.IsAuthenticated) return;

        if (_currentLoadingTask != null && !_currentLoadingTask.IsCompleted)
        {
            await _currentLoadingTask;
            return;
        }

        // Only skip if initialized AND not forced
        if (!forceRefresh && _isInitialized && UserHouseholds.Any()) return;

        _currentLoadingTask = LoadDataInternal();
        await _currentLoadingTask;
    }

    private async Task LoadDataInternal()
    {
        try
        {
            // Get user's households
            await RefreshHouseholdsAsync();

            // By default, activate ALL groups so user sees everything
            if (!ActiveGroupFilters.Any() && UserHouseholds.Any())
            {
                ActiveGroupFilters = UserHouseholds.Select(h => h.Id).ToList();
            }

            _isInitialized = true;
            OnHouseholdChanged?.Invoke();
            OnActiveGroupsChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing households: {ex.Message}");
        }
    }

    public async Task RefreshHouseholdsAsync()
    {
        if (!_authService.IsAuthenticated) return;

        try
        {
            UserHouseholds = await _apiClient.GetMyHouseholdsAsync();

            // Clean up ActiveGroupFilters - remove any groups user is no longer a member of
            ActiveGroupFilters = ActiveGroupFilters
                .Where(id => UserHouseholds.Any(h => h.Id == id))
                .ToList();

            // If no active filters and user has groups, activate all
            if (!ActiveGroupFilters.Any() && UserHouseholds.Any())
            {
                ActiveGroupFilters = UserHouseholds.Select(h => h.Id).ToList();
            }

            OnHouseholdChanged?.Invoke();
            OnActiveGroupsChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing households: {ex.Message}");
        }
    }

    public void ToggleGroupFilter(Guid groupId)
    {
        if (!UserHouseholds.Any(h => h.Id == groupId)) return;

        if (ActiveGroupFilters.Contains(groupId))
        {
            // Don't allow removing the last active group
            if (ActiveGroupFilters.Count > 1)
            {
                ActiveGroupFilters.Remove(groupId);
            }
        }
        else
        {
            ActiveGroupFilters.Add(groupId);
        }

        OnActiveGroupsChanged?.Invoke();
    }

    public void SelectAllGroups()
    {
        ActiveGroupFilters = UserHouseholds.Select(h => h.Id).ToList();
        OnActiveGroupsChanged?.Invoke();
    }

    public void ClearAllGroups()
    {
        // Keep at least one group active if available
        if (UserHouseholds.Any())
        {
            ActiveGroupFilters = new List<Guid> { UserHouseholds.First().Id };
        }
        else
        {
            ActiveGroupFilters.Clear();
        }
        OnActiveGroupsChanged?.Invoke();
    }

    public bool IsGroupActive(Guid groupId)
    {
        return ActiveGroupFilters.Contains(groupId);
    }

    // [DEPRECATED] For backwards compatibility
    public async Task SetCurrentHouseholdAsync(Guid householdId)
    {
        if (!_authService.IsAuthenticated) throw new UnauthorizedAccessException("User is not authenticated");
        try
        {
            await _apiClient.SwitchHouseholdAsync(householdId);
            await _apiClient.UpdateMyProfileAsync(new UpdateUserDto { CurrentHouseholdId = householdId });

            // Set only this group as active (backwards compatible behavior)
            ActiveGroupFilters = new List<Guid> { householdId };

            OnHouseholdChanged?.Invoke();
            OnActiveGroupsChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error switching household: {ex.Message}");
            throw;
        }
    }

    public async Task<HouseholdDto> CreateHouseholdAsync(string name)
    {
        if (!_authService.IsAuthenticated) throw new UnauthorizedAccessException("User is not authenticated");
        try
        {
            var household = await _apiClient.CreateHouseholdAsync(new CreateHouseholdDto { Name = name });
            await RefreshHouseholdsAsync();

            // Add the new group to active filters
            if (!ActiveGroupFilters.Contains(household.Id))
            {
                ActiveGroupFilters.Add(household.Id);
            }

            OnHouseholdChanged?.Invoke();
            OnActiveGroupsChanged?.Invoke();
            return household;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error creating household: {ex.Message}");
            throw;
        }
    }

    public async Task<List<HouseholdInviteDto>> GetPendingInvitesAsync()
    {
        if (!_authService.IsAuthenticated) return new List<HouseholdInviteDto>();
        try { return await _apiClient.GetPendingInvitesAsync(); }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting pending invites: {ex.Message}");
            return new List<HouseholdInviteDto>();
        }
    }

    public async Task AcceptInviteAsync(Guid inviteId)
    {
        if (!_authService.IsAuthenticated) throw new UnauthorizedAccessException("User is not authenticated");
        try
        {
            await _apiClient.AcceptInviteAsync(inviteId);
            await RefreshHouseholdsAsync();
            OnHouseholdChanged?.Invoke();
            OnActiveGroupsChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accepting invite: {ex.Message}");
            throw;
        }
    }
}