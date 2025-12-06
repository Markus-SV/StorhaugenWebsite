# 🚀 Complete Deployment Instructions

## ✅ What Has Been Prepared

All code and configurations are ready for deployment:

1. ✅ **Complete multi-tenant backend API** with 32 endpoints
2. ✅ **Frontend with household management** and recipe features
3. ✅ **GitHub Actions workflow** configured with .NET 8.0.x
4. ✅ **Backend CORS** includes your GitHub Pages URL
5. ✅ **All code merged** to `claude/continue-session-01Ebyhu8RK9DVNzrSZq2EMkn`
6. ✅ **Database schema** ready in `database/schema.sql`
7. ✅ **Deployment scripts** prepared

**Your Branch**: `claude/continue-session-01Ebyhu8RK9DVNzrSZq2EMkn`
**Project Status**: 95% complete, ready for deployment

---

## 🎯 What You Need to Do

### Step 1: Deploy Backend to Azure (From Your Local Machine)

**Option A: Automated Script (Recommended)**
```bash
# From your local repository
./deploy-to-azure.sh
```

**Option B: Visual Studio**
1. Open `StorhaugenWebsite.sln` in Visual Studio
2. Right-click `StorhaugenEats.API` → Publish
3. Follow the Azure App Service wizard
4. Configure environment variables (see below)

**Option C: Azure Portal + Manual Build**
1. Build the API:
   ```bash
   cd StorhaugenEats.API
   dotnet publish -c Release -o ./publish
   ```
2. Deploy via Azure Portal or Azure CLI

### Step 2: Configure Azure Environment Variables

After deploying, add these in **Azure Portal** → Your App Service → **Configuration** → **Application settings**:

| Setting Name | Value |
|-------------|-------|
| `ConnectionStrings__DefaultConnection` | `Host=aws-1-eu-west-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.ithuvxvsoozmvdicxedx;Password=Elias25112099!;Timeout=60;CommandTimeout=60;Pooling=true;MinPoolSize=1;MaxPoolSize=20;SSL Mode=Require;Trust Server Certificate=true;Include Error Detail=true;Keepalive=30;TCP Keepalive=true;TCP Keepalive Time=30;TCP Keepalive Interval=10` |
| `Supabase__Url` | `https://ithuvxvsoozmvdicxedx.supabase.co` |
| `Supabase__AnonKey` | `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Iml0aHV2eHZzb296bXZkaWN4ZWR4Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjQ5NjA1NzIsImV4cCI6MjA4MDUzNjU3Mn0._CnQGm26PbG_8HxoLy5m1lQIfFT6P1RNhLNnbQ4DDy0` |
| `Supabase__ServiceRoleKey` | `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Iml0aHV2eHZzb296bXZkaWN4ZWR4Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc2NDk2MDU3MiwiZXhwIjoyMDgwNTM2NTcyfQ.U5W3C_LKYYlBkqUfjXfL9nxrWyDxG3PReVsqamfjOWY` |
| `Supabase__JwtSecret` | `qlDo8dj/zFMRia+cZa5ZqOHI+zOc6YLPmk8BhLf6pzREOdpQt+xHhHH/2FxY0z+eygXNrahHu0ihk/VH5VPelA==` |

**Verify Backend Deployment**:
```bash
curl https://storhaugen-eats-api.azurewebsites.net/health
# Should return: {"status":"healthy","timestamp":"..."}
```

### Step 3: Update Supabase OAuth Configuration

