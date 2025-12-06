using Microsoft.JSInterop;
using Supabase;
using Supabase.Gotrue;
using StorhaugenWebsite.Models;
using Client = Supabase.Client;
using static Supabase.Gotrue.Constants;

namespace StorhaugenWebsite.Services;

public class SupabaseAuthService : IAuthService, IAsyncDisposable
{
    private readonly Client _supabaseClient;
    private readonly IJSRuntime _jsRuntime;
    private Session? _session;

    public event Action? OnAuthStateChanged;

    public bool IsAuthenticated => _session?.User != null;
    public bool IsAuthorized => IsAuthenticated && AppConfig.IsAllowedEmail(CurrentUserEmail);
    public string? CurrentUserEmail => _session?.User?.Email;
    public FamilyMember? CurrentUser => AppConfig.GetMemberByEmail(CurrentUserEmail);

    public SupabaseAuthService(Client supabaseClient, IJSRuntime jsRuntime)
    {
        _supabaseClient = supabaseClient;
        _jsRuntime = jsRuntime;

        // Subscribe to auth state changes
        _supabaseClient.Auth.AddStateChangedListener(OnAuthStateChange);
    }

    private void OnAuthStateChange(object? sender, Constants.AuthState state)
    {
        _session = _supabaseClient.Auth.CurrentSession;
        OnAuthStateChanged?.Invoke();
    }

    public async Task InitializeAsync()
    {
        try
        {
            // Check if user is already logged in
            var session = await _supabaseClient.Auth.RetrieveSessionAsync();
            if (session != null)
            {
                _session = session;
                OnAuthStateChanged?.Invoke();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Auth initialization error: {ex.Message}");
            // User not logged in, that's okay
        }
    }

    public async Task<(bool success, string? errorMessage)> LoginAsync()
    {
        try
        {
            // Get redirect URL dynamically from browser
            var redirectUrl = await GetRedirectUrlAsync();

            // Sign in with Google OAuth
            var options = new SignInOptions
            {
                RedirectTo = redirectUrl
            };

            var result = await _supabaseClient.Auth.SignIn(Provider.Google, options);

            if (result != null && !string.IsNullOrEmpty(result))
            {
                // Redirect to Google OAuth - the page will reload after auth
                await _jsRuntime.InvokeVoidAsync("open", result, "_self");
                return (true, null);
            }

            return (false, "Login was cancelled or failed.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Login error: {ex.Message}");
            return (false, $"Login error: {ex.Message}");
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            await _supabaseClient.Auth.SignOut();
            _session = null;
            OnAuthStateChanged?.Invoke();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Logout error: {ex.Message}");
        }
    }

    public Task<string?> GetAccessTokenAsync()
    {
        return Task.FromResult(_session?.AccessToken);
    }

    private async Task<string> GetRedirectUrlAsync()
    {
        // Get current URL from browser dynamically - works for both local and production
        var origin = await _jsRuntime.InvokeAsync<string>("eval", "window.location.origin");
        return $"{origin}/"; // Redirect to home page after auth
    }

    public async ValueTask DisposeAsync()
    {
        _supabaseClient.Auth.RemoveStateChangedListener(OnAuthStateChange);
        await Task.CompletedTask;
    }
}
