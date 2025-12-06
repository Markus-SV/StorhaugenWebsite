# 🚀 Quick Start - Deploy Your Recipe Platform

## TL;DR - Get Running in 30 Minutes

Your multi-tenant recipe sharing platform is **95% complete** and ready to deploy!

### What You Have
- ✅ Complete backend API with 32 endpoints
- ✅ Blazor WASM frontend with household management
- ✅ PostgreSQL database with multi-tenant architecture
- ✅ Google OAuth authentication
- ✅ Reference vs Fork recipe pattern
- ✅ Image uploads to Supabase Storage

### What You Need To Do
1. Deploy backend to Azure (15 min)
2. Configure environment variables (5 min)
3. Update Supabase OAuth settings (2 min)
4. Deploy frontend to GitHub Pages (5 min)
5. Test everything (10 min)

---

## 🎯 One-Command Deployment (From Your Local Machine)

```bash
# 1. Deploy backend to Azure
./deploy-to-azure.sh

# 2. Deploy frontend to GitHub Pages
git checkout main
git merge claude/continue-session-01Ebyhu8RK9DVNzrSZq2EMkn
git push origin main

# 3. Visit your app
open https://markus-sv.github.io/StorhaugenWebsite
```

Then configure Azure environment variables and Supabase OAuth (see `DEPLOYMENT_INSTRUCTIONS.md`).

---

## 📋 Architecture at a Glance

```
┌─────────────────────────────────────────────────────────────┐
│                    USER (Browser)                           │
│          https://markus-sv.github.io/StorhaugenWebsite      │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ Blazor WASM + MudBlazor
                     │ Google OAuth (Supabase Auth)
                     │
                     ↓
┌─────────────────────────────────────────────────────────────┐
│              GitHub Pages (Frontend)                        │
│  - Blazor WebAssembly (.NET 8)                             │
│  - HouseholdStateService (multi-tenant context)            │
│  - ApiClient (REST calls to backend)                       │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ HTTPS + CORS
                     │ JWT Bearer Token
                     │
                     ↓
┌─────────────────────────────────────────────────────────────┐
│        Azure App Service (Backend API)                      │
│  https://storhaugen-eats-api.azurewebsites.net             │
│                                                              │
│  - 32 REST API endpoints                                   │
│  - JWT validation (Supabase JWT Secret)                    │
│  - Auto-migrations (EF Core)                               │
│  - CurrentUserService (auto-create users)                  │
└────────────────────┬────────────────────────────────────────┘
                     │
                     │ Npgsql (PostgreSQL)
                     │ Connection Pooling (port 6543)
                     │
                     ↓
┌─────────────────────────────────────────────────────────────┐
│           Supabase (Database + Auth + Storage)              │
│  https://ithuvxvsoozmvdicxedx.supabase.co                  │
│                                                              │
│  PostgreSQL Database (8 tables with RLS):                  │
│  ├── users                                                  │
│  ├── households                                             │
│  ├── household_members                                      │
│  ├── household_invites                                      │
│  ├── global_recipes                                         │
│  ├── household_recipes                                      │
│  ├── ratings                                                │
│  └── etl_sync_logs                                          │
│                                                              │
│  Auth: Google OAuth provider                               │
│  Storage: Recipe images                                     │
└─────────────────────────────────────────────────────────────┘
```

---

## 🏗️ Key Features Implemented

### Multi-Tenant Household System
- Users can belong to multiple households
- Automatic household context switching
- Invite system with email notifications
- Household member management

### Reference vs Fork Pattern
```
Global Recipe (HelloFresh)
       │
       ├──> Household Recipe (LINKED)
       │    └── Tracks updates from global
       │    └── Personal notes only
       │
       └──> Household Recipe (FORKED)
            └── Independent copy
            └── Fully editable
            └── No updates from global
```

