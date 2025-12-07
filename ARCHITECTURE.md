# System Architecture - Storhaugen Eats Multi-Tenant Platform

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    FRONTEND (Blazor WASM)                    │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────┐   │
│  │  Browse  │  │  My List │  │ Add Food │  │Household │   │
│  │   Page   │  │   Page   │  │   Page   │  │ Settings │   │
│  └──────────┘  └──────────┘  └──────────┘  └──────────┘   │
└────────────────────────┬────────────────────────────────────┘
                         │ HTTPS REST API (JWT Auth)
                         ▼
┌─────────────────────────────────────────────────────────────┐
│              BACKEND (ASP.NET Core Web API)                  │
│  ┌────────────────────────────────────────────────────────┐ │
│  │                    Controllers                          │ │
│  │  Users │ Households │ GlobalRecipes │ HouseholdRecipes │ │
│  │  Ratings │ HelloFresh                                   │ │
│  └─────────────────────┬──────────────────────────────────┘ │
│                        │                                     │
│  ┌─────────────────────▼──────────────────────────────────┐ │
│  │                  Service Layer                          │ │
│  │  UserService │ HouseholdService │ RecipeServices       │ │
│  │  RatingService │ StorageService │ ScraperService       │ │
│  └─────────────────────┬──────────────────────────────────┘ │
└────────────────────────┼────────────────────────────────────┘
                         │
         ┌───────────────┼───────────────┐
         │               │               │
         ▼               ▼               ▼
┌────────────────┐ ┌──────────┐ ┌──────────────┐
│   Supabase     │ │Supabase  │ │  HelloFresh  │
│   PostgreSQL   │ │ Storage  │ │   API/Web    │
│                │ │          │ │              │
│ • Users        │ │ • Images │ │ • Scraping   │
│ • Households   │ │          │ │ • JSON data  │
│ • Recipes      │ │          │ │              │
│ • Ratings      │ │          │ │              │
└────────────────┘ └──────────┘ └──────────────┘
```

## Data Flow Examples

### 1. User Browses Public Recipes

```
┌──────┐         ┌──────┐          ┌──────────┐         ┌──────────┐
│ User │         │ API  │          │ Service  │         │ Database │
└───┬──┘         └───┬──┘          └────┬─────┘         └────┬─────┘
    │                │                  │                     │
    │ GET /api/globalrecipes?sortBy=rating                   │
    ├───────────────>│                  │                     │
    │                │ GetPublicRecipes()                     │
    │                ├─────────────────>│                     │
    │                │                  │ SELECT * FROM global_recipes
    │                │                  │ WHERE is_hellofresh OR is_public
    │                │                  │ ORDER BY average_rating DESC
    │                │                  ├────────────────────>│
    │                │                  │<────────────────────┤
    │                │<─────────────────┤     Recipes         │
    │<───────────────┤  JSON Response   │                     │
    │  [recipes...]  │                  │                     │
```

### 2. User Adds Recipe to Household List (Linked Mode)

```
┌──────┐    ┌──────┐    ┌──────────┐    ┌──────────┐
│ User │    │ API  │    │ Service  │    │ Database │
└───┬──┘    └───┬──┘    └────┬─────┘    └────┬─────┘
    │           │            │                │
    │ POST /api/householdrecipes/link         │
    │ { globalRecipeId, notes }               │
    ├──────────>│            │                │
    │           │ AddLinked()│                │
    │           ├───────────>│                │
    │           │            │ INSERT INTO household_recipes
    │           │            │ (household_id, global_recipe_id, notes)
    │           │            ├───────────────>│
    │           │            │<───────────────┤
    │           │<───────────┤   Success      │
    │<──────────┤            │                │
    │  Recipe   │            │                │
```

### 3. User Forks a Linked Recipe

```
┌──────┐    ┌──────┐    ┌──────────┐    ┌──────────┐
│ User │    │ API  │    │ Service  │    │ Database │
└───┬──┘    └───┬──┘    └────┬─────┘    └────┬─────┘
    │           │            │                │
    │ POST /api/householdrecipes/{id}/fork   │
    ├──────────>│            │                │
    │           │ ForkRecipe()                │
    │           ├───────────>│                │
    │           │            │ 1. SELECT recipe + global_recipe
    │           │            ├───────────────>│
    │           │            │<───────────────┤
    │           │            │ 2. UPDATE household_recipes
    │           │            │    SET local_title = global.title,
    │           │            │        local_ingredients = global.ingredients,
    │           │            │        global_recipe_id = NULL
    │           │            ├───────────────>│
    │           │            │<───────────────┤
    │           │<───────────┤   Success      │
    │<──────────┤  Forked    │                │
    │   Recipe  │            │                │
