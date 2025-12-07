# Next Steps - Ready for Deployment

## ✅ What Just Got Fixed

1. **GitHub Actions Workflow** - Changed .NET version from 10.0.x → 8.0.x
2. **Backend CORS** - Added your GitHub Pages URL (`https://markus-sv.github.io`)
3. **PROJECT_HANDOFF.md** - Updated with confirmed configuration

**Everything is now ready for deployment!**

---

## 🚀 Deployment Steps (In Order)

### Step 1: Deploy Backend to Azure (15 minutes)

You have **3 options**. Choose one:

#### Option A: Automated Script (Easiest)
```bash
# From repository root
./deploy-to-azure.sh

# When prompted, configure these environment variables in Azure Portal:
# (See Step 2 below for the values)
```

#### Option B: Visual Studio (If you prefer GUI)
1. Open `StorhaugenWebsite.sln` in Visual Studio
2. Right-click `StorhaugenEats.API` project → **Publish**
3. Choose **Azure** → **Azure App Service (Linux)**
4. Sign in to your Azure account
5. Create new App Service:
   - Name: `storhaugen-eats-api` (or your choice)
   - Subscription: Your subscription
   - Resource Group: Create new → `storhaugen-eats-rg`
   - Runtime: .NET 8
   - OS: Linux
   - Region: West Europe
6. Click **Publish**
7. Configure environment variables (see Step 2)

#### Option C: Azure Portal (Manual)
See `AZURE_DEPLOYMENT_GUIDE.md` for detailed steps.

---

### Step 2: Configure Azure Environment Variables (5 minutes)

**After deploying**, you MUST configure these in Azure:

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to your App Service (e.g., `storhaugen-eats-api`)
3. Go to **Configuration** → **Application settings**
4. Click **+ New application setting** and add each of these:

| Name | Value |
|------|-------|
| `ConnectionStrings__DefaultConnection` | `Host=aws-1-eu-west-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.ithuvxvsoozmvdicxedx;Password=Elias25112099!;Timeout=60;CommandTimeout=60;Pooling=true;MinPoolSize=1;MaxPoolSize=20;SSL Mode=Require;Trust Server Certificate=true;Include Error Detail=true;Keepalive=30;TCP Keepalive=true;TCP Keepalive Time=30;TCP Keepalive Interval=10` |
| `Supabase__Url` | `https://ithuvxvsoozmvdicxedx.supabase.co` |
| `Supabase__AnonKey` | `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Iml0aHV2eHZzb296bXZkaWN4ZWR4Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjQ5NjA1NzIsImV4cCI6MjA4MDUzNjU3Mn0._CnQGm26PbG_8HxoLy5m1lQIfFT6P1RNhLNnbQ4DDy0` |
| `Supabase__ServiceRoleKey` | `eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Iml0aHV2eHZzb296bXZkaWN4ZWR4Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc2NDk2MDU3MiwiZXhwIjoyMDgwNTM2NTcyfQ.U5W3C_LKYYlBkqUfjXfL9nxrWyDxG3PReVsqamfjOWY` |
| `Supabase__JwtSecret` | `qlDo8dj/zFMRia+cZa5ZqOHI+zOc6YLPmk8BhLf6pzREOdpQt+xHhHH/2FxY0z+eygXNrahHu0ihk/VH5VPelA==` |

5. Click **Save** at the top
6. Wait for the app to restart (~1 minute)

**Verify Backend is Running:**
```bash
# Should return {"status":"healthy","timestamp":"..."}
curl https://storhaugen-eats-api.azurewebsites.net/health

# Or open in browser:
https://storhaugen-eats-api.azurewebsites.net/swagger
```

---

### Step 3: Update Supabase OAuth (2 minutes)

