# Azure Deployment Guide - Storhaugen Eats API

This guide will help you deploy the backend API to Azure App Service.

## Prerequisites

- Azure account with active subscription
- Azure CLI installed locally (optional, can use portal)
- .NET 8 SDK installed

## Step 1: Create Azure App Service

### Using Azure Portal:

1. Go to [Azure Portal](https://portal.azure.com)
2. Click **Create a resource** → **Web App**
3. Configure:
   - **Subscription**: Your subscription
   - **Resource Group**: Create new → `storhaugen-eats-rg`
   - **Name**: `storhaugen-eats-api` (must be globally unique)
   - **Publish**: Code
   - **Runtime stack**: .NET 8 (LTS)
   - **Operating System**: Linux
   - **Region**: West Europe (or closest to Supabase)
   - **Pricing Plan**: B1 (Basic) or higher
4. Click **Review + Create** → **Create**

### Using Azure CLI:

```bash
# Login to Azure
az login

# Create resource group
az group create --name storhaugen-eats-rg --location westeurope

# Create App Service Plan
az appservice plan create \
  --name storhaugen-eats-plan \
  --resource-group storhaugen-eats-rg \
  --sku B1 \
  --is-linux

# Create Web App
az webapp create \
  --resource-group storhaugen-eats-rg \
  --plan storhaugen-eats-plan \
  --name storhaugen-eats-api \
  --runtime "DOTNETCORE:8.0"
```

## Step 2: Configure Application Settings

Add these as **Environment Variables** in Azure App Service:

### Navigation: App Service → Configuration → Application settings

| Name | Value | Source |
|------|-------|--------|
| `ConnectionStrings__DefaultConnection` | Your Supabase connection string | From appsettings.json line 11 |
| `Supabase__Url` | `https://ithuvxvsoozmvdicxedx.supabase.co` | From appsettings.json line 16 |
| `Supabase__AnonKey` | Your anon key | From appsettings.json line 17 |
| `Supabase__ServiceRoleKey` | Your service role key | From appsettings.json line 18 |
| `Supabase__JwtSecret` | Your JWT secret | From appsettings.json line 19 |

### Using Azure Portal:
1. Navigate to your App Service
2. Go to **Configuration** → **Application settings**
3. Click **+ New application setting** for each variable above
4. Click **Save** at the top

### Using Azure CLI:

```bash
# Set connection string
az webapp config connection-string set \
  --resource-group storhaugen-eats-rg \
  --name storhaugen-eats-api \
  --settings DefaultConnection="Host=aws-1-eu-west-1.pooler.supabase.com;Port=6543;Database=postgres;Username=postgres.ithuvxvsoozmvdicxedx;Password=Elias25112099!;Timeout=60;CommandTimeout=60;Pooling=true;MinPoolSize=1;MaxPoolSize=20;SSL Mode=Require;Trust Server Certificate=true;Include Error Detail=true;Keepalive=30;TCP Keepalive=true;TCP Keepalive Time=30;TCP Keepalive Interval=10" \
  --connection-string-type PostgreSQL

# Set Supabase settings
az webapp config appsettings set \
  --resource-group storhaugen-eats-rg \
  --name storhaugen-eats-api \
  --settings \
    Supabase__Url="https://ithuvxvsoozmvdicxedx.supabase.co" \
    Supabase__AnonKey="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Iml0aHV2eHZzb296bXZkaWN4ZWR4Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjQ5NjA1NzIsImV4cCI6MjA4MDUzNjU3Mn0._CnQGm26PbG_8HxoLy5m1lQIfFT6P1RNhLNnbQ4DDy0" \
    Supabase__ServiceRoleKey="eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Iml0aHV2eHZzb296bXZkaWN4ZWR4Iiwicm9sZSI6InNlcnZpY2Vfcm9sZSIsImlhdCI6MTc2NDk2MDU3MiwiZXhwIjoyMDgwNTM2NTcyfQ.U5W3C_LKYYlBkqUfjXfL9nxrWyDxG3PReVsqamfjOWY" \
    Supabase__JwtSecret="qlDo8dj/zFMRia+cZa5ZqOHI+zOc6YLPmk8BhLf6pzREOdpQt+xHhHH/2FxY0z+eygXNrahHu0ihk/VH5VPelA=="
```

## Step 3: Configure CORS

The API needs to accept requests from your frontend domain.

### Using Azure Portal:
1. Go to your App Service
2. Navigate to **CORS** (under API section)
3. Add allowed origins:
   - For development: `https://localhost:5001`, `http://localhost:5000`
   - For production: `https://your-frontend-domain.azurestaticapps.net`
   - Or use `*` for testing (NOT recommended for production)
4. Check **Enable Access-Control-Allow-Credentials**
5. Click **Save**

### Using Azure CLI:

```bash
az webapp cors add \
  --resource-group storhaugen-eats-rg \
  --name storhaugen-eats-api \
  --allowed-origins https://localhost:5001 http://localhost:5000

# For production, add your frontend URL
az webapp cors add \
  --resource-group storhaugen-eats-rg \
  --name storhaugen-eats-api \
  --allowed-origins https://your-frontend-domain.azurestaticapps.net
```

## Step 4: Deploy the Application

### Option A: Deploy from Visual Studio

1. Right-click on `StorhaugenEats.API` project
2. Select **Publish**
3. Choose **Azure** → **Azure App Service (Linux)**
4. Select your subscription and the `storhaugen-eats-api` app service
5. Click **Finish** → **Publish**

### Option B: Deploy using Azure CLI

```bash
cd StorhaugenWebsite/StorhaugenEats.API

# Build and publish
dotnet publish -c Release -o ./publish

# Create deployment package
cd publish
zip -r ../deploy.zip .
cd ..

# Deploy to Azure
az webapp deployment source config-zip \
  --resource-group storhaugen-eats-rg \
  --name storhaugen-eats-api \
  --src deploy.zip
```

### Option C: Deploy using Git

```bash
# Get deployment credentials
az webapp deployment user set \
  --user-name <username> \
  --password <password>

# Get Git URL
az webapp deployment source config-local-git \
  --resource-group storhaugen-eats-rg \
  --name storhaugen-eats-api

# Add Azure remote and push
git remote add azure <git-url-from-previous-command>
git push azure main:master
```

## Step 5: Verify Deployment

### Check if API is running:

```bash
# Get the default hostname
az webapp show \
  --resource-group storhaugen-eats-rg \
  --name storhaugen-eats-api \
  --query defaultHostName --output tsv
```

Visit: `https://storhaugen-eats-api.azurewebsites.net`

You should see the Swagger UI if everything is configured correctly.

### Test API endpoints:

```bash
# Test health check (if you have one)
curl https://storhaugen-eats-api.azurewebsites.net/api/health

# Test Swagger
curl https://storhaugen-eats-api.azurewebsites.net/swagger/index.html
```

## Step 6: Run Database Migrations

The database migrations should run automatically on startup, but if they don't:

### Option 1: Enable migrations on startup

Check `Program.cs` for:
```csharp
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}
```

### Option 2: Run manually from local machine

```bash
cd StorhaugenEats.API

# Set connection string environment variable
export ConnectionStrings__DefaultConnection="your-connection-string"

# Run migrations
dotnet ef database update --connection "your-connection-string"
```

## Step 7: Monitor and Troubleshoot

### View Logs:

**Azure Portal:**
1. Go to App Service → **Log stream**
2. Or **Monitoring** → **Logs**

**Azure CLI:**
```bash
az webapp log tail \
  --resource-group storhaugen-eats-rg \
  --name storhaugen-eats-api
```

### Common Issues:

**1. "Cannot connect to database"**
- Check connection string in Application Settings
- Verify Supabase allows connections from Azure IPs
- Check SSL Mode is set to `Require`

**2. "401 Unauthorized"**
- Check Supabase JWT settings are correct
- Verify `Supabase__Url` includes `/auth/v1` in JWT validation

**3. "CORS errors"**
- Add frontend domain to CORS allowed origins
- Enable credentials if using authentication

**4. "500 Internal Server Error"**
- Check Application Insights or Log Stream
- Look for Entity Framework migration errors
- Verify all environment variables are set

## Step 8: Update Frontend Configuration

Once deployed, update the frontend to use the production API:

**File:** `StorhaugenWebsite/Program.cs`

```csharp
#if DEBUG
var apiBaseUrl = "https://localhost:64797"; // Local API
#else
var apiBaseUrl = "https://storhaugen-eats-api.azurewebsites.net"; // Production
#endif
```

## Production Checklist

Before going live, ensure:

- [ ] All environment variables configured in Azure
- [ ] CORS properly configured for your frontend domain
- [ ] Connection string uses connection pooling
- [ ] Database migrations have run successfully
- [ ] API responds to test requests
- [ ] Swagger UI is accessible (or disabled for production)
- [ ] HTTPS is enforced
- [ ] Application Insights enabled for monitoring
- [ ] Backup plan for database configured
- [ ] Logging configured appropriately

## Cost Optimization

**Basic B1 Plan**: ~$13/month
- 1 Core, 1.75 GB RAM
- Suitable for small to medium apps
- Auto-scaling not available

**Standard S1 Plan**: ~$69/month
- 1 Core, 1.75 GB RAM
- Auto-scaling available
- Better for production

**Free F1 Plan**: $0/month
- Limited to 60 CPU minutes/day
- Good for testing only
- Not recommended for production

## Next Steps

1. Deploy the application
2. Test all API endpoints
3. Configure custom domain (optional)
4. Set up Application Insights monitoring
5. Configure backup strategy
6. Set up CI/CD pipeline (optional)

## Support

If you encounter issues:
- Check Azure App Service logs
- Verify environment variables
- Test database connection from Azure
- Review Supabase authentication settings

---

**Your API URL will be:** `https://storhaugen-eats-api.azurewebsites.net`