### Recipe Features
- Browse global recipe catalog (HelloFresh)
- Add recipes as links or forks
- Personal notes per recipe
- 5-star rating system
- Image uploads (Supabase Storage)
- Ingredients and instructions
- Prep/cook time tracking

### Authentication & Security
- Google OAuth via Supabase
- JWT token validation
- Row-Level Security (RLS) in database
- Automatic user creation from JWT
- Household-based data isolation

---

## 🗂️ File Structure

```
StorhaugenWebsite/
├── StorhaugenEats.API/                 # Backend API (.NET 8)
│   ├── Controllers/
│   │   ├── GlobalRecipesController.cs  # Global recipe catalog
│   │   ├── HouseholdRecipesController.cs
│   │   ├── HouseholdsController.cs     # Household management
│   │   ├── UsersController.cs
│   │   ├── RatingsController.cs
│   │   └── StorageController.cs        # Image uploads
│   ├── Services/
│   │   ├── CurrentUserService.cs       # Auto-create users from JWT
│   │   ├── GlobalRecipeService.cs
│   │   ├── HouseholdRecipeService.cs
│   │   ├── HouseholdService.cs
│   │   ├── HelloFreshScraperService.cs # ETL scraping (40% done)
│   │   └── SupabaseStorageService.cs
│   ├── Models/                         # EF Core entities
│   ├── DTOs/                           # Request/Response DTOs
│   ├── Data/AppDbContext.cs            # EF Core context
│   └── Program.cs                      # API configuration
│
├── StorhaugenWebsite/                  # Frontend (Blazor WASM)
│   ├── Pages/
│   │   ├── Login.razor                 # Google OAuth
│   │   ├── Browse.razor                # Global recipe catalog
│   │   ├── FoodDetails.razor           # Recipe details + notes
│   │   ├── Storage.razor               # Household recipes
│   │   └── Settings.razor              # Household management
│   ├── Components/
│   │   ├── HouseholdSelector.razor     # Switch households
│   │   ├── HouseholdMembersDialog.razor
│   │   └── InviteMemberDialog.razor
│   ├── Services/
│   │   ├── HouseholdStateService.cs    # Multi-tenant context
│   │   ├── FoodService.cs              # Adapter pattern
│   │   └── SupabaseAuthService.cs
│   ├── ApiClient/ApiClient.cs          # REST API client
│   └── Program.cs                      # Frontend configuration
│
├── database/
│   ├── schema.sql                      # Complete DB schema
│   └── SUPABASE_SETUP.md              # Database setup guide
│
├── .github/workflows/
│   └── deploy.yml                      # GitHub Pages deployment
│
├── deploy-to-azure.sh                  # Azure deployment script
│
└── Documentation/
    ├── PROJECT_HANDOFF.md              # Complete project overview
    ├── DEPLOYMENT_INSTRUCTIONS.md      # Detailed deployment guide
    ├── NEXT_STEPS.md                   # Step-by-step deployment
    ├── AZURE_DEPLOYMENT_GUIDE.md       # Azure-specific guide
    ├── SUPABASE_CONFIG.md              # Supabase configuration
    └── ARCHITECTURE.md                 # System architecture
```

---

## 🎯 API Endpoints (32 Total)

### Global Recipes (6 endpoints)
```
GET    /api/globalrecipes              # List all global recipes
GET    /api/globalrecipes/{id}         # Get recipe details
POST   /api/globalrecipes              # Create global recipe
PUT    /api/globalrecipes/{id}         # Update global recipe
DELETE /api/globalrecipes/{id}         # Delete global recipe
GET    /api/globalrecipes/search?q=... # Search recipes
```

### Household Recipes (11 endpoints)
```
GET    /api/households/{id}/recipes            # List household recipes
GET    /api/householdrecipes/{id}              # Get recipe details
POST   /api/households/{id}/recipes/link       # Link global recipe
POST   /api/households/{id}/recipes/fork       # Fork global recipe
POST   /api/households/{id}/recipes/custom     # Create custom recipe
PUT    /api/householdrecipes/{id}              # Update recipe
DELETE /api/householdrecipes/{id}              # Delete recipe
POST   /api/householdrecipes/{id}/fork         # Fork linked recipe
GET    /api/householdrecipes/{id}/notes        # Get personal notes
PUT    /api/householdrecipes/{id}/notes        # Update personal notes
DELETE /api/householdrecipes/{id}/notes        # Delete personal notes
```

