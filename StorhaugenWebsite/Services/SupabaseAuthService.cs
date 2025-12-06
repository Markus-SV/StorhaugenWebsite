using Microsoft.JSInterop;
using StorhaugenWebsite.Models;
using Supabase;
using Supabase.Gotrue;

namespace StorhaugenWebsite.Services
{
    public class SupabaseAuthService : IAuthService
    {
        private readonly Client _supabaseClient;
        private readonly IJSRuntime _jsRuntime;

        public event Action? OnAuthStateChanged;

        public bool IsAuthenticated => !string.IsNullOrEmpty(CurrentUserEmail);
        public bool IsAuthorized => IsAuthenticated && AppConfig.IsAllowedEmail(CurrentUserEmail);
        public string? CurrentUserEmail { get; private set; }
        public FamilyMember? CurrentUser => AppConfig.GetMemberByEmail(CurrentUserEmail);

        public SupabaseAuthService(Client supabaseClient, IJSRuntime jsRuntime)
        {
            _supabaseClient = supabaseClient;
            _jsRuntime = jsRuntime;

            // Listen for auth state changes
            _supabaseClient.Auth.AddStateChangedListener(AuthStateChanged);
        }

        private void AuthStateChanged(object? sender, Supabase.Gotrue.Constants.AuthState state)
        {
            var session = _supabaseClient.Auth.CurrentSession;
            CurrentUserEmail = session?.User?.Email;
            OnAuthStateChanged?.Invoke();
        }

        public async Task InitializeAsync()
        {
            try
            {
                // Check if we have a session from URL (OAuth callback)
                await _supabaseClient.Auth.RetrieveSessionAsync();

                var session = _supabaseClient.Auth.CurrentSession;
                if (session?.User != null)
                {
                    CurrentUserEmail = session.User.Email;
                    OnAuthStateChanged?.Invoke();
                }
            }
            catch
            {
                // User not logged in, that's okay
                CurrentUserEmail = null;
            }
        }

        public async Task<(bool success, string? errorMessage)> LoginAsync()
        {
            try
            {
                // Get the current URL for redirect
                var currentUrl = await _jsRuntime.InvokeAsync<string>("eval", "window.location.origin");
                var redirectUrl = $"{currentUrl}/";

                // Sign in with Google OAuth
                var signInUrl = await _supabaseClient.Auth.SignIn(
                    Supabase.Gotrue.Constants.Provider.Google,
                    new Supabase.Gotrue.SignInOptions
                    {
                        RedirectTo = redirectUrl
                    }
                );

                if (!string.IsNullOrEmpty(signInUrl))
                {
                    // Redirect to Google OAuth consent screen
                    await _jsRuntime.InvokeVoidAsync("open", signInUrl, "_self");
                    return (true, null);
                }

                return (false, "Failed to initiate Google sign-in");
            }
            catch (Exception ex)
            {
                return (false, $"Login error: {ex.Message}");
            }
        }

        public async Task LogoutAsync()
        {
            try
            {
                await _supabaseClient.Auth.SignOut();
                CurrentUserEmail = null;
                OnAuthStateChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Logout error: {ex.Message}");
            }
        }
    }
}