```

### 4. User Rates a Recipe (Triggers Aggregation)

```
┌──────┐    ┌──────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│ User │    │ API  │    │ Service  │    │ Database │    │ Trigger  │
└───┬──┘    └───┬──┘    └────┬─────┘    └────┬─────┘    └────┬─────┘
    │           │            │                │                │
    │ POST /api/ratings                       │                │
    │ { globalRecipeId, score: 8 }            │                │
    ├──────────>│            │                │                │
    │           │ UpsertRating()              │                │
    │           ├───────────>│                │                │
    │           │            │ INSERT/UPDATE ratings           │
    │           │            ├───────────────>│                │
    │           │            │                │  Trigger fires │
    │           │            │                ├───────────────>│
    │           │            │                │ UPDATE global_recipes
    │           │            │                │ SET average_rating = AVG(score),
    │           │            │                │     rating_count = COUNT(*)
    │           │            │                │<───────────────┤
    │           │            │<───────────────┤   Success      │
    │           │<───────────┤                │                │
    │<──────────┤  Rating    │                │                │
```

### 5. HelloFresh ETL Sync

```
┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐    ┌──────────┐
│Scheduler │    │   API    │    │ Scraper  │    │HelloFresh│    │ Database │
└────┬─────┘    └────┬─────┘    └────┬─────┘    └────┬─────┘    └────┬─────┘
     │              │               │                │                │
     │ POST /hellofresh/sync        │                │                │
     ├─────────────>│               │                │                │
     │              │ SyncRecipes() │                │                │
     │              ├──────────────>│                │                │
     │              │               │ 1. GET https://hellofresh.no   │
     │              │               ├───────────────>│                │
     │              │               │<───────────────┤   HTML         │
     │              │               │ 2. Extract Build ID             │
     │              │               │ 3. GET /menus/2025-W51.json    │
     │              │               ├───────────────>│                │
     │              │               │<───────────────┤   JSON         │
     │              │               │ 4. For each recipe:             │
     │              │               │    - Parse data                 │
     │              │               │    - Download image             │
     │              │               │    - Upload to Supabase Storage │
     │              │               │    - Upsert to DB               │
     │              │               ├────────────────────────────────>│
     │              │               │<────────────────────────────────┤
     │              │<──────────────┤                │   Success      │
     │<─────────────┤ { added: 50, updated: 10 }    │                │
```

## Database Schema (Simplified)

```
┌─────────────┐         ┌──────────────┐
│    users    │         │  households  │
├─────────────┤         ├──────────────┤
│ id (PK)     │◄───┐    │ id (PK)      │
│ email       │    │    │ name         │
│ share_id    │    │    │ leader_id    │
│ household_id├────┘    │ settings     │
└─────────────┘         └──────────────┘
                               ▲
                               │
                               │ household_id
                               │
                       ┌───────┴──────────┐
                       │                  │
             ┌─────────┴──────────┐  ┌───┴───────────────┐
             │ household_recipes  │  │  global_recipes   │
             ├────────────────────┤  ├───────────────────┤
             │ id (PK)            │  │ id (PK)           │
             │ household_id (FK)  │  │ title             │
             │ global_recipe_id ──┼──┤ ingredients       │
             │   (FK, nullable)   │  │ is_hellofresh     │
             │ local_title        │  │ is_public         │
             │ local_ingredients  │  │ average_rating ◄──┐
             │ personal_notes     │  │ rating_count      │
             └────────────────────┘  └───────────────────┘
                                              ▲
                                              │
                                      ┌───────┴────────┐
                                      │    ratings     │
                                      ├────────────────┤
                                      │ id (PK)        │
                                      │ user_id (FK)   │
                                      │ global_recipe_id
                                      │ score (0-10)   │
                                      │ comment        │
                                      └────────────────┘
```

## Key Design Patterns

### 1. Reference vs Fork (Hybrid Linking)

**Linked Recipe:**
```sql
household_recipes {
  global_recipe_id: UUID,  -- Points to global
  local_title: NULL,       -- Uses global.title
  local_ingredients: NULL, -- Uses global.ingredients
  personal_notes: "We loved this!"  -- Household-specific
}
```

**Forked Recipe:**
```sql
household_recipes {
  global_recipe_id: NULL,  -- No longer linked
  local_title: "Custom Pasta",  -- Own data
  local_ingredients: "[...]",   -- Own data
  personal_notes: "Modified version"
}
```

### 2. Rating Aggregation (Database Trigger)

```sql
-- On INSERT/UPDATE/DELETE of ratings table:
TRIGGER → recalculate_global_recipe_rating()
  ↓