### Households (8 endpoints)
```
GET    /api/households                 # List user's households
GET    /api/households/{id}            # Get household details
POST   /api/households                 # Create household
PUT    /api/households/{id}            # Update household
DELETE /api/households/{id}            # Delete household
GET    /api/households/{id}/members    # List members
POST   /api/households/{id}/invite     # Invite member
DELETE /api/households/{id}/members/{userId} # Remove member
```

### Users (3 endpoints)
```
GET    /api/users/me                   # Get current user
PUT    /api/users/me                   # Update profile
GET    /api/users/{id}                 # Get user by ID
```

### Ratings (2 endpoints)
```
POST   /api/recipes/{id}/rating        # Rate recipe
GET    /api/recipes/{id}/ratings       # Get recipe ratings
```

### Storage (1 endpoint)
```
POST   /api/storage/upload             # Upload image
```

### HelloFresh ETL (1 endpoint)
```
POST   /api/hellofresh/sync            # Trigger ETL sync (WIP)
```

---

## 🔒 Security Features

### Authentication
- Google OAuth via Supabase Auth
- JWT bearer tokens
- Automatic token refresh
- Secure session storage

### Authorization
- Row-Level Security (RLS) on all tables
- Household-based access control
- User can only access their households
- Admin users per household

### Data Protection
- HTTPS everywhere
- CORS configured for specific origins
- SQL injection prevention (EF Core)
- Input validation on all endpoints

---

## 📊 Database Schema Highlights

### Key Relationships
```sql
users (1) ──→ (N) household_members (N) ──→ (1) households
                                               │
                                               │
                                               ↓
                             household_recipes (N) ──→ (1) global_recipes
                                               │
                                               │
                                               ↓
                                          ratings (N)
```

### Reference vs Fork Logic
```sql
household_recipes table:
- is_linked = true  → References global_recipe_id, read-only
- is_linked = false → Forked copy, fully editable
- personal_notes    → Always editable (even for linked)
```

---

## 🚀 Deployment Checklist

### Pre-Deployment
- [x] All code committed to `claude/continue-session-01Ebyhu8RK9DVNzrSZq2EMkn`
- [x] GitHub Actions workflow configured (.NET 8.0.x)
- [x] Backend CORS includes GitHub Pages URL
- [x] Frontend configured for production API URL
- [x] Database schema ready (`database/schema.sql`)
- [x] Deployment scripts prepared

### Backend Deployment
- [ ] Run `./deploy-to-azure.sh` from local machine
- [ ] Configure Azure environment variables (5 settings)
- [ ] Verify health endpoint: `/health`
- [ ] Check Swagger docs: `/swagger`
- [ ] Verify migrations applied successfully

### Frontend Deployment
- [ ] Merge to main branch
- [ ] GitHub Actions workflow completes
- [ ] Verify site loads at GitHub Pages URL
- [ ] Check browser console for errors

### Post-Deployment
- [ ] Update Supabase OAuth redirect URLs
- [ ] Test Google OAuth login
- [ ] Create test household
- [ ] Add test recipes (link and fork)
- [ ] Test with multiple users
- [ ] Verify image uploads work

---

## 🎉 You're Almost There!

Everything is prepared and ready. Follow `DEPLOYMENT_INSTRUCTIONS.md` for detailed steps.

**Your Branch**: `claude/continue-session-01Ebyhu8RK9DVNzrSZq2EMkn`

**Estimated Deployment Time**: 30-45 minutes

**Need Help?** Check the documentation files listed above.

Good luck! 🚀
