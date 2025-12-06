# Multi-Tenant Food App - Implementation Progress

## ✅ Completed (Backend API - Core Functionality)

### 1. Database Schema & Models ✅
- PostgreSQL schema with all tables (users, households, global_recipes, household_recipes, ratings, invites)
- Entity Framework Core models with proper relationships
- Row-Level Security (RLS) policies in Supabase

### 2. Authentication & Authorization ✅
- **Supabase JWT validation** configured in Program.cs
- **CurrentUserService** - Extracts user from JWT and auto-creates in database
- **HTTP Context Accessor** for user identity throughout API
- Proper issuer validation (`/auth/v1` endpoint)

### 3. DTOs (Data Transfer Objects) ✅
Created comprehensive DTOs for all operations:
- `UserDTOs.cs` - User profile management
- `HouseholdDTOs.cs` - Household, members, invites
- `HouseholdRecipeDTOs.cs` - Recipe CRUD with ratings
- `GlobalRecipeDTOs.cs` - Browse/search global recipes
- `StorageDTOs.cs` - Image upload/delete

### 4. Households Controller ✅
**Endpoint:** `/api/households`

Implemented full household management:
- `GET /api/households/my` - List user's households
- `GET /api/households/{id}` - Get household details
- `POST /api/households` - Create new household (auto-join as first member)
- `PUT /api/households/{id}` - Update household name
- `POST /api/households/{id}/invites` - Send invitation by email
- `GET /api/households/invites/pending` - Check pending invites
- `POST /api/households/invites/{id}/accept` - Accept invitation
- `POST /api/households/invites/{id}/reject` - Reject invitation
- `POST /api/households/{id}/switch` - Switch active household
- `POST /api/households/{id}/leave` - Leave household

**Key Features:**
- Email-based invitations
- Multi-household support (users can belong to multiple households)
- Current household context (user.CurrentHouseholdId)
- Creator verification for updates

### 5. Household Recipes Controller ✅
**Endpoint:** `/api/household-recipes`

Replaced old Firebase FoodItems with multi-tenant recipe system:
- `GET /api/household-recipes` - List household recipes (with archive filter)
- `GET /api/household-recipes/{id}` - Get recipe details
- `POST /api/household-recipes` - Create custom or add from global
- `PUT /api/household-recipes/{id}` - Update recipe (respects fork status)
- `POST /api/household-recipes/{id}/rate` - Rate recipe (0-10 scale)
- `POST /api/household-recipes/{id}/archive` - Archive recipe
- `POST /api/household-recipes/{id}/restore` - Restore archived recipe
- `POST /api/household-recipes/{id}/fork` - Fork linked recipe to editable copy
- `DELETE /api/household-recipes/{id}` - Delete recipe permanently

**Key Features:**
- **Reference vs Fork System:**
  - Link: References global recipe, only stores personal notes
  - Fork: Copies global recipe data, becomes editable
- **Multi-user Ratings:** Aggregated by household member (Markus, Siv, Elias)
- **Global Rating Updates:** When household recipe is rated, global average updates
- **Automatic Context:** Uses CurrentUserService for user/household identification
- **Smart DTO Mapping:** Falls back to global recipe data for linked recipes

### 6. Database Connection ✅
- **IPv4/IPv6 Resilience:** Multiple connection string options (transaction pooler, session pooler, direct)
- **Connection Testing:** `/test-connections` endpoint to diagnose connectivity
- **TCP Keepalive:** Configured for stable connections
- **Documentation:** DATABASE_CONNECTION_TROUBLESHOOTING.md

### 7. HelloFresh Scraper ✅
- Robust type handling for HelloFresh API inconsistencies
- Batch operations to prevent N+1 query issues
- Safe JSON parsing (handles both string and number types)

---

## ⏳ Remaining Backend Tasks

### 1. Global Recipes Controller (Browse/Search)
**Endpoint:** `/api/global-recipes`