UPDATE global_recipes
SET average_rating = AVG(ratings.score),
    rating_count = COUNT(ratings.*)
WHERE id = affected_recipe_id
```

### 3. Multi-Tenancy via RLS (Row Level Security)

```sql
-- User can only see their household's recipes
CREATE POLICY household_recipes_select ON household_recipes
FOR SELECT USING (
  household_id IN (
    SELECT current_household_id
    FROM users
    WHERE id = auth.uid()
  )
);
```

## Security Model

### Authentication Flow

```
1. User clicks "Login with Google"
   ↓
2. Supabase Auth handles OAuth flow
   ↓
3. User receives JWT token
   ↓
4. Blazor app stores JWT in local storage
   ↓
5. All API requests include: Authorization: Bearer <JWT>
   ↓
6. API validates JWT signature with Supabase secret
   ↓
7. Extract user ID from JWT claims
   ↓
8. Database RLS policies enforce access control
```

### Authorization Levels

| Resource | Public | Authenticated | Household Member | Household Leader |
|----------|--------|---------------|------------------|------------------|
| Browse Global Recipes | ✅ Read | ✅ Read | ✅ Read | ✅ Read |
| Create User Recipe | ❌ | ✅ Create | ✅ Create | ✅ Create |
| View Household Recipes | ❌ | ❌ | ✅ Read | ✅ Read |
| Add to Household | ❌ | ❌ | ✅ Create | ✅ Create |
| Edit Household Settings | ❌ | ❌ | ❌ | ✅ Update |
| Merge Households | ❌ | ❌ | ❌ | ✅ Execute |

## Performance Considerations

### Caching Strategy

1. **Global Recipes** - Cache for 1 hour (rarely changes)
2. **Household Recipes** - No cache (real-time updates)
3. **Ratings** - Cache aggregates for 5 minutes

### Query Optimization

```sql
-- Index on frequently queried columns
CREATE INDEX idx_global_recipes_rating ON global_recipes(average_rating DESC);
CREATE INDEX idx_household_recipes_household ON household_recipes(household_id);

-- Use database view for complex joins
CREATE VIEW household_recipes_full AS
  SELECT hr.*, gr.title, gr.ingredients, ...
  FROM household_recipes hr
  LEFT JOIN global_recipes gr ON hr.global_recipe_id = gr.id;
```

### ETL Performance

- **Batch Processing**: Process recipes in chunks of 50
- **Parallel Uploads**: Upload images concurrently (max 5 at once)
- **Deduplication**: Check HelloFresh UUID before inserting

## Scalability

### Current Limits (Free Tier)

- **Database**: 500 MB (thousands of recipes)
- **Storage**: 1 GB (thousands of images)
- **API Requests**: Unlimited on most hosting
- **Concurrent Users**: Blazor WASM handles 1000+ users easily

### Scale-Up Path

1. **10-100 households**: Current setup perfect
2. **100-1,000 households**: Add Redis cache, CDN for images
3. **1,000+ households**: Supabase Pro tier, load balancer, multiple API instances

## Deployment Architecture

```
┌─────────────────────────────────────────────┐
│           Production Environment             │
│                                              │
│  ┌────────────┐         ┌────────────┐     │
│  │   Blazor   │         │   API      │     │
│  │   WASM     │◄───────►│ (Azure/    │     │
│  │ (Static    │  HTTPS  │  Railway)  │     │
│  │  Hosting)  │         └─────┬──────┘     │
│  └────────────┘               │             │
│       │                       │             │
│       │                       ▼             │
│       │              ┌─────────────────┐    │
│       │              │   Supabase      │    │
│       └─────────────►│ • PostgreSQL    │    │
│                      │ • Storage       │    │
│                      │ • Auth          │    │
│                      └─────────────────┘    │
└─────────────────────────────────────────────┘
```

**Hosting Recommendations:**
- **Blazor WASM**: Netlify, Vercel, GitHub Pages (static hosting)
- **API**: Azure App Service, Railway.app, Render.com
- **Database**: Supabase (managed PostgreSQL)

---

This architecture provides a solid foundation for scaling from a family app to a multi-household social platform! 🚀
