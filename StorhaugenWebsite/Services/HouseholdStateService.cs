using StorhaugenWebsite.ApiClient;
using StorhaugenWebsite.DTOs;

namespace StorhaugenWebsite.Services;

public class HouseholdStateService : IHouseholdStateService
{
    private readonly IApiClient _apiClient;
    private readonly IAuthService _authService;

    public event Action? OnHouseholdChanged;

    public HouseholdDto? CurrentHousehold { get; private set; }
    public List<HouseholdDto> UserHouseholds { get; private set; } = new();
    public bool HasHousehold => CurrentHousehold != null;
    public bool NeedsHouseholdSetup => _authService.IsAuthenticated && !HasHousehold && UserHouseholds.Count == 0;

    public HouseholdStateService(IApiClient apiClient, IAuthService authService)
    {
        _apiClient = apiClient;
        _authService = authService;

        // Subscribe to auth changes
        _authService.OnAuthStateChanged += OnAuthChanged;
    }

    private async void OnAuthChanged()
    {
        if (_authService.IsAuthenticated)
        {
            await InitializeAsync();
        }
        else
        {
            CurrentHousehold = null;
            UserHouseholds.Clear();
            OnHouseholdChanged?.Invoke();
        }
    }

    public async Task InitializeAsync()
    {
        if (!_authService.IsAuthenticated)
            return;

        try
        {
            // Get user's households
            await RefreshHouseholdsAsync();

            // Get user profile to find current household
            var user = await _apiClient.GetMyProfileAsync();
            if (user?.CurrentHouseholdId.HasValue == true)
            {
                CurrentHousehold = UserHouseholds.FirstOrDefault(h => h.Id == user.CurrentHouseholdId.Value);
            }
            else if (UserHouseholds.Count == 1)
            {
                // Auto-select if only one household
                await SetCurrentHouseholdAsync(UserHouseholds[0].Id);
            }

            OnHouseholdChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error initializing households: {ex.Message}");
        }
    }

    public async Task RefreshHouseholdsAsync()
    {
        if (!_authService.IsAuthenticated)
            return;

        try
        {
            UserHouseholds = await _apiClient.GetMyHouseholdsAsync();

            // Clear CurrentHousehold if it's no longer in the user's households
            if (CurrentHousehold != null && !UserHouseholds.Any(h => h.Id == CurrentHousehold.Id))
            {
                CurrentHousehold = null;
            }

            // Auto-select if only one household and none selected
            if (CurrentHousehold == null && UserHouseholds.Count == 1)
            {
                CurrentHousehold = UserHouseholds[0];
            }

            OnHouseholdChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error refreshing households: {ex.Message}");
        }
    }

    public async Task SetCurrentHouseholdAsync(Guid householdId)
    {
        if (!_authService.IsAuthenticated)
            throw new UnauthorizedAccessException("User is not authenticated");

        try
        {
            await _apiClient.SwitchHouseholdAsync(householdId);
            CurrentHousehold = UserHouseholds.FirstOrDefault(h => h.Id == householdId);

            // Also update user profile
            await _apiClient.UpdateMyProfileAsync(new UpdateUserDto
            {
                CurrentHouseholdId = householdId
            });

            OnHouseholdChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error switching household: {ex.Message}");
            throw;
        }
    }

    public async Task<HouseholdDto> CreateHouseholdAsync(string name)
    {
        if (!_authService.IsAuthenticated)
            throw new UnauthorizedAccessException("User is not authenticated");

        try
        {
            var household = await _apiClient.CreateHouseholdAsync(new CreateHouseholdDto { Name = name });
            await RefreshHouseholdsAsync();
            // Backend already sets CurrentHouseholdId, just update local state
            CurrentHousehold = UserHouseholds.FirstOrDefault(h => h.Id == household.Id);
            OnHouseholdChanged?.Invoke();
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
        if (!_authService.IsAuthenticated)
            return new List<HouseholdInviteDto>();

        try
        {
            return await _apiClient.GetPendingInvitesAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error getting pending invites: {ex.Message}");
            return new List<HouseholdInviteDto>();
        }
    }

    public async Task AcceptInviteAsync(Guid inviteId)
    {
        if (!_authService.IsAuthenticated)
            throw new UnauthorizedAccessException("User is not authenticated");

        try
        {
            await _apiClient.AcceptInviteAsync(inviteId);
            await RefreshHouseholdsAsync();
            OnHouseholdChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accepting invite: {ex.Message}");
            throw;
        }
    }
}
