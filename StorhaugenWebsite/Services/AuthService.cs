//using StorhaugenWebsite.Brokers;
//using StorhaugenWebsite.Models;

//namespace StorhaugenWebsite.Services
//{
//    public class AuthService : IAuthService
//    {
//        private readonly IFirebaseBroker _firebaseBroker;

//        public event Action? OnAuthStateChanged;

//        public bool IsAuthenticated => !string.IsNullOrEmpty(CurrentUserEmail);
//        public bool IsAuthorized => IsAuthenticated && AppConfig.IsAllowedEmail(CurrentUserEmail);
//        public string? CurrentUserEmail { get; private set; }
//        public FamilyMember? CurrentUser => AppConfig.GetMemberByEmail(CurrentUserEmail);

//        public AuthService(IFirebaseBroker firebaseBroker)
//        {
//            _firebaseBroker = firebaseBroker;
//        }

//        public async Task InitializeAsync()
//        {
//            try
//            {
//                CurrentUserEmail = await _firebaseBroker.GetCurrentUserEmailAsync();
//                OnAuthStateChanged?.Invoke();
//            }
//            catch
//            {
//                // User not logged in, that's okay
//            }
//        }

//        public async Task<(bool success, string? errorMessage)> LoginAsync()
//        {
//            try
//            {
//                var email = await _firebaseBroker.LoginWithGoogleAsync();

//                if (string.IsNullOrEmpty(email))
//                {
//                    return (false, "Login was cancelled or failed.");
//                }

//                CurrentUserEmail = email;
//                OnAuthStateChanged?.Invoke();

//                return (true, null);
//            }
//            catch (Exception ex)
//            {
//                return (false, $"Login error: {ex.Message}");
//            }
//        }

//        public async Task LogoutAsync()
//        {
//            await _firebaseBroker.SignOutAsync();
//            CurrentUserEmail = null;
//            OnAuthStateChanged?.Invoke();
//        }
//    }
//}