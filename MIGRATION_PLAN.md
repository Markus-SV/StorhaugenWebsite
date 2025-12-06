# Frontend Migration & Hosting Plan

## Current Architecture

```
┌─────────────────────────────────────────┐
│  Blazor WASM (GitHub Pages)             │
│  - Static files only                    │
│  - Client-side Firebase SDK (JS Interop)│
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│  Firebase                                │
│  - Firestore (NoSQL database)           │
│  - Firebase Auth (Google Login)         │
│  - Firebase Storage (Images)            │
└─────────────────────────────────────────┘
```

## Target Architecture

```
┌─────────────────────────────────────────┐
│  Blazor WASM (GitHub Pages)             │
│  - Static files only                    │
│  - HttpClient to call API               │
└──────────────┬──────────────────────────┘
               │ HTTPS/REST
               ▼
┌─────────────────────────────────────────┐
│  ASP.NET Core Web API (Cloud Host)      │
│  - Business logic                       │
│  - Authentication (Supabase JWT)        │
└──────────────┬──────────────────────────┘
               │
               ▼
┌─────────────────────────────────────────┐
│  Supabase                                │
│  - PostgreSQL (Relational database)     │
│  - Supabase Auth (Google Login)         │
│  - Supabase Storage (Images)            │
└─────────────────────────────────────────┘
```

---

## Backend Hosting Options

### Option 1: Azure App Service (Recommended)
**Best for: .NET developers, easy deployment, scalability**

**Pros:**
- Native .NET support (no Docker required)
- Free tier available (F1)
- Easy CI/CD with GitHub Actions
- Auto-scaling
- Integrated logging/monitoring

**Cons:**
- Free tier has limitations (60 min/day CPU time)
- Can be expensive at scale

**Cost:**
- Free: F1 tier (60 min/day, 1GB RAM, 1GB storage)
- Paid: B1 tier ~$13/month (24/7, 1.75GB RAM)

**Setup:**
```bash
# Install Azure CLI
az login
az webapp up --name storhaugen-eats-api --resource-group storhaugen --runtime "DOTNET|10.0" --location westeurope --plan storhaugen-plan --sku F1
```

**GitHub Actions deployment:** Auto-deploy on push to main

---

### Option 2: Railway.app (Easiest)
**Best for: Quick deployment, simple pricing**

**Pros:**
- Extremely easy deployment (connect GitHub repo)
- Free tier: $5 credit/month
- Automatic HTTPS
- Built-in PostgreSQL (or use Supabase)
- No Docker knowledge required

**Cons:**
- Less flexible than Azure
- Free tier limited ($5/month credit ≈ 500 hours)

**Cost:**
- Free: $5 credit/month
- Paid: Pay-as-you-go ($0.000231/GB-s)

**Setup:**
1. Connect GitHub repo
2. Railway auto-detects .NET app
3. Add environment variables
4. Deploy

---

### Option 3: Render.com
**Best for: Free tier, Docker-based**

**Pros:**
- Free tier (with limitations)
- Easy deployment
- Automatic HTTPS
- Good documentation

**Cons:**
- Free tier spins down after 15 min inactivity (cold starts)
- Requires Dockerfile

**Cost:**
- Free: Spins down after inactivity
- Paid: $7/month (always-on)

**Setup:**
Requires a `Dockerfile` (I can create this for you)

---

### Option 4: Fly.io
**Best for: Global edge deployment, Docker-based**

**Pros:**
- Free tier: 3 shared VMs, 3GB storage
- Global edge deployment
- Fast cold starts
- Great for distributed apps

**Cons:**
- Requires Docker knowledge
- Slightly more complex setup

**Cost:**
- Free: 3 shared VMs (256MB RAM each)
- Paid: $1.94/month per VM

---

### Recommendation

**For you: Azure App Service (F1 Free Tier) or Railway**

**Why Azure:**
- You're already using .NET/C#
- Free tier is generous (60 min/day is enough for low traffic)
- Easy to upgrade to B1 ($13/month) for 24/7
- GitHub Actions integration
- Can upgrade to pay-as-you-go later

**Why Railway (alternative):**
- Simpler than Azure (no Azure account needed)
- $5/month credit is enough for testing/development
- Auto-deploy from GitHub
- Less vendor lock-in

---

## Migration Strategy

### Phase 1: Backend API Completion (50% done)
**Status:** ✅ Database schema, ✅ Models, ✅ Services, ✅ Controllers

