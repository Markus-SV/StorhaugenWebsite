# Storhaugen Eats - Multi-Tenant Refactor

This document guides you through the complete refactor from a single-family app to a multi-tenant social meal platform.

## 📋 What's Been Built

### ✅ Backend API (Complete)

A full-featured ASP.NET Core Web API with:

**Database Architecture:**
- PostgreSQL schema with proper relationships and constraints
- Row-Level Security (RLS) for multi-tenancy
- Automatic triggers for rating aggregation
- Database views for optimized queries

**API Endpoints:**
- 👤 **Users API** - User management, profile, share IDs
- 🏠 **Households API** - Create, join, merge households
- 🌍 **Global Recipes API** - Browse public HelloFresh & user recipes
- 📝 **Household Recipes API** - Manage household list (linked/forked)
- ⭐ **Ratings API** - Rate global recipes (1-10 scale)
- 🔄 **HelloFresh Sync API** - ETL scraper for HelloFresh recipes

**Key Features Implemented:**
- ✅ Reference/Fork logic (linked vs forked recipes)
- ✅ Household merging (atomic transactions)
- ✅ Global rating aggregation (auto-calculated)
- ✅ Image re-hosting to Supabase Storage
- ✅ HelloFresh scraper with week-based fetching
- ✅ JWT authentication (Supabase)
- ✅ CORS for Blazor WASM

### 📁 Project Structure

```
StorhaugenWebsite/
├── database/
│   ├── schema.sql              # Complete PostgreSQL schema
│   ├── SUPABASE_SETUP.md      # Step-by-step Supabase setup
│   └── supabase-config.env     # Config template (add to .gitignore)
│
└── StorhaugenEats.API/
    ├── Controllers/            # REST API endpoints
    │   ├── UsersController.cs
    │   ├── HouseholdsController.cs
    │   ├── GlobalRecipesController.cs
    │   ├── HouseholdRecipesController.cs
    │   ├── RatingsController.cs
    │   └── HelloFreshController.cs
    │
    ├── Services/               # Business logic
    │   ├── UserService.cs
    │   ├── HouseholdService.cs
    │   ├── GlobalRecipeService.cs
    │   ├── HouseholdRecipeService.cs
    │   ├── RatingService.cs
    │   ├── SupabaseStorageService.cs
    │   └── HelloFreshScraperService.cs
    │
    ├── Models/                 # Data models
    │   ├── User.cs
    │   ├── Household.cs
    │   ├── GlobalRecipe.cs
    │   ├── HouseholdRecipe.cs
    │   ├── Rating.cs
    │   ├── HouseholdInvite.cs
    │   └── EtlSyncLog.cs
    │
    ├── Data/
    │   └── AppDbContext.cs     # EF Core DbContext
    │
    ├── Program.cs              # API startup & DI configuration
    ├── appsettings.json        # Configuration template
    └── StorhaugenEats.API.csproj
```

---

## 🚀 Getting Started

### Step 1: Set Up Supabase

Follow the detailed guide in `database/SUPABASE_SETUP.md`:

1. Create Supabase project
2. Run `database/schema.sql` in SQL Editor
3. Configure Google OAuth
4. Create `recipe-images` storage bucket
5. Copy API keys

**Quick Setup:**
```bash
# After creating Supabase project, run:
# 1. Go to SQL Editor in Supabase
# 2. Copy/paste contents of database/schema.sql
# 3. Click "Run"
```

### Step 2: Configure Backend API

1. **Update `appsettings.json`:**

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db.xxxxx.supabase.co;Port=5432;Database=postgres;Username=postgres;Password=YOUR_DB_PASSWORD"
  },
  "Supabase": {
    "Url": "https://xxxxx.supabase.co",
    "AnonKey": "YOUR_ANON_KEY",
    "ServiceRoleKey": "YOUR_SERVICE_ROLE_KEY",
    "JwtSecret": "YOUR_JWT_SECRET"
  },
  "HelloFresh": {
    "BaseUrl": "https://www.hellofresh.no",
    "SyncIntervalHours": 24,
    "WeeksToFetch": 4
  }
}
```

**Where to find these values:**
- Connection string: Supabase → Settings → Database → Connection string (URI mode)
- API keys: Supabase → Settings → API
- JWT Secret: Supabase → Settings → API → JWT Settings → JWT Secret

2. **Add to `.gitignore`:**

```
appsettings.Development.json
database/supabase-config.env
```

### Step 3: Run the Backend

```bash
# Navigate to API project
cd StorhaugenWebsite/StorhaugenEats.API