1. Go to [Supabase Dashboard](https://supabase.com/dashboard/project/ithuvxvsoozmvdicxedx)
2. Navigate to **Authentication** → **URL Configuration**
3. Add these **Redirect URLs**:
   ```
   https://markus-sv.github.io/StorhaugenWebsite
   https://markus-sv.github.io/StorhaugenWebsite/
   ```
4. Update **Site URL** to:
   ```
   https://markus-sv.github.io/StorhaugenWebsite
   ```
5. Click **Save**

### Step 4: Deploy Frontend to GitHub Pages

```bash
# From your local repository
git fetch origin claude/continue-session-01Ebyhu8RK9DVNzrSZq2EMkn
git checkout main
git merge origin/claude/continue-session-01Ebyhu8RK9DVNzrSZq2EMkn
git push origin main
```

This will trigger the GitHub Actions workflow that:
- Builds the Blazor WASM app
- Publishes to GitHub Pages
- Makes it available at `https://markus-sv.github.io/StorhaugenWebsite`

**Monitor the deployment**:
```
https://github.com/Markus-SV/StorhaugenWebsite/actions
```

### Step 5: Test End-to-End

Visit: `https://markus-sv.github.io/StorhaugenWebsite`

**Test Flow**:
1. ✅ Click "Login with Google"
2. ✅ Authenticate and verify redirect works
3. ✅ Create a household
4. ✅ Browse global recipes
5. ✅ Add recipe as link or fork
6. ✅ Add personal notes
7. ✅ Rate recipes
8. ✅ Invite household members
9. ✅ Switch between households

---

## 📋 Architecture Overview

### Backend (StorhaugenEats.API)
- **Framework**: ASP.NET Core Web API (.NET 8)
- **Database**: PostgreSQL via Supabase
- **Authentication**: Supabase JWT tokens
- **Hosting**: Azure App Service
- **Endpoints**: 32 RESTful API endpoints

**Key Controllers**:
- `GlobalRecipesController` - Manage global recipe catalog
- `HouseholdRecipesController` - Household-specific recipes
- `HouseholdsController` - Household management
- `UsersController` - User profiles
- `RatingsController` - Recipe ratings
- `StorageController` - Image uploads

### Frontend (StorhaugenWebsite)
- **Framework**: Blazor WebAssembly (.NET 8)
- **UI Library**: MudBlazor
- **Authentication**: Supabase Auth (Google OAuth)
- **Hosting**: GitHub Pages
- **State Management**: HouseholdStateService

**Key Pages**:
- `Login.razor` - Google OAuth authentication
- `Browse.razor` - Global recipe catalog
- `FoodDetails.razor` - Recipe details with notes
- `Storage.razor` - Household recipes
- `Settings.razor` - Household management

### Database (Supabase PostgreSQL)
8 tables with Row-Level Security (RLS):

1. **users** - User profiles (auto-created from JWT)
2. **households** - Household groups
3. **household_members** - User-household relationships
4. **household_invites** - Pending invitations
5. **global_recipes** - Shared recipe catalog
6. **household_recipes** - Household recipes (linked or forked)
7. **ratings** - User recipe ratings
8. **etl_sync_logs** - HelloFresh ETL tracking

**Key Pattern**: Reference vs Fork
- **Linked** recipes reference global recipes (updates propagate)
- **Forked** recipes are independent copies (editable)

---

## 🔍 Troubleshooting

### Backend Issues

**"Backend not responding"**
```bash
# Check Azure App Service status
az webapp show --resource-group storhaugen-eats-rg --name storhaugen-eats-api

# View logs
az webapp log tail --resource-group storhaugen-eats-rg --name storhaugen-eats-api
```

**"Database connection failed"**
- Verify connection string in Azure Portal
- Check Supabase database is not paused
- Verify port 6543 (pooler) not 5432 (direct)

**"Migrations failed"**
- Check startup logs for "✅ Database migrations applied successfully"
- Migrations run automatically on app startup
- Verify EF Core connection string format

### Frontend Issues

**"CORS error when calling API"**
- ✅ Already fixed: `https://markus-sv.github.io` added to backend CORS
- Verify backend is deployed and running
- Check browser console for specific error

**"Login redirect fails"**
- Verify Supabase redirect URLs include GitHub Pages URL
- Check browser console for Supabase errors
- Ensure Site URL is set correctly in Supabase

**"GitHub Pages shows 404"**
- Check GitHub Actions workflow completed successfully
- Verify gh-pages branch exists
- Check Settings → Pages → Source is set to gh-pages

---

## 📊 Monitoring

### Backend
```bash
# Real-time logs
az webapp log tail --resource-group storhaugen-eats-rg --name storhaugen-eats-api

# Or in Azure Portal:
# App Service → Monitoring → Log stream
# App Service → Monitoring → Metrics
```

### Frontend
- **GitHub Actions**: https://github.com/Markus-SV/StorhaugenWebsite/actions
- **gh-pages branch**: https://github.com/Markus-SV/StorhaugenWebsite/tree/gh-pages

---

## 🎉 Your Production URLs

After deployment:

| Service | URL |
|---------|-----|
| **Frontend** | https://markus-sv.github.io/StorhaugenWebsite |
| **Backend API** | https://storhaugen-eats-api.azurewebsites.net |
| **API Documentation** | https://storhaugen-eats-api.azurewebsites.net/swagger |
| **Supabase Console** | https://supabase.com/dashboard/project/ithuvxvsoozmvdicxedx |

---

## 📚 Additional Documentation

Detailed guides available in the repository:

- `PROJECT_HANDOFF.md` - Complete project overview (697 lines)
- `NEXT_STEPS.md` - Step-by-step deployment guide
- `AZURE_DEPLOYMENT_GUIDE.md` - Azure deployment details
- `SUPABASE_CONFIG.md` - Supabase configuration
- `ARCHITECTURE.md` - System architecture
- `database/SUPABASE_SETUP.md` - Database setup guide
- `database/schema.sql` - Complete database schema

---

## 🚀 Post-Deployment Tasks

After successful deployment:

### Immediate
1. ✅ Test login with Google OAuth
2. ✅ Create test household
3. ✅ Add test recipes
4. ✅ Verify CRUD operations work
5. ✅ Test with multiple users

### Short-term
1. Complete HelloFresh ETL scraping (40% done in `HelloFreshScraperService.cs`)
2. Add recipe editing for forked recipes
3. Implement recipe search/filtering
4. Add image upload for custom recipes
5. Test with real data

### Long-term
1. Replace hardcoded family names with dynamic members
2. Add meal planning features
3. Set up Application Insights monitoring
4. Configure automated database backups
5. Consider custom domain (optional)

---

## ❓ Need Help?

If you encounter issues:

1. **Check logs**: Backend in Azure Portal, Frontend in browser console
2. **Review documentation**: See files listed above
3. **Verify configuration**: Environment variables, CORS, OAuth URLs
4. **Test connectivity**:
   - Backend: `curl https://storhaugen-eats-api.azurewebsites.net/health`
   - Database: Check Supabase dashboard
   - Frontend: Check GitHub Actions logs

**Common Issue Checklist**:
- [ ] Azure App Service is running
- [ ] Environment variables are configured
- [ ] Supabase database is active (not paused)
- [ ] Supabase OAuth redirect URLs include GitHub Pages
- [ ] GitHub Actions workflow completed successfully
- [ ] CORS includes `https://markus-sv.github.io`

---

## 📝 Summary

**What's Ready**:
- ✅ Complete multi-tenant recipe platform
- ✅ 32 backend API endpoints
- ✅ Frontend with household management
- ✅ Database schema with RLS
- ✅ Authentication with Google OAuth
- ✅ GitHub Actions deployment workflow
- ✅ Azure deployment script
- ✅ Comprehensive documentation

**Your Next Steps**:
1. Deploy backend to Azure (Step 1)
2. Configure Azure environment variables (Step 2)
3. Update Supabase OAuth URLs (Step 3)
4. Merge to main and deploy frontend (Step 4)
5. Test end-to-end (Step 5)

**Estimated Time**: 30-45 minutes total

Good luck with the deployment! 🎊