**Needed Endpoints:**
```
GET /api/global-recipes - Browse with filters (cuisine, difficulty, prep time, tags)
GET /api/global-recipes/{id} - Get global recipe details
POST /api/global-recipes - Create user-contributed global recipe
GET /api/global-recipes/hellofresh - Browse only HelloFresh recipes
GET /api/global-recipes/search?q=chicken - Search by name/description
```

**Features to Implement:**
- Pagination (page, pageSize)
- Sorting (popular, newest, rating, name)
- Filtering (cuisine, difficulty, max prep time, tags, HelloFresh-only)
- Full-text search on name/description
- Return aggregated ratings and times added

### 2. Storage Controller (Image Upload/Delete)
**Endpoint:** `/api/storage`

**Needed Endpoints:**
```
POST /api/storage/upload - Upload image to Supabase Storage
DELETE /api/storage/{fileName} - Delete image from storage
```

**Features to Implement:**
- Base64 image upload
- File size limits (e.g., 5MB max)
- Image format validation (JPEG, PNG, WebP)
- Generate public URL for uploaded images
- Integrate with SupabaseStorageService (already exists)

### 3. Users Controller (Profile Management)
**Endpoint:** `/api/users`

**Needed Endpoints:**
```
GET /api/users/me - Get current user profile
PUT /api/users/me - Update display name, avatar
GET /api/users/{id} - Get user public profile (for household members)
```

**Nice to Have:**
- Upload avatar via Storage endpoint
- Email preferences
- Dietary restrictions

### 4. Fix HelloFresh Scraper Loop Issue
**Current Issue:** Scraper gets stuck in a loop during sync

**Investigation Needed:**
- Check if it's an infinite loop in week parsing
- Verify build ID fetching doesn't repeat
- Add better logging to identify loop location

**Workaround:** Can manually populate global_recipes table for now

---

## 🎨 Frontend Migration Tasks

### Phase 1: Setup & Authentication

#### 1. Install Supabase Client Library
```bash
cd StorhaugenWebsite
dotnet add package supabase-csharp
```

#### 2. Create ApiClient Service
Replace `FirebaseBroker.cs` with `ApiClient.cs`:
- HttpClient with base URL (localhost dev, Azure prod)
- JWT token management (get from Supabase Auth)
- Add Bearer token to all requests
- Handle 401 unauthorized (redirect to login)

#### 3. Update Authentication
Replace Firebase Auth JS Interop with Supabase:
- Use Supabase.Auth.SignIn (Google provider)
- Store access token in browser localStorage
- Pass token to ApiClient for API calls

**Files to Modify:**
- `Services/AuthService.cs` - Replace Firebase with Supabase
- `Brokers/IFirebaseBroker.cs` → `Brokers/IApiClient.cs`
- `Program.cs` - Register ApiClient instead of FirebaseBroker

### Phase 2: Update Models & Services

#### 1. Update Models
**File:** `Models/FoodItem.cs`

Map to HouseholdRecipeDto:
```csharp
public class Recipe // Rename from FoodItem
{
    public int Id { get; set; } // Change from string to int
    public int HouseholdId { get; set; } // NEW
    public string Name { get; set; }
    public string? Description { get; set; }
    public List<string> ImageUrls { get; set; }
    public Dictionary<string, int?> Ratings { get; set; }
    public double AverageRating { get; set; }
    public DateTime DateAdded { get; set; }
    public int AddedByUserId { get; set; } // Change from string
    public string? AddedByName { get; set; } // NEW
    public bool IsArchived { get; set; }

    // NEW: Multi-tenant fields
    public int? GlobalRecipeId { get; set; }
    public bool IsForked { get; set; }
    public string? PersonalNotes { get; set; }
}
```

#### 2. Create ApiClient Methods
**File:** `Brokers/ApiClient.cs`

```csharp
public class ApiClient : IApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IAuthService _authService;

    public ApiClient(HttpClient httpClient, IAuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }

    public async Task<List<Recipe>> GetRecipesAsync(bool includeArchived = false)
    {
        var token = await _authService.GetAccessTokenAsync();
        _httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var response = await _httpClient.GetAsync(
            $"/api/household-recipes?includeArchived={includeArchived}");
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<List<Recipe>>();
    }

    // Similar methods for add, update, archive, rate, etc.
}
```