# Restore packages
dotnet restore

# Run the API
dotnet run

# API will start at https://localhost:5001 (or http://localhost:5000)
```

**Swagger UI:** https://localhost:5001/swagger

### Step 4: Test the API

**Health Check:**
```bash
curl https://localhost:5001/health
```

**Trigger HelloFresh Sync (requires auth):**
```bash
# First login via your Blazor app to get JWT token, then:
curl -X POST https://localhost:5001/api/hellofresh/sync?force=true \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

**Browse Public Recipes (no auth needed):**
```bash
curl https://localhost:5001/api/globalrecipes?skip=0&take=20&sortBy=rating
```

---

## 📖 API Documentation

### Authentication

All endpoints except public recipe browsing require JWT authentication.

**Headers:**
```
Authorization: Bearer <JWT_TOKEN_FROM_SUPABASE>
```

### Key Endpoints

#### Users
- `GET /api/users/me` - Get current user
- `POST /api/users` - Create user (called during signup)
- `GET /api/users/share/{shareId}` - Find user by share ID

#### Households
- `POST /api/households` - Create household
- `GET /api/households/{id}` - Get household details
- `POST /api/households/join` - Join household by share ID
- `POST /api/households/merge` - Merge two households (leader only)

#### Global Recipes (Browse Feed)
- `GET /api/globalrecipes?skip=0&take=50&sortBy=rating` - Browse public recipes
- `GET /api/globalrecipes/{id}` - Get recipe details
- `POST /api/globalrecipes` - Create user recipe

#### Household Recipes (My List)
- `GET /api/householdrecipes` - Get my household's recipes
- `POST /api/householdrecipes/link` - Add linked recipe
- `POST /api/householdrecipes/fork` - Add forked recipe
- `POST /api/householdrecipes/{id}/fork` - Fork a linked recipe
- `POST /api/householdrecipes/{id}/archive` - Archive recipe

#### Ratings
- `GET /api/ratings/recipe/{globalRecipeId}` - Get all ratings
- `GET /api/ratings/recipe/{globalRecipeId}/my-rating` - Get my rating
- `POST /api/ratings` - Create/update rating

#### HelloFresh Sync
- `POST /api/hellofresh/sync?force=true` - Trigger sync
- `GET /api/hellofresh/sync-status` - Get last sync status
- `GET /api/hellofresh/build-id` - Get current HelloFresh build ID

---

## 🎨 Frontend Integration (Next Steps)

### Phase 1: Update Authentication

**Current:** Firebase Auth
**New:** Supabase Auth

**Steps:**
1. Install Supabase client in Blazor:
   ```bash
   dotnet add package supabase-csharp
   ```

2. Update `Services/AuthService.cs` to use Supabase:
   ```csharp
   private readonly Supabase.Client _supabaseClient;

   public async Task<bool> SignInWithGoogleAsync()
   {
       await _supabaseClient.Auth.SignIn(Provider.Google);
       // On success, call API to create user record
       var session = _supabaseClient.Auth.CurrentSession;
       var jwt = session.AccessToken;

       // Store JWT for API calls
       await DeviceStateService.SetAsync("jwt_token", jwt);
   }
   ```

3. Add JWT to HTTP requests:
   ```csharp
   _httpClient.DefaultRequestHeaders.Authorization =
       new AuthenticationHeaderValue("Bearer", jwtToken);
   ```

### Phase 2: Create Household Management Pages

**New Pages Needed:**

1. **`Pages/Onboarding.razor`** - New user flow
   - "Create New Household" button
   - "Join Existing Household" button

2. **`Pages/HouseholdSettings.razor`**
   - Display household name & members
   - Show your share ID (for inviting others)
   - "Invite Member" form (enter their share ID)
   - "Leave Household" button
   - Leader-only: "Merge Household" feature

### Phase 3: Refactor Existing Pages

#### A. Home.razor (My List)

