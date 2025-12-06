# Supabase Setup Guide - Storhaugen Eats

This guide will walk you through setting up your Supabase project for the multi-tenant Storhaugen Eats platform.

## Step 1: Create Supabase Project

1. Go to [https://supabase.com](https://supabase.com)
2. Sign in or create an account
3. Click **"New Project"**
4. Fill in:
   - **Name**: `storhaugen-eats`
   - **Database Password**: (Generate strong password - **SAVE THIS**)
   - **Region**: Choose closest to your users (e.g., `Europe West` for Norway)
   - **Pricing Plan**: Free tier is fine for development
5. Click **"Create new project"** (takes ~2 minutes)

## Step 2: Run Database Schema

1. In your Supabase project, go to **SQL Editor** (left sidebar)
2. Click **"New query"**
3. Copy the contents of `database/schema.sql`
4. Paste into the SQL editor
5. Click **"Run"** (or press `Ctrl+Enter`)
6. Verify success: You should see "Success. No rows returned"

## Step 3: Configure Authentication

### Enable Google OAuth

1. Go to **Authentication** → **Providers** (left sidebar)
2. Find **Google** and toggle it **ON**
3. You'll need Google OAuth credentials:

   **Get Google OAuth Credentials:**
   - Go to [Google Cloud Console](https://console.cloud.google.com)
   - Create a new project or select existing
   - Navigate to **APIs & Services** → **Credentials**
   - Click **"Create Credentials"** → **"OAuth 2.0 Client ID"**
   - Application type: **Web application**
   - Name: `Storhaugen Eats`
   - **Authorized redirect URIs**:
     - Copy the "Callback URL" from Supabase Google provider settings
     - Paste into Google (e.g., `https://yourproject.supabase.co/auth/v1/callback`)
   - Click **Create**
   - Copy **Client ID** and **Client Secret**

4. Paste Client ID and Secret into Supabase Google provider settings
5. Click **Save**

### Configure Email Settings (Optional)

If you want email confirmations:
1. Go to **Authentication** → **Email Templates**
2. Customize as needed
3. For production, set up custom SMTP in **Settings** → **Auth**

## Step 4: Set Up Storage for Images

1. Go to **Storage** (left sidebar)
2. Click **"Create a new bucket"**
3. Name: `recipe-images`
4. **Public bucket**: Toggle **ON** (so images are publicly accessible)
5. Click **"Create bucket"**

### Set Storage Policies

1. Click on the `recipe-images` bucket
2. Go to **Policies** tab
3. Create policies:

**Policy 1: Public Read Access**
```sql
CREATE POLICY "Public can view images"
ON storage.objects FOR SELECT
USING (bucket_id = 'recipe-images');
```

**Policy 2: Authenticated Upload**
```sql
CREATE POLICY "Authenticated users can upload"
ON storage.objects FOR INSERT
WITH CHECK (
  bucket_id = 'recipe-images' AND
  auth.role() = 'authenticated'
);
```

**Policy 3: Users can update their own uploads**
```sql
CREATE POLICY "Users can update own images"
ON storage.objects FOR UPDATE
USING (
  bucket_id = 'recipe-images' AND
  auth.uid()::text = (storage.foldername(name))[1]
);
```

**Policy 4: Users can delete their own uploads**
```sql
CREATE POLICY "Users can delete own images"
ON storage.objects FOR DELETE
USING (
  bucket_id = 'recipe-images' AND
  auth.uid()::text = (storage.foldername(name))[1]
);
```

## Step 5: Get Your API Keys

1. Go to **Settings** → **API** (left sidebar)
2. Copy these values (you'll need them for the backend):

   ```
   Project URL: https://xxxxxxxxxxxxx.supabase.co
   Anon (public) key: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
   Service Role key: eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
   ```

   - **Anon key**: Use in frontend (Blazor)
   - **Service Role key**: Use in backend API (has admin privileges)

## Step 6: Save Configuration

Create a file `database/supabase-config.env` (add to `.gitignore`):

```bash
# Supabase Configuration
SUPABASE_URL=https://xxxxxxxxxxxxx.supabase.co
SUPABASE_ANON_KEY=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
SUPABASE_SERVICE_ROLE_KEY=eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
DATABASE_PASSWORD=your-db-password-from-step1

# Google OAuth (for reference)
GOOGLE_CLIENT_ID=xxxxxxxxxxxxx.apps.googleusercontent.com
GOOGLE_CLIENT_SECRET=GOCSPX-xxxxxxxxxxxxx
```

**IMPORTANT**: Add this to `.gitignore`:
```
database/supabase-config.env
```

## Step 7: Test Database Connection

Run this test query in **SQL Editor** to verify everything is set up:

```sql
-- Test: Create a test household
INSERT INTO households (name, settings)
VALUES ('Test Family', '{"theme": "dark"}')
RETURNING *;

-- Verify it was created
SELECT * FROM households;

-- Clean up test data
DELETE FROM households WHERE name = 'Test Family';
```

## Step 8: Verify RLS Policies

Test Row Level Security is working:

1. Go to **SQL Editor**
2. Run this query:
   ```sql
   -- This should show all policies
   SELECT schemaname, tablename, policyname
   FROM pg_policies
   WHERE schemaname = 'public'
   ORDER BY tablename, policyname;
   ```

3. You should see policies for:
   - `households`
   - `users`
   - `global_recipes`
   - `household_recipes`
   - `ratings`
   - `household_invites`

## Next Steps

✅ Database schema created
✅ Authentication configured
✅ Storage bucket ready
✅ API keys obtained

**You're ready to build the ASP.NET Core Web API!**

---

## Useful Links

- **Supabase Dashboard**: https://supabase.com/dashboard/project/your-project-id
- **Database Editor**: https://supabase.com/dashboard/project/your-project-id/editor
- **API Documentation**: https://supabase.com/dashboard/project/your-project-id/api
- **Storage**: https://supabase.com/dashboard/project/your-project-id/storage/buckets

## Troubleshooting

**Problem**: "relation does not exist" error
- **Solution**: Make sure you ran the entire `schema.sql` file

**Problem**: Can't upload images
- **Solution**: Check storage policies are created and bucket is public

**Problem**: Google OAuth not working
- **Solution**: Verify redirect URI exactly matches in both Google Console and Supabase

**Problem**: RLS blocking queries
- **Solution**: During development, you can temporarily disable RLS on a table:
  ```sql
  ALTER TABLE table_name DISABLE ROW LEVEL SECURITY;
  ```
  (Remember to re-enable for production!)