#### 3. Update FoodService
**File:** `Services/FoodService.cs`

Replace `IFirebaseBroker` with `IApiClient`:
```csharp
public class FoodService : IFoodService
{
    private readonly IApiClient _apiClient; // Changed from IFirebaseBroker
    private readonly IAuthService _authService;

    public FoodService(IApiClient apiClient, IAuthService authService)
    {
        _apiClient = apiClient;
        _authService = authService;
    }

    public async Task<List<Recipe>> GetAllFoodsAsync(bool includeArchived = false)
    {
        if (!_authService.IsAuthenticated)
            throw new UnauthorizedAccessException();

        return await _apiClient.GetRecipesAsync(includeArchived);
    }

    // Update all other methods similarly
}
```

### Phase 3: Update Pages

#### 1. Login Page
**File:** `Pages/Login.razor`

- Replace Firebase Google login with Supabase
- On successful login, store JWT token
- Redirect to household selection if user has multiple households

#### 2. Home Page
**File:** `Pages/Home.razor`

- **Minimal changes needed** (business logic stays same)
- FoodService API stays identical
- UI components stay the same
- Just works with new backend

#### 3. AddFood Page
**File:** `Pages/AddFood.razor`

**New Features to Add:**
- Browse global recipes button
- Add from global (link vs fork choice)
- Personal notes field

**Changes:**
- Image upload now calls `/api/storage/upload`
- Recipe creation calls `/api/household-recipes`

#### 4. Settings Page
**File:** `Pages/Settings.razor`

**New Household Management Section:**
- List user's households
- Create new household button
- Switch household dropdown
- Invite members by email
- View pending invites
- Leave household option

### Phase 4: New Pages

#### 1. Household Selection (New)
**File:** `Pages/HouseholdSelect.razor`

- Show if user belongs to multiple households
- Allow switching active household
- Display household members

#### 2. Browse Global Recipes (New)
**File:** `Pages/Browse.razor`

- Search bar
- Filters (cuisine, difficulty, prep time, HelloFresh-only)
- Recipe cards with ratings
- "Add to Household" button (link/fork choice)
- Pagination

---

## 🚀 Deployment Tasks

### Backend Deployment (Azure App Service)

#### 1. Create Azure Resources
```bash
az login
az group create --name storhaugen --location westeurope
az appservice plan create --name storhaugen-plan --resource-group storhaugen --sku F1 --is-linux
az webapp create --name storhaugen-eats-api --resource-group storhaugen --plan storhaugen-plan --runtime "DOTNETCORE:10.0"
```

#### 2. Configure Environment Variables
```bash
az webapp config appsettings set --name storhaugen-eats-api --resource-group storhaugen --settings \
  ConnectionStrings__DefaultConnection="YOUR_SUPABASE_CONNECTION_STRING" \
  Supabase__Url="https://ithuvxvsoozmvdicxedx.supabase.co" \
  Supabase__ServiceRoleKey="YOUR_SERVICE_ROLE_KEY" \
  Supabase__JwtSecret="YOUR_JWT_SECRET"
```

#### 3. Deploy via GitHub Actions
**File:** `.github/workflows/deploy-api.yml`

```yaml
name: Deploy API to Azure
on:
  push:
    branches: [main]
    paths: ['StorhaugenEats.API/**']

jobs:
  deploy:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: azure/webapps-deploy@v2
        with:
          app-name: storhaugen-eats-api
          publish-profile: ${{ secrets.AZURE_WEBAPP_PUBLISH_PROFILE }}
          package: './StorhaugenEats.API'
```

#### 4. Update CORS
Add production frontend URL to allowed origins in `Program.cs`:
```csharp
policy.WithOrigins(
    "https://localhost:7000",
    "https://markus-sv.github.io" // Your GitHub Pages URL
)
```

### Frontend Deployment (GitHub Pages)

#### 1. Update API Base URL
**File:** `StorhaugenWebsite/Program.cs`