**Changes:**
```csharp
// OLD: Load from Firebase
var foodItems = await FirebaseBroker.GetAllFoodItems();

// NEW: Load from API
var response = await _httpClient.GetFromJsonAsync<List<HouseholdRecipe>>(
    "api/householdrecipes"
);

// Display with mode indicator
@foreach (var recipe in recipes)
{
    <MudCard>
        <MudCardMedia Image="@recipe.ImageUrl" />
        <MudCardContent>
            <MudText Typo="Typo.h6">@recipe.DisplayTitle</MudText>

            @if (recipe.RecipeMode == "linked")
            {
                <MudChip Size="Size.Small" Color="Color.Primary">HelloFresh</MudChip>

                @if (recipe.GlobalRecipe != null)
                {
                    <MudRating ReadOnly="true" SelectedValue="@((int)recipe.GlobalRecipe.AverageRating)" />
                    <MudText Typo="Typo.body2">@recipe.GlobalRecipe.RatingCount ratings</MudText>
                }
            }
            else
            {
                <MudChip Size="Size.Small" Color="Color.Secondary">Custom</MudChip>
            }

            @if (!string.IsNullOrEmpty(recipe.PersonalNotes))
            {
                <MudText Typo="Typo.body2" Color="Color.Info">
                    📝 @recipe.PersonalNotes
                </MudText>
            }
        </MudCardContent>
    </MudCard>
}
```

#### B. Create Browse.razor (New Page)

**Public Recipe Feed:**
```csharp
@page "/browse"

<MudContainer>
    <MudText Typo="Typo.h4">Browse Recipes</MudText>

    <MudSelect @bind-Value="sortBy" Label="Sort By">
        <MudSelectItem Value="@("rating")">Highest Rated</MudSelectItem>
        <MudSelectItem Value="@("date")">Newest</MudSelectItem>
        <MudSelectItem Value="@("title")">Alphabetical</MudSelectItem>
    </MudSelect>

    <MudGrid>
        @foreach (var recipe in globalRecipes)
        {
            <MudItem xs="12" sm="6" md="4">
                <MudCard>
                    <MudCardMedia Image="@recipe.ImageUrl" Height="200" />
                    <MudCardContent>
                        <MudText Typo="Typo.h6">@recipe.Title</MudText>
                        <MudRating ReadOnly="true" SelectedValue="@((int)recipe.AverageRating)" />
                        <MudText Typo="Typo.caption">@recipe.RatingCount ratings</MudText>

                        @if (recipe.IsHellofresh)
                        {
                            <MudChip Size="Size.Small" Color="Color.Primary">HelloFresh</MudChip>
                        }
                    </MudCardContent>
                    <MudCardActions>
                        <MudButton OnClick="@(() => AddToMyList(recipe.Id))">
                            Add to My List
                        </MudButton>
                    </MudCardActions>
                </MudCard>
            </MudItem>
        }
    </MudGrid>
</MudContainer>

@code {
    private List<GlobalRecipe> globalRecipes = new();
    private string sortBy = "rating";

    protected override async Task OnInitializedAsync()
    {
        await LoadRecipes();
    }

    private async Task LoadRecipes()
    {
        globalRecipes = await _httpClient.GetFromJsonAsync<List<GlobalRecipe>>(
            $"api/globalrecipes?skip=0&take=50&sortBy={sortBy}"
        ) ?? new();
    }

    private async Task AddToMyList(Guid globalRecipeId)
    {
        await _httpClient.PostAsJsonAsync("api/householdrecipes/link", new
        {
            GlobalRecipeId = globalRecipeId
        });

        Snackbar.Add("Added to your list!", Severity.Success);
    }
}
```

#### C. Update AddFood.razor

**Add option to make recipe public:**
```csharp
<MudSwitch @bind-Checked="makePublic" Label="Make this recipe public" />

// When saving:
if (makePublic)
{
    // Create as GlobalRecipe
    await _httpClient.PostAsJsonAsync("api/globalrecipes", new {
        Title = title,
        Description = description,
        Ingredients = ingredientsJson,
        ImageUrl = imageUrl,
        IsPublic = true
    });
}
else
{
    // Create as forked HouseholdRecipe
    await _httpClient.PostAsJsonAsync("api/householdrecipes/fork", new {
        Title = title,
        Description = description,
        Ingredients = ingredientsJson,
        ImageUrl = imageUrl
    });
}
```

### Phase 4: Update Navigation

