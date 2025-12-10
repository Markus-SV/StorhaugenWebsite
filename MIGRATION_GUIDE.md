# Migration Guide: Household-Centric to User-Centric Architecture

This guide covers migrating StorhaugenEats from household-based recipe ownership to user-based ownership.

## Overview

| Before (Legacy) | After (New) |
|-----------------|-------------|
| HouseholdRecipe | UserRecipe |
| HouseholdFriendship | UserFriendship |
| Recipes owned by household | Recipes owned by user |
| `IsPublic: true/false` | `Visibility: private/household/friends/public` |

## Prerequisites

- Database backup completed
- Application in maintenance mode (recommended)
- Admin user authenticated

## Migration Endpoints

All endpoints require authentication. Base URL: `/api/admin/migration`

### 1. Check Current State

```bash
GET /api/admin/migration/stats
```

Returns:
```json
{
  "totalHouseholdRecipes": 150,
  "totalUserRecipes": 0,
  "householdRecipesNotMigrated": 150,
  "ratingsWithHouseholdRecipeId": 300,
  "ratingsWithUserRecipeId": 0,
  "totalHouseholdFriendships": 20,
  "totalUserFriendships": 0,
  "migrationComplete": false
}
```

### 2. Dry Run (Recommended First)

Test migration without making changes:

```bash
POST /api/admin/migration/run?dryRun=true
```

### 3. Run Migration

Execute the actual migration:

```bash
POST /api/admin/migration/run?dryRun=false
```

Or run individual steps:

```bash
POST /api/admin/migration/recipes?dryRun=false
POST /api/admin/migration/ratings?dryRun=false
POST /api/admin/migration/friendships?dryRun=false
```

### 4. Verify Migration

```bash
GET /api/admin/migration/verify
```

Individual checks:
```bash
GET /api/admin/migration/verify/recipes    # All recipes migrated?
GET /api/admin/migration/verify/ratings    # Ratings linked correctly?
GET /api/admin/migration/verify/integrity  # Data matches?
GET /api/admin/migration/verify/orphans    # No orphaned records?
```

## Migration Logic

### Recipes
- Each `HouseholdRecipe` creates a `UserRecipe` with the same ID
- `AddedByUserId` → `UserId` (recipe owner)
- `IsPublic: true` → `Visibility: "public"`
- `IsPublic: false` → `Visibility: "household"`
- Duplicates (same user + GlobalRecipeId) are skipped

### Ratings
- `Rating.UserRecipeId` is set to match `Rating.HouseholdRecipeId`
- Only ratings with existing UserRecipes are migrated

### Friendships
- Each unique user pair from HouseholdFriendship creates a UserFriendship
- Status and timestamps are preserved

## Rollback

If issues occur:
1. UserRecipes can be deleted (HouseholdRecipes remain intact)
2. Rating.UserRecipeId can be set back to NULL
3. UserFriendships can be deleted

The legacy tables remain unchanged during migration.

## Post-Migration

After successful migration and verification:
1. Update frontend to use new endpoints (`/api/user-recipes/*`, `/api/friendships/*`)
2. Monitor for issues
3. Plan removal of deprecated endpoints (marked with `[Obsolete]`)

## Troubleshooting

| Issue | Solution |
|-------|----------|
| "Duplicate key" errors | Recipe already migrated, check stats |
| Ratings not linking | Run recipe migration first |
| Verification fails | Check specific endpoint for details |