1. Go to [Supabase Dashboard](https://supabase.com/dashboard/project/ithuvxvsoozmvdicxedx)
2. Navigate to **Authentication** → **URL Configuration**
3. Add to **Redirect URLs**:
   ```
   https://markus-sv.github.io/StorhaugenWebsite
   https://markus-sv.github.io/StorhaugenWebsite/
   ```
4. Update **Site URL** to:
   ```
   https://markus-sv.github.io/StorhaugenWebsite
   ```
5. Click **Save**

---

### Step 4: Deploy Frontend to GitHub Pages (5 minutes)

Your GitHub Actions workflow is already set up. Just merge to main:

```bash
# 1. Make sure you're on your feature branch
git checkout claude/multi-tenant-food-app-refactor-0165XDASp2JYXM1R5w3SqvY2

# 2. Pull any changes from main
git fetch origin main
git merge origin/main
# (Resolve any conflicts if they appear - unlikely)

# 3. Push updated feature branch
git push origin claude/multi-tenant-food-app-refactor-0165XDASp2JYXM1R5w3SqvY2

# 4. Switch to main
git checkout main

# 5. Merge your feature branch into main
git merge claude/multi-tenant-food-app-refactor-0165XDASp2JYXM1R5w3SqvY2

# 6. Push to main (triggers GitHub Actions deployment)
git push origin main

# 7. Watch the deployment:
# Go to: https://github.com/Markus-SV/StorhaugenWebsite/actions
# Wait for the workflow to complete (~2-3 minutes)
```

**Verify Frontend is Running:**
```
https://markus-sv.github.io/StorhaugenWebsite
```

---

### Step 5: Test End-to-End (10 minutes)

Visit your app: `https://markus-sv.github.io/StorhaugenWebsite`

**Critical Test Flow:**
1. ✅ Click "Login with Google"
2. ✅ Authenticate with Google
3. ✅ Should redirect back to your app
4. ✅ Should auto-create user in database
5. ✅ Create a household
6. ✅ Browse global recipes (see HelloFresh recipes)
7. ✅ Add a recipe as "link"
8. ✅ View recipe details
9. ✅ Add personal notes
10. ✅ Fork the linked recipe
11. ✅ Rate the recipe
12. ✅ Invite a member (use second email)
13. ✅ Switch households

**If login fails:**
- Check browser console for errors
- Verify Supabase redirect URLs are correct
- Check backend logs in Azure Portal

---

## 🔍 Troubleshooting

### Backend Issues

**"Cannot find backend API"**
```bash
# Check if backend is running:
curl https://storhaugen-eats-api.azurewebsites.net/health

# Check backend logs:
az webapp log tail --resource-group storhaugen-eats-rg --name storhaugen-eats-api

# Or in Azure Portal:
# Your App Service → Monitoring → Log stream
```

**"Database connection failed"**
- Check environment variables are set in Azure
- Verify connection string is correct
- Check Supabase database is not paused

**"Migrations failed"**
- Migrations run automatically on startup
- Check logs for "✅ Database migrations applied successfully"
- If failed, check connection string and database access

### Frontend Issues

**"CORS error"**
- Verify backend is deployed and running
- Check `https://markus-sv.github.io` is in backend CORS (already added)
- Try clearing browser cache

**"Login redirect fails"**
- Check Supabase redirect URLs include your GitHub Pages URL
- Verify Site URL is set correctly
- Check browser console for specific error

**"API calls return 401"**
- Check Supabase JWT secret is correct in Azure
- Verify you're logged in (check session storage)
- Try logging out and back in

---

## 📊 Monitor Your Deployment

### Backend Monitoring
```bash
# Real-time logs
az webapp log tail --resource-group storhaugen-eats-rg --name storhaugen-eats-api

# Or in Azure Portal:
# App Service → Monitoring → Metrics
# App Service → Monitoring → Application Insights
```

### Frontend Monitoring
```bash
# Check GitHub Actions workflow
https://github.com/Markus-SV/StorhaugenWebsite/actions

# View gh-pages branch
https://github.com/Markus-SV/StorhaugenWebsite/tree/gh-pages
```

---

## 🎉 You're Done!

If all steps succeeded, you now have:
- ✅ Backend API running on Azure
- ✅ Frontend running on GitHub Pages
- ✅ Database on Supabase
- ✅ Google OAuth working
- ✅ Multi-tenant household system operational

**Your URLs:**
- **Frontend**: https://markus-sv.github.io/StorhaugenWebsite
- **Backend API**: https://storhaugen-eats-api.azurewebsites.net
- **Backend Swagger**: https://storhaugen-eats-api.azurewebsites.net/swagger
- **Database**: https://supabase.com/dashboard/project/ithuvxvsoozmvdicxedx

---

## 📞 Need Help?

Check these documents:
- `PROJECT_HANDOFF.md` - Complete project overview
- `AZURE_DEPLOYMENT_GUIDE.md` - Detailed Azure deployment
- `QUICK_DEPLOYMENT.md` - Quick reference
- `SUPABASE_CONFIG.md` - Supabase configuration

**Common Issues:**
1. Backend not responding → Check Azure App Service is running
2. Login fails → Check Supabase redirect URLs
3. CORS errors → Already fixed (GitHub Pages URL added)
4. Database errors → Check environment variables in Azure

---

## 🚀 What's Next?

After successful deployment, you can:
1. Test with real data
2. Invite actual users
3. Implement remaining features (HelloFresh ETL, recipe editing)
4. Set up custom domain (optional)
5. Configure Application Insights for better monitoring
6. Set up automated backups

**Enjoy your multi-tenant recipe platform!** 🎊