**`Shared/NavMenu.razor`:**
```html
<MudNavLink Href="/browse" Icon="@Icons.Material.Filled.Explore">Browse</MudNavLink>
<MudNavLink Href="/" Icon="@Icons.Material.Filled.Home">My List</MudNavLink>
<MudNavLink Href="/addfood" Icon="@Icons.Material.Filled.Add">Add Recipe</MudNavLink>
<MudNavLink Href="/household" Icon="@Icons.Material.Filled.Group">Household</MudNavLink>
<MudNavLink Href="/settings" Icon="@Icons.Material.Filled.Settings">Settings</MudNavLink>
```

---

## 🔄 Deployment

### Backend Deployment Options

**Option 1: Azure App Service (Recommended for .NET)**
```bash
# Publish to Azure
dotnet publish -c Release
az webapp up --name storhaugen-eats-api --resource-group storhaugen-rg
```

**Option 2: Docker + any cloud**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY bin/Release/net8.0/publish .
ENTRYPOINT ["dotnet", "StorhaugenEats.API.dll"]
```

**Option 3: Railway.app / Render.com**
- Push to GitHub
- Connect repo to Railway/Render
- Set environment variables from appsettings.json

### Scheduled HelloFresh Sync

**Option A: Trigger from client (on login)**
```csharp
protected override async Task OnInitializedAsync()
{
    // Check if sync needed
    var response = await _httpClient.PostAsync("api/hellofresh/sync", null);
}
```

**Option B: Background service (in API)**
Add to `Program.cs`:
```csharp
builder.Services.AddHostedService<HelloFreshSyncBackgroundService>();
```

**Option C: External cron (recommended)**
Use a service like [cron-job.org](https://cron-job.org) to hit:
```
POST https://your-api.com/api/hellofresh/sync
```
Daily at 3 AM UTC.

---

## 🧪 Testing Checklist

### Backend API
- [ ] Can create user via POST /api/users
- [ ] Can create household
- [ ] Can join household by share ID
- [ ] Can add linked recipe from global recipes
- [ ] Can fork a recipe and edit it
- [ ] Can rate a global recipe
- [ ] Can browse public recipes
- [ ] HelloFresh sync completes successfully

### Frontend (After Integration)
- [ ] New user sees onboarding (create/join household)
- [ ] Can invite household members via share ID
- [ ] My List shows linked recipes with HelloFresh badge
- [ ] My List shows forked recipes with Custom badge
- [ ] Browse page shows HelloFresh + public recipes
- [ ] Can add recipe from Browse to My List
- [ ] Can fork a linked recipe and edit it
- [ ] Personal notes persist on linked recipes
- [ ] Can rate recipes (updates global average)

---

## 🐛 Troubleshooting

### "Cannot connect to database"
- Check connection string in appsettings.json
- Verify Supabase project is active
- Check database password

### "401 Unauthorized"
- Verify JWT token is valid (check Supabase dashboard)
- Check Authorization header format: `Bearer <token>`

### "HelloFresh sync fails"
- Test build ID endpoint: GET /api/hellofresh/build-id
- Check HelloFresh website structure hasn't changed
- Verify internet access from API server

### "Images not uploading"
- Check Supabase Storage bucket exists: `recipe-images`
- Verify bucket is public
- Check storage policies are created

---

## 📚 Next Steps

1. **Complete frontend integration** (see Phase 1-4 above)
2. **Add household merging UI** (Phase 5)
3. **Set up scheduled HelloFresh sync** (cron job or background service)
4. **Deploy to production** (Azure/Railway/Render)
5. **Test with real users** (invite family/friends)

---

## 🎯 Implementation Priority (Your Choice)

You specified: **1 → 3 → 2 → 4**

1. ✅ **Multi-household support** - DONE (backend complete)
2. ⏳ **Public browse/ratings** - DONE (backend) / TODO (frontend)
3. ⏳ **HelloFresh ETL** - DONE (backend) / TODO (schedule)
4. ⏳ **Household merging** - DONE (backend) / TODO (frontend UI)

---

## 💡 Tips

- Start with authentication migration first (most critical)
- Test API endpoints with Swagger before building frontend
- Use Postman/Insomnia to test complex flows
- Enable detailed logging during development
- Monitor Supabase usage (free tier limits)

---

## 📞 Support

If you encounter issues:
1. Check Swagger UI for API documentation
2. Review Supabase logs (Dashboard → Logs)
3. Check ASP.NET Core logs in console
4. Verify database schema matches models

**Happy coding! 🚀**
