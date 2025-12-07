# Database Connection Troubleshooting

## IPv4/IPv6 Connection Issues

Npgsql (PostgreSQL driver for .NET) can have issues with IPv6 resolution when connecting to Supabase. This document explains how to test and resolve these issues.

## Connection Modes

Supabase provides three connection modes:

### 1. **Transaction Pooler (Port 6543)** - Default
```
Host=aws-1-eu-west-1.pooler.supabase.com;Port=6543
```
- Best for serverless/short-lived connections
- Limited to PostgreSQL transaction mode
- Some features not available (e.g., prepared statements, LISTEN/NOTIFY)

### 2. **Session Pooler (Port 5432)**
```
Host=aws-1-eu-west-1.pooler.supabase.com;Port=5432
```
- Session pooling mode
- More features available than transaction mode
- Still uses connection pooling

### 3. **Direct Connection**
```
Host=db.ithuvxvsoozmvdicxedx.supabase.co;Port=5432
```
- Direct connection to database
- All PostgreSQL features available
- Not recommended for production (use pooler instead)

## Testing Connections

### Method 1: Use the Test Endpoint

1. Start your API:
   ```bash
   dotnet run
   ```

2. Call the test endpoint:
   ```bash
   curl http://localhost:5000/test-connections
   ```

3. Check the console output - it will test all three connection modes and show:
   - Which connections succeed/fail
   - Connection time in milliseconds
   - Detailed error messages

### Method 2: Manual psql Test

Test if you can connect directly with psql:

```bash
# Test transaction pooler
psql "postgresql://postgres.ithuvxvsoozmvdicxedx:YOUR_PASSWORD@aws-1-eu-west-1.pooler.supabase.com:6543/postgres?sslmode=require"

# Test session pooler
psql "postgresql://postgres.ithuvxvsoozmvdicxedx:YOUR_PASSWORD@aws-1-eu-west-1.pooler.supabase.com:5432/postgres?sslmode=require"

# Test direct connection
psql "postgresql://postgres.ithuvxvsoozmvdicxedx:YOUR_PASSWORD@db.ithuvxvsoozmvdicxedx.supabase.co:5432/postgres?sslmode=require"
```

## Fixing IPv4/IPv6 Issues

### Option 1: Change Connection Mode

If the transaction pooler (port 6543) fails, try session pooler (port 5432):

In `appsettings.json`, change `DefaultConnection` to use `SessionPooler`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=aws-1-eu-west-1.pooler.supabase.com;Port=5432;..."
  }
}
```

### Option 2: Use Direct Connection (Development Only)

For local development, you can use the direct connection:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=db.ithuvxvsoozmvdicxedx.supabase.co;Port=5432;..."
  }
}
```

⚠️ **Warning**: Direct connections are not recommended for production - use pooler instead.

### Option 3: Force IPv4 Resolution (System Level)

If your system prefers IPv6 but Supabase only responds on IPv4:

**Linux/Mac:**
Add to `/etc/hosts`:
```
# Resolve Supabase pooler to IPv4
<IPv4_ADDRESS>  aws-1-eu-west-1.pooler.supabase.com
```

**Windows:**
Add to `C:\Windows\System32\drivers\etc\hosts`:
```
# Resolve Supabase pooler to IPv4
<IPv4_ADDRESS>  aws-1-eu-west-1.pooler.supabase.com
```

To find the IPv4 address:
```bash
nslookup aws-1-eu-west-1.pooler.supabase.com
# or
dig aws-1-eu-west-1.pooler.supabase.com
```

## Connection String Parameters

The updated connection string includes IPv4/IPv6 resilience parameters:

```
Host=...;Port=...;
Timeout=60;                          # Connection timeout
CommandTimeout=60;                   # Command execution timeout
Pooling=true;                       # Enable connection pooling
MinPoolSize=1;MaxPoolSize=20;       # Pool size limits
SSL Mode=Require;                   # Require SSL
Trust Server Certificate=true;      # Trust self-signed certs
Include Error Detail=true;          # Detailed error messages
Keepalive=30;                       # TCP keepalive (seconds)
TCP Keepalive=true;                 # Enable TCP keepalive
TCP Keepalive Time=30;              # Time before first keepalive
TCP Keepalive Interval=10;          # Interval between keepalives
```

## Common Errors

### "No such host is known" or "The requested name is valid, but no data of the requested type was found"
- **Cause**: DNS resolution failure or IPv6/IPv4 mismatch
- **Fix**: Try session pooler (port 5432) or direct connection

### "timeout period elapsed" or "Connection timed out"
- **Cause**: Network firewall or IPv6 blackhole routing
- **Fix**: Check firewall rules, try different connection mode

### "password authentication failed"
- **Cause**: Wrong password or user doesn't exist
- **Fix**: Double-check password in Supabase dashboard

### "database is paused" or "shutdown: db_termination"
- **Cause**: Supabase free tier auto-pauses after 1 week inactivity
- **Fix**: Resume database from Supabase dashboard

## Recommended Solution

1. **First**: Test all three connection modes using `/test-connections`
2. **Use Session Pooler (Port 5432)** if transaction pooler fails
3. **For development**: Direct connection is acceptable
4. **For production**: Always use pooler (transaction or session mode)

## Current Configuration

Your `appsettings.json` now includes all three connection strings:
- `DefaultConnection`: Transaction pooler (port 6543)
- `SessionPooler`: Session pooler (port 5432)
- `DirectConnection`: Direct database connection (port 5432)

You can switch between them by changing which one is used as `DefaultConnection`, or by modifying `Program.cs` to use a different connection string name.