```csharp
#if DEBUG
var apiBaseUrl = "https://localhost:64797";
#else
var apiBaseUrl = "https://storhaugen-eats-api.azurewebsites.net";
#endif

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
});
```

#### 2. Build & Deploy
```bash
cd StorhaugenWebsite
dotnet publish -c Release
# GitHub Pages deployment (already configured)
```

---

## 📊 Progress Summary

### Backend API: ~70% Complete
- ✅ Authentication & Authorization
- ✅ Households Management
- ✅ Household Recipes (CRUD + Ratings)
- ✅ Database Connection
- ⏳ Global Recipes Browse/Search
- ⏳ Storage Upload/Delete
- ⏳ Users Profile

### Frontend: ~20% Complete
- ✅ Existing UI components (can be reused)
- ⏳ API Client Service
- ⏳ Supabase Auth Integration
- ⏳ Page Migrations
- ⏳ New Household Management UI
- ⏳ Browse Global Recipes Page

### Deployment: 0% Complete
- ⏳ Azure App Service setup
- ⏳ Environment variables configuration
- ⏳ GitHub Actions CI/CD
- ⏳ Frontend API URL update

---

## 🎯 Next Session Priorities

1. **Complete Backend API** (~2-3 hours)
   - Global Recipes controller (browse, search, filters)
   - Storage controller (image upload/delete)
   - Test all endpoints with Postman

2. **Start Frontend Migration** (~3-4 hours)
   - Create ApiClient service
   - Update Authentication to Supabase
   - Migrate Home page (recipes list)

3. **Deploy Backend** (~1 hour)
   - Set up Azure App Service
   - Configure environment variables
   - Deploy and test

4. **Continue Frontend** (~4-5 hours)
   - Household management UI
   - Browse global recipes page
   - Complete remaining pages

---

## 🔗 API Endpoints Summary

### ✅ Implemented

#### Households
```
GET    /api/households/my
GET    /api/households/{id}
POST   /api/households
PUT    /api/households/{id}
POST   /api/households/{id}/invites
GET    /api/households/invites/pending
POST   /api/households/invites/{id}/accept
POST   /api/households/invites/{id}/reject
POST   /api/households/{id}/switch
POST   /api/households/{id}/leave
```

#### Household Recipes
```
GET    /api/household-recipes
GET    /api/household-recipes/{id}
POST   /api/household-recipes
PUT    /api/household-recipes/{id}
POST   /api/household-recipes/{id}/rate
POST   /api/household-recipes/{id}/archive
POST   /api/household-recipes/{id}/restore
POST   /api/household-recipes/{id}/fork
DELETE /api/household-recipes/{id}
```

#### Utilities
```
GET    /health
GET    /test-connections
```

### ⏳ To Be Implemented

#### Global Recipes
```
GET    /api/global-recipes
GET    /api/global-recipes/{id}
POST   /api/global-recipes
GET    /api/global-recipes/search
```

#### Storage
```
POST   /api/storage/upload
DELETE /api/storage/{fileName}
```

#### Users
```
GET    /api/users/me
PUT    /api/users/me
```

---

## 💡 Technical Decisions Made

1. **Integer IDs** instead of GUIDs for better PostgreSQL performance
2. **Supabase JWT** instead of custom auth for easier integration
3. **CurrentUserService** for automatic user context (no manual ID passing)
4. **Reference vs Fork** for flexible global recipe usage
5. **Ratings per household member** instead of single average
6. **EF Core direct queries** instead of repository pattern (simpler for this scale)
7. **Azure App Service** for backend hosting (free tier available)
8. **Transaction Pooler** (port 6543) for Supabase connection

---

## 📝 Notes

- HelloFresh scraper has a loop issue - can be debugged later or manual population
- Direct database connection fails (IPv6 issue) but poolers work fine
- Free Azure tier (F1) has 60 min/day CPU limit - sufficient for low traffic
- Supabase free tier auto-pauses after 1 week inactivity - just resume from dashboard
- No Firebase data migration needed (starting fresh per user decision)