**Remaining tasks:**
1. ✅ Fix HelloFresh scraper (can skip for now)
2. ⬜ Add Supabase Auth integration (JWT validation)
3. ⬜ Test all API endpoints
4. ⬜ Deploy to hosting platform

---

### Phase 2: Frontend Migration (Next)
**Goal:** Replace Firebase with API calls

**Current Firebase operations to migrate:**

| Firebase Operation | New API Endpoint | Status |
|-------------------|------------------|--------|
| `loginWithGoogle()` | Supabase Auth (client-side) | ⬜ |
| `getFoodItems()` | `GET /api/HouseholdRecipes` | ⬜ |
| `addFoodItem()` | `POST /api/HouseholdRecipes` | ⬜ |
| `updateFoodItem()` | `PUT /api/HouseholdRecipes/{id}` | ⬜ |
| `archiveFoodItem()` | `PATCH /api/HouseholdRecipes/{id}/archive` | ⬜ |
| `restoreFoodItem()` | `PATCH /api/HouseholdRecipes/{id}/restore` | ⬜ |
| `uploadImage()` | `POST /api/Storage/upload` | ⬜ |
| `deleteImage()` | `DELETE /api/Storage/{fileName}` | ⬜ |

**Implementation steps:**

1. **Create API client service** (replaces FirebaseBroker)
   ```csharp
   public class ApiClient : IApiClient
   {
       private readonly HttpClient _httpClient;
       private readonly IAuthService _authService;

       public ApiClient(HttpClient httpClient, IAuthService authService)
       {
           _httpClient = httpClient;
           _httpClient.BaseAddress = new Uri("https://your-api-url.com");
           _authService = authService;
       }

       // Add JWT token to all requests
       private async Task<HttpRequestMessage> CreateAuthenticatedRequest(HttpMethod method, string url)
       {
           var request = new HttpRequestMessage(method, url);
           var token = await _authService.GetAccessTokenAsync();
           request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
           return request;
       }
   }
   ```

2. **Update models** (align with backend DTOs)
   - `FoodItem` → `HouseholdRecipeDto`
   - Add `HouseholdId` to all operations

3. **Replace FirebaseBroker with ApiClient**
   - Swap injection in `Program.cs`
   - Update FoodService to use ApiClient

4. **Authentication changes**
   - Replace Firebase Auth JS Interop with Supabase Auth
   - Use Supabase client library for Blazor WASM
   - Store JWT in browser storage

5. **Update Pages**
   - No changes needed (business logic stays the same)

---

### Phase 3: Multi-Tenant Features (Future)

After basic migration works:

1. **Household Management**
   - Create/join household UI
   - Invite family members
   - Switch between households

2. **Browse Global Recipes**
   - Feed/browse page
   - Search HelloFresh recipes
   - Add to household (fork or link)

3. **Social Features**
   - Rate global recipes
   - See what other households are eating

---

## Deployment Pipeline

### Frontend (Blazor WASM → GitHub Pages)
**Current:** Already set up ✅

**No changes needed:**
- Build: `dotnet publish -c Release`
- Deploy: Push to `gh-pages` branch
- GitHub Pages serves static files

**Update required:**
- Change API base URL from localhost to production
  ```csharp
  builder.Services.AddScoped(sp => new HttpClient
  {
      BaseAddress = new Uri("https://your-api-url.com")
  });
  ```

---

### Backend (API → Cloud Host)

#### Option A: Azure App Service

1. **Create Azure resources:**
   ```bash
   az login
   az group create --name storhaugen --location westeurope
   az appservice plan create --name storhaugen-plan --resource-group storhaugen --sku F1
   az webapp create --name storhaugen-eats-api --resource-group storhaugen --plan storhaugen-plan --runtime "DOTNET|10.0"
   ```

2. **Set environment variables:**
   ```bash
   az webapp config appsettings set --name storhaugen-eats-api --resource-group storhaugen --settings \
     ConnectionStrings__DefaultConnection="Host=aws-1-eu-west-1.pooler.supabase.com;..." \
     Supabase__Url="https://ithuvxvsoozmvdicxedx.supabase.co" \
     Supabase__JwtSecret="your-jwt-secret"
   ```

3. **Deploy with GitHub Actions:**
   ```yaml
   # .github/workflows/deploy-api.yml
   name: Deploy API
   on:
     push:
       branches: [main]
       paths: ['StorhaugenEats.API/**']

   jobs:
     deploy:
       runs-on: ubuntu-latest
       steps:
         - uses: actions/checkout@v3
         - uses: azure/login@v1
           with:
             creds: ${{ secrets.AZURE_CREDENTIALS }}
         - name: Deploy to Azure
           run: |
             dotnet publish StorhaugenEats.API -c Release
             az webapp deploy --name storhaugen-eats-api --resource-group storhaugen --src-path ./publish.zip
   ```

