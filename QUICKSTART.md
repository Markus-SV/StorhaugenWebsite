# Quick Start Guide - 5 Minutes to Running Backend

## Prerequisites
- .NET 8 SDK installed
- Supabase account (free tier is fine)

## Setup in 5 Steps

### 1. Create Supabase Project (2 min)
1. Go to [supabase.com](https://supabase.com)
2. Create new project → Wait for it to provision
3. Go to **SQL Editor** → Paste contents of `database/schema.sql` → Run

### 2. Configure Google OAuth (1 min)
1. Supabase → **Authentication** → **Providers** → Toggle **Google** ON
2. Use existing Google OAuth credentials from your current Firebase setup
3. Paste Client ID & Secret → Save

### 3. Create Storage Bucket (30 sec)
1. Supabase → **Storage** → **Create bucket**
2. Name: `recipe-images`
3. **Public bucket**: ON → Create

### 4. Configure API (1 min)
1. Copy `StorhaugenEats.API/appsettings.json`
2. Fill in (from Supabase → Settings → API):
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "COPY_FROM_SUPABASE_DATABASE_SETTINGS"
     },
     "Supabase": {
       "Url": "https://xxxxx.supabase.co",
       "AnonKey": "COPY_ANON_KEY",
       "ServiceRoleKey": "COPY_SERVICE_ROLE_KEY",
       "JwtSecret": "COPY_JWT_SECRET"
     }
   }
   ```

### 5. Run API (30 sec)
```bash
cd StorhaugenWebsite/StorhaugenEats.API
dotnet restore
dotnet run
```

**Open:** https://localhost:5001/swagger

## Test It Works

### 1. Health Check
```bash
curl https://localhost:5001/health
```
Expected: `{"status":"healthy","timestamp":"..."}`

### 2. Browse Public Recipes (empty initially)
```bash
curl https://localhost:5001/api/globalrecipes
```
Expected: `[]` (empty array)

### 3. Trigger HelloFresh Sync (requires login first)
- Login via your Blazor app
- Get JWT token from browser DevTools
- Run:
  ```bash
  curl -X POST https://localhost:5001/api/hellofresh/sync?force=true \
    -H "Authorization: Bearer YOUR_JWT_TOKEN"
  ```

## Next Steps

1. Read `README_REFACTOR.md` for complete documentation
2. Update Blazor frontend to call API instead of Firebase
3. Create household management pages
4. Deploy to production

## Quick Reference

**API Base URL:** https://localhost:5001 (development)

**Key Endpoints:**
- `GET /api/globalrecipes` - Browse public recipes
- `GET /api/householdrecipes` - My household's recipes
- `POST /api/households` - Create household
- `POST /api/ratings` - Rate a recipe
- `POST /api/hellofresh/sync` - Sync HelloFresh recipes

**Swagger UI:** https://localhost:5001/swagger

## Troubleshooting

**Can't connect to database?**
- Check connection string format
- Verify Supabase project is active

**401 Unauthorized?**
- You need to login via Blazor app first to get JWT token
- Some endpoints (browse) don't require auth

**HelloFresh sync fails?**
- Test: `curl https://localhost:5001/api/hellofresh/build-id`
- Check internet connection from server

---

**You're ready to go! 🚀**

See `README_REFACTOR.md` for detailed frontend integration steps.
