# Supabase Configuration for Production

## Important: OAuth Redirect URLs

When deploying to production, you **MUST** update Supabase OAuth settings to include your production frontend URL.

### Steps:

1. Go to [Supabase Dashboard](https://supabase.com/dashboard)
2. Select your project: `ithuvxvsoozmvdicxedx`
3. Navigate to **Authentication** → **URL Configuration**
4. Add your production frontend URL to **Redirect URLs**:

### Development URLs (already configured):
```
http://localhost:5000
https://localhost:5001
https://localhost:7000
https://localhost:7001
```

### Production URLs (ADD THESE):

**If deploying frontend to Azure Static Web Apps:**
```
https://your-app-name.azurestaticapps.net
https://your-custom-domain.com (if using custom domain)
```

**If deploying to another service:**
```
https://your-production-domain.com
```

### Site URL

Update the **Site URL** to your production frontend URL:
```
https://your-production-domain.com
```

## OAuth Provider Configuration

### Google OAuth (currently configured):

1. Go to **Authentication** → **Providers** → **Google**
2. Verify **Authorized redirect URIs** in Google Cloud Console includes:
   ```
   https://ithuvxvsoozmvdicxedx.supabase.co/auth/v1/callback
   ```
3. Add your production domain to **Authorized JavaScript origins**:
   ```
   https://your-production-domain.com
   ```

## Database Configuration

### Row Level Security (RLS)

RLS policies are already configured in migrations, but verify they're active:

```sql
-- Check RLS is enabled on all tables
SELECT schemaname, tablename, rowsecurity
FROM pg_tables
WHERE schemaname = 'public';
```

All tables should have `rowsecurity = true`.

### Connection Pooling

The API is configured to use Supabase's connection pooler:
- **Pooler Port**: 6543 (IPv4 pooler)
- **Direct Port**: 5432 (direct connection)

Current connection string uses the pooler for better performance.

## API Configuration in Azure

### Environment Variables Required:

| Variable | Value | Notes |
|----------|-------|-------|
| `Supabase__Url` | `https://ithuvxvsoozmvdicxedx.supabase.co` | Your Supabase project URL |
| `Supabase__AnonKey` | Your anon key | Public key for client-side |
| `Supabase__ServiceRoleKey` | Your service role key | Backend-only, has elevated permissions |
| `Supabase__JwtSecret` | Your JWT secret | Used to validate tokens |
| `ConnectionStrings__DefaultConnection` | Full connection string | PostgreSQL connection |

### Security Notes:

⚠️ **IMPORTANT**:
- Never commit `ServiceRoleKey` or `JwtSecret` to git
- Use Azure App Service **Application Settings** for secrets
- Enable **Application Insights** for monitoring
- Set up **Azure Key Vault** for production secrets (optional but recommended)

## Testing Authentication

### Test Login Flow:

1. Navigate to your frontend
2. Click "Login with Google"
3. Should redirect to Google OAuth
4. After authentication, should redirect back to your app
5. Check browser console for any CORS errors
6. Verify JWT token is present in session storage

### Common Issues:

**"Invalid redirect URI"**
- Add your domain to Supabase Redirect URLs
- Check for trailing slashes (be consistent)

**"CORS error on auth callback"**
- Add frontend domain to API CORS policy
- Verify credentials are allowed in CORS

**"JWT validation failed"**
- Check `Supabase__JwtSecret` matches your Supabase project
- Verify issuer URL is correct: `{SupabaseUrl}/auth/v1`

## Frontend Supabase Configuration

File: `StorhaugenWebsite/Program.cs`

Already configured with your Supabase project:
```csharp
var supabaseUrl = "https://ithuvxvsoozmvdicxedx.supabase.co";
var supabaseKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."; // Anon key
```

### For Production:

Consider moving these to configuration file:
```csharp
var supabaseUrl = builder.Configuration["Supabase:Url"];
var supabaseKey = builder.Configuration["Supabase:AnonKey"];
```

Then add to `wwwroot/appsettings.json`:
```json
{
  "Supabase": {
    "Url": "https://ithuvxvsoozmvdicxedx.supabase.co",
    "AnonKey": "your-anon-key-here"
  }
}
```

## Monitoring

### Check Authentication Logs:

1. Supabase Dashboard → **Authentication** → **Users**
2. See all logged-in users
3. Check last sign-in times

### Check API Usage:

1. Supabase Dashboard → **Settings** → **API**
2. See request counts
3. Monitor rate limits

## Backup

Supabase provides automatic backups, but consider:

1. **Point-in-Time Recovery**: Enable in Settings
2. **Manual Backups**: Use `pg_dump` for critical snapshots
3. **Export Users**: Backup user data separately

```bash
# Export users (from Supabase Dashboard)
# Settings → Database → Backups → Point-in-time Recovery
```

## Rate Limits

**Free Tier Limits:**
- 50,000 monthly active users
- 500 MB database space
- 1 GB bandwidth
- 2 GB file storage

**Upgrade if exceeding:**
- Pro: $25/month
- Includes 100,000 MAU, 8 GB database, 50 GB bandwidth

## Contact Support

If issues persist:
- [Supabase Support](https://supabase.com/support)
- [Supabase Discord](https://discord.supabase.com)
- [Supabase GitHub Issues](https://github.com/supabase/supabase/issues)