#### Option B: Railway

1. Go to railway.app
2. "New Project" → "Deploy from GitHub repo"
3. Select `StorhaugenWebsite` repo
4. Railway auto-detects .NET
5. Set root directory: `StorhaugenEats.API`
6. Add environment variables (same as above)
7. Deploy

**Railway will give you a URL:** `storhaugen-eats-api.up.railway.app`

---

## Data Migration

**Current data:** Firebase Firestore (your existing food items)

**Migration options:**

### Option 1: Manual re-entry (Recommended for small datasets)
- Start fresh with new multi-tenant structure
- Re-add important recipes manually
- HelloFresh scraper will populate global recipes

### Option 2: Firebase export + import script
1. Export Firestore data to JSON
2. Create migration script to transform data
3. Import into PostgreSQL

**I can create this script if you have many existing food items to migrate.**

---

## Configuration Changes Needed

### 1. Frontend: Update API base URL

**File:** `StorhaugenWebsite/Program.cs`

```csharp
// Development
#if DEBUG
var apiBaseUrl = "https://localhost:64797";
#else
// Production
var apiBaseUrl = "https://storhaugen-eats-api.azurewebsites.net"; // or Railway URL
#endif

builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(apiBaseUrl)
});
```

### 2. Backend: CORS configuration

**File:** `StorhaugenEats.API/Program.cs`

Update allowed origins:
```csharp
policy.WithOrigins(
    "https://localhost:7000", // Development
    "https://markus-sv.github.io" // Your GitHub Pages URL
)
```

### 3. Backend: Environment variables

**File:** `appsettings.Production.json` (create this)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "*** set via environment variable ***"
  },
  "Supabase": {
    "Url": "https://ithuvxvsoozmvdicxedx.supabase.co",
    "ServiceRoleKey": "*** set via environment variable ***",
    "JwtSecret": "*** set via environment variable ***"
  }
}
```

**Never commit secrets to git!** Use environment variables on hosting platform.

---

## Estimated Timeline

| Phase | Tasks | Time Estimate |
|-------|-------|---------------|
| **Backend completion** | Add auth, test endpoints, deploy | 2-3 hours |
| **Frontend migration** | Create API client, update services | 3-4 hours |
| **Testing & debugging** | E2E testing, bug fixes | 2-3 hours |
| **Multi-tenant features** | Household management UI | 4-6 hours |
| **Browse/feed feature** | Global recipe browsing | 3-4 hours |
| **Total** | | **14-20 hours** |

---

## Next Steps (Recommended Order)

### Step 1: Deploy Backend to Cloud ⬜
**Why first:** Need production API URL for frontend development

**Tasks:**
1. Choose hosting platform (Azure or Railway)
2. Deploy current API code
3. Test endpoints with Postman/curl
4. Verify database connection works

### Step 2: Create Frontend API Client ⬜
**Tasks:**
1. Create `ApiClient.cs` (replaces FirebaseBroker)
2. Add Supabase Auth client library
3. Implement authentication flow

### Step 3: Migrate One Page at a Time ⬜
**Order:**
1. Login page (auth migration)
2. Home page (list recipes)
3. Add Food page (create recipe)
4. Food Details page (update/archive)
5. Settings page (household management)

### Step 4: Test & Deploy ⬜
1. Test full flow on localhost
2. Update production API URL
3. Deploy frontend to GitHub Pages
4. Verify E2E

### Step 5: Add Multi-Tenant Features ⬜
1. Household invite/join UI
2. Browse global recipes page
3. HelloFresh integration UI

---

## Questions to Answer

1. **Which hosting platform do you prefer?**
   - Azure (free tier, .NET-native, more features)
   - Railway (simpler, $5/month, easier setup)

2. **Do you have existing food data in Firebase to migrate?**
   - If yes, I'll create a migration script
   - If no, we can start fresh

3. **Priority for next phase?**
   - A) Deploy backend + basic migration (get it working ASAP)
   - B) Multi-tenant features first (full vision)
   - C) Frontend migration only (keep Firebase for now, just refactor UI)

4. **Budget for hosting?**
   - Free only (Azure F1 or Railway $5 credit)
   - $10-20/month (Azure B1 or Railway paid)

Let me know your preferences and I'll start implementation!
