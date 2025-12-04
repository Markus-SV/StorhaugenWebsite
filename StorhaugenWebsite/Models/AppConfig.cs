namespace StorhaugenWebsite.Models
{
    public static class AppConfig
    {
        public static readonly List<FamilyMember> AllowedUsers = new()
        {
            new FamilyMember { Name = "Markus", Email = "markussvenoy@gmail.com", AvatarColor = "#6366f1" },
            //new FamilyMember { Name = "Siv", Email = "siv@example.com", AvatarColor = "#ec4899" },  // Update with real email
            //new FamilyMember { Name = "Elias", Email = "elias@example.com", AvatarColor = "#10b981" }  // Update with real email
        };

        public static readonly List<string> FamilyNames = new() { "Markus", "Siv", "Elias" };

        public static bool IsAllowedEmail(string? email)
        {
            if (string.IsNullOrEmpty(email)) return false;
            return AllowedUsers.Any(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        }

        public static FamilyMember? GetMemberByEmail(string? email)
        {
            if (string.IsNullOrEmpty(email)) return null;
            return AllowedUsers.FirstOrDefault(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));
        }
    }
}