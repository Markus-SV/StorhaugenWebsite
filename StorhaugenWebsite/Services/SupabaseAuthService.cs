using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using StorhaugenWebsite.Models;
using Supabase;
using Supabase.Gotrue;
using static Supabase.Gotrue.Constants;
using Client = Supabase.Client;

namespace StorhaugenWebsite.Services;

public class SupabaseAuthService : IAuthService, IAsyncDisposable
{
    private readonly Client _supabaseClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly NavigationManager _navigationManager; // Add this
    private Session? _session;

    public event Action? OnAuthStateChanged;

    public bool IsAuthenticated => _session?.User != null;
    public bool IsAuthorized => IsAuthenticated && AppConfig.IsAllowedEmail(CurrentUserEmail);
    public string? CurrentUserEmail => _session?.User?.Email;
    public FamilyMember? CurrentUser => AppConfig.GetMemberByEmail(CurrentUserEmail);

    // Inject NavigationManager here
    public SupabaseAuthService(Client supabaseClient, IJSRuntime jsRuntime, NavigationManager navigationManager)
    {
        _supabaseClient = supabaseClient;
        _jsRuntime = jsRuntime;
        _navigationManager = navigationManager;

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
            // 1. Check if we are coming back from a login redirect (URL contains access_token)
            var uri = _navigationManager.Uri;
            if (uri.Contains("access_token") && uri.Contains("type=recovery") == false)
            {
                // Parse the session from the URL
                var session = await _supabaseClient.Auth.GetSessionFromUrl(new Uri(uri));

                if (session != null)
                {
                    _session = session;
                    OnAuthStateChanged?.Invoke();

                    // Optional: Clean the URL so the user doesn't see the ugly token
                    // _navigationManager.NavigateTo("/", replace: true); 
                    return;
                }
            }

            // 2. If no token in URL, check LocalStorage for existing session
            var storedSession = await _supabaseClient.Auth.RetrieveSessionAsync();
            if (storedSession != null)
            {
                _session = storedSession;
                OnAuthStateChanged?.Invoke();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Auth initialization error: {ex.Message}");
        }
    }

    public async Task<(bool success, string? errorMessage)> LoginAsync()
    {
        try
        {
            var redirectUrl = await GetRedirectUrlAsync();

            var options = new SignInOptions
            {
                RedirectTo = redirectUrl
            };

            var result = await _supabaseClient.Auth.SignIn(Provider.Google, options);

            if (result != null && result.Uri != null)
            {
                // 1. We are leaving the app. Using NavigationManager with forceLoad: true 
                // is cleaner than JS Interop for external links.
                _navigationManager.NavigateTo(result.Uri.ToString(), forceLoad: true);

                // 2. We return basic success here, but the code below 
                // technically won't matter because the browser is navigating away.
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
