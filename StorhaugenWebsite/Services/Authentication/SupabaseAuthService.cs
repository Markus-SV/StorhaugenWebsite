using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Supabase;
using Supabase.Gotrue;
using System.IdentityModel.Tokens.Jwt;
using static Supabase.Gotrue.Constants;
using Client = Supabase.Client;

namespace StorhaugenWebsite.Services;

public class SupabaseAuthService : IAuthService, IAsyncDisposable
{
    private readonly Client _supabaseClient;
    private readonly IJSRuntime _jsRuntime;
    private readonly NavigationManager _navigationManager;
    private Session? _session;
    private const string AuthCacheKey = "supa_auth_session";

    public event Action? OnAuthStateChanged;

    public bool IsAuthenticated => _session?.User != null;
    public bool IsAuthorized => IsAuthenticated; // Any authenticated user is authorized

    private string? _cachedDisplayName; 
    public string? CurrentUserEmail => _session?.User?.Email;
    public string? CurrentUserName => !string.IsNullOrEmpty(_cachedDisplayName)
        ? _cachedDisplayName
        : GetUserNameFromEmail(CurrentUserEmail);

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

        // Prevent refresh storms (TokenRefreshed / SetSession should not cause the whole app to refetch)
        if (state is Constants.AuthState.SignedIn or Constants.AuthState.SignedOut)
            OnAuthStateChanged?.Invoke();
    }


    public void UpdateCachedDisplayName(string? displayName)
    {
        _cachedDisplayName = displayName;
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

    public async Task<(bool success, string? errorMessage)> SignInWithEmailAsync(string email, string password)
    {
        try
        {
            var session = await _supabaseClient.Auth.SignIn(email, password);

            if (session != null && session.User != null)
            {
                _session = session;
                OnAuthStateChanged?.Invoke();
                return (true, null);
            }
            return (false, "Invalid credentials");
        }
        catch (Exception ex)
        {
            // Supabase throws specific exceptions for invalid login
            return (false, ex.Message);
        }
    }

    public async Task<(bool success, string? errorMessage)> SignUpWithEmailAsync(string email, string password, string displayName)
    {
        try
        {
            var options = new SignUpOptions
            {
                Data = new Dictionary<string, object>
                {
                    { "name", displayName }, // Pass display name to Supabase metadata
                    { "full_name", displayName } // Some providers look for full_name
                }
            };

            var session = await _supabaseClient.Auth.SignUp(email, password, options);

            // If Supabase is set to "Auto Confirm Emails", you get a session immediately.
            // If "Confirm Email" is on, session might be null, but User is not.
            if (session?.User != null)
            {
                if (session.AccessToken != null)
                {
                    _session = session;
                    OnAuthStateChanged?.Invoke();
                    return (true, null);
                }
                else
                {
                    return (true, "Registration successful! Please check your email to confirm your account.");
                }
            }

            return (false, "Registration failed.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
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

    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public async Task<string?> GetAccessTokenAsync()
    {
        _session ??= _supabaseClient.Auth.CurrentSession
                 ?? await _supabaseClient.Auth.RetrieveSessionAsync();

        if (_session?.AccessToken is null)
            return null;

        // Refresh earlier than 60s if your backend uses strict lifetime validation (yours does)
        if (!IsExpiredOrNearExpiry(_session.AccessToken, skewSeconds: 180))
            return _session.AccessToken;

        await _refreshLock.WaitAsync();
        try
        {
            // Re-check after waiting: another caller may have refreshed already
            _session = _supabaseClient.Auth.CurrentSession ?? _session;
            if (_session?.AccessToken is null)
                return null;

            if (!IsExpiredOrNearExpiry(_session.AccessToken, skewSeconds: 180))
                return _session.AccessToken;

            //  Do an actual refresh (method name may be RefreshSession / RefreshSessionAsync depending on your SDK)
            var refreshed = await _supabaseClient.Auth.RefreshSession();
            if (refreshed != null)
                _session = refreshed;
            else
                _session = _supabaseClient.Auth.CurrentSession;

            return _session?.AccessToken;
        }
        finally
        {
            _refreshLock.Release();
        }
    }


    private static bool IsExpiredOrNearExpiry(string jwt, int skewSeconds)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var token = handler.ReadJwtToken(jwt);
            return token.ValidTo <= DateTime.UtcNow.AddSeconds(skewSeconds);
        }
        catch
        {
            // If we can't parse it, treat it as invalid and force renewal path.
            return true;
        }
    }

    private Task<string> GetRedirectUrlAsync()
    {
        // Ensure we redirect specifically to the login page so the 
        // OnInitializedAsync method in Login.razor actually runs to parse the token.
        var baseUri = _navigationManager.BaseUri;

        // Handle trailing slash just in case
        var redirectUrl = baseUri.EndsWith("/")
            ? $"{baseUri}login"
            : $"{baseUri}/login";

        return Task.FromResult(redirectUrl);
    }

    public async ValueTask DisposeAsync()
    {
        _supabaseClient.Auth.RemoveStateChangedListener(OnAuthStateChange);
        await Task.CompletedTask;
    }

    private static string? GetUserNameFromEmail(string? email)
    {
        if (string.IsNullOrEmpty(email))
            return null;

        // Extract name from email (part before @)
        var atIndex = email.IndexOf('@');
        if (atIndex <= 0)
            return email;

        var namePart = email.Substring(0, atIndex);

        // Capitalize first letter
        if (namePart.Length > 0)
        {
            return char.ToUpper(namePart[0]) + namePart.Substring(1).ToLower();
        }

        return namePart;
    }
}
