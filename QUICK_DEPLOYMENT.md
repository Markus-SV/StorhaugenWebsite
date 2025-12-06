# Quick Deployment Checklist

## 🚀 Deploy Backend to Azure (5 minutes)

### Option 1: Using Azure CLI (Recommended)

```bash
# 1. Run the deployment script
./deploy-to-azure.sh

# 2. Configure environment variables in Azure Portal
# Visit: https://portal.azure.com → storhaugen-eats-api → Configuration
# Add these application settings:

ConnectionStrings__DefaultConnection=Host=aws-1-eu-west-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.ithuvxvsoozmvdicxedx;Password=Elias25112099!;Timeout=60;CommandTimeout=60;Pooling=true;MinPoolSize=1;MaxPoolSize=20;SSL Mode=Require;Trust Server Certificate=true;Include Error Detail=true;Keepalive=30;TCP Keepalive=true;TCP Keepalive Time=30;TCP Keepalive Interval=10

Supabase__Url=https://ithuvxvsoozmvdicxedx.supabase.co

Supabase__AnonKey=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Iml0aHV2eHZzb296bXZkaWN4ZWR4Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjQ5NjA1NzIsImV4cCI6MjA4MDUzNjU3Mn0._CnQGm26PbG_8HxoLy5m1lQIfFT6P1RNhLNnbQ4DDy0

Supabase__ServiceRoleKey=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Iml0aHV2eHZzb296bXZkaWN4ZWR4Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc2NDk2MDU3MiwiZXhwIjoyMDgwNTM2NTcyfQ.U5W3C_LKYYlBkqUfjXfL9nxrWyDxG3PReVsqamfjOWY

Supabase__JwtSecret=qlDo8dj/zFMRia+cZa5ZqOHI+zOc6YLPmk8BhLf6pzREOdpQt+xHhHH/2FxY0z+eygXNrahHu0ihk/VH5VPelA==
```

### Option 2: Using Visual Studio

1. Right-click `StorhaugenEats.API` project
2. Click **Publish**
3. Choose **Azure → Azure App Service (Linux)**
4. Sign in and create/select app service
5. Click **Publish**
6. Configure environment variables in Azure Portal (same as above)

### Option 3: Manual Azure Portal

1. Visit [Azure Portal](https://portal.azure.com)
2. Create **App Service**:
   - Name: `storhaugen-eats-api`
   - Runtime: .NET 8
   - OS: Linux
   - Region: West Europe
   - Plan: B1 or higher
3. Go to **Configuration** → Add application settings (same as above)
4. Go to **Deployment Center** → Choose deployment method

## ✅ Verify Deployment

```bash
# Test API health
curl https://storhaugen-eats-api.azurewebsites.net/health

# Open Swagger UI
open https://storhaugen-eats-api.azurewebsites.net/swagger
```

## 🎯 Update Frontend

Update `StorhaugenWebsite/Program.cs`:

```csharp
#if DEBUG
var apiBaseUrl = "https://localhost:64797";
#else
var apiBaseUrl = "https://storhaugen-eats-api.azurewebsites.net";  // ← YOUR AZURE URL
#endif
```

## 🧪 Test Locally Against Production Backend

```bash
cd StorhaugenWebsite

# Update Program.cs API URL temporarily to point to Azure
# Change line to: var apiBaseUrl = "https://storhaugen-eats-api.azurewebsites.net";

# Run frontend
dotnet watch run

# Or build and run
dotnet run --urls="https://localhost:7000"
```

## ⚠️ Troubleshooting

### "Cannot connect to database"
```bash
# Check Azure App Service logs
az webapp log tail --resource-group storhaugen-eats-rg --name storhaugen-eats-api
```

### "CORS error"
1. Go to Azure Portal → App Service → CORS
2. Add: `https://localhost:7000`, `https://localhost:7001`
3. Enable **Access-Control-Allow-Credentials**

### "401 Unauthorized"
- Verify all Supabase settings are correct in Azure
- Check `Supabase__JwtSecret` is set
- Check `Supabase__Url` includes correct URL

### "Migrations not running"
- Check Application Insights or Log Stream in Azure
- Migrations run automatically on startup
- Look for "✅ Database migrations applied successfully" in logs

## 📊 Monitor

**View Logs:**
```bash
az webapp log tail --resource-group storhaugen-eats-rg --name storhaugen-eats-api
```

**Or in Portal:**
1. App Service → Log stream
2. App Service → Monitoring → Logs

## 💰 Cost

- **B1 Basic**: ~$13/month (recommended for testing/small production)
- **F1 Free**: $0 (60 min/day limit, good for initial testing only)
- **S1 Standard**: ~$69/month (production with auto-scaling)

## 🎉 You're Ready!

Your API should now be running at:
**https://storhaugen-eats-api.azurewebsites.net**

Test it:
1. Visit `/swagger` to see all endpoints
2. Try `/health` to verify it's running
3. Use Swagger to test authentication and household endpoints
4. Run your Blazor app and test full flow
