# Storhaugen Eats - Comprehensive Refactoring Plan

## Executive Summary

This document outlines a comprehensive refactoring plan to transform Storhaugen Eats from a **household-centric meal planning utility** to a **user-centric social recipe application**. The key architectural shifts are:

1. **User-First Architecture**: Users can function without a household (Personal Cookbook)
2. **Recipe Publishing Flow**: Local recipes can be "published" to become Global Recipes
3. **User-Level Friendships**: Individual user friendships instead of household friendships
4. **Social Feed**: Activity feed showing friend ratings and discoveries
5. **Aggregated Household View**: Household recipe list becomes a dynamic aggregation of member recipes

---

## Table of Contents

1. [Current Architecture Overview](#1-current-architecture-overview)
2. [Target Architecture](#2-target-architecture)
3. [Database Schema Changes](#3-database-schema-changes)
4. [API Changes](#4-api-changes)
5. [Frontend Changes](#5-frontend-changes)
6. [Migration Strategy](#6-migration-strategy)
7. [Implementation Phases](#7-implementation-phases)
8. [Risk Assessment](#8-risk-assessment)

---

## 1. Current Architecture Overview

### Current Data Model
```
User (1) ──→ (N) HouseholdMember ──→ (N) Household
                                          │
                                          ↓
                                    HouseholdRecipe ──→ GlobalRecipe (optional)
                                          │
                                          ↓
                                       Rating
```

### Current Problems
- **Users cannot function without a household** - The app blocks access on the home page
- **Recipes belong to households, not users** - When a user leaves, they lose their recipes
- **Friendships are at household level** - Not user-centric for social features
- **No "publish" flow** - Users can't share their custom recipes globally
- **Duplicate "Browse" logic** - Separate endpoints for HelloFresh vs Community recipes

---

## 2. Target Architecture

### New Data Model
```
User (1) ──→ (N) UserRecipe ──→ GlobalRecipe (optional, via publishing)
  │                │
  │                └──→ (N) Rating
  │
  ├──→ (N) UserFriendship ──→ (N) User
  │
  └──→ (N) HouseholdMember ──→ (N) Household
                                     │
                                     └──→ (Aggregated View of Member UserRecipes)
```

### Key Architectural Principles

1. **Users OWN Recipes**: `UserRecipes` table replaces `HouseholdRecipes`
2. **Households VIEW Recipes**: The household recipe list is an aggregation of member recipes
3. **Publishing Creates Global Recipes**: Local recipes can be promoted to `GlobalRecipes`
4. **Users Have Friends**: `UserFriendship` table for individual connections
5. **Feed Shows Friend Activity**: Query friend ratings and activity

---

## 3. Database Schema Changes

### 3.1 New Tables

#### `user_recipes` (Replaces `household_recipes`)
```sql
CREATE TABLE public.user_recipes (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,                          -- Owner (FK to users)
    global_recipe_id uuid,                          -- Link to published/global recipe (nullable)

    -- Local recipe data (used when not linked or when forked)
    local_title character varying,
    local_description text,
    local_ingredients jsonb,
    local_image_url text,
    local_image_urls jsonb DEFAULT '[]'::jsonb,

    -- Metadata
    personal_notes text,
    is_archived boolean NOT NULL DEFAULT false,
    archived_date timestamp with time zone,
    visibility character varying NOT NULL DEFAULT 'private',  -- 'private', 'household', 'friends', 'public'

    -- Timestamps
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    updated_at timestamp with time zone NOT NULL DEFAULT now(),

    CONSTRAINT user_recipes_pkey PRIMARY KEY (id),
    CONSTRAINT FK_user_recipes_users_user_id FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE,
    CONSTRAINT FK_user_recipes_global_recipes_global_recipe_id FOREIGN KEY (global_recipe_id) REFERENCES public.global_recipes(id)
);

CREATE INDEX IX_user_recipes_user_id ON public.user_recipes(user_id);
CREATE INDEX IX_user_recipes_global_recipe_id ON public.user_recipes(global_recipe_id);
CREATE INDEX IX_user_recipes_visibility ON public.user_recipes(visibility);
```

#### `user_friendships` (New table for user-level friendships)
```sql
CREATE TABLE public.user_friendships (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    requester_user_id uuid NOT NULL,
    target_user_id uuid NOT NULL,
    status character varying NOT NULL DEFAULT 'pending',  -- 'pending', 'accepted', 'rejected', 'blocked'
    message character varying(255),
    created_at timestamp with time zone NOT NULL DEFAULT now(),
    responded_at timestamp with time zone,

    CONSTRAINT user_friendships_pkey PRIMARY KEY (id),
    CONSTRAINT FK_user_friendships_requester FOREIGN KEY (requester_user_id) REFERENCES public.users(id) ON DELETE CASCADE,
    CONSTRAINT FK_user_friendships_target FOREIGN KEY (target_user_id) REFERENCES public.users(id) ON DELETE CASCADE,
    CONSTRAINT UQ_user_friendships_unique_pair UNIQUE (requester_user_id, target_user_id),
    CONSTRAINT CHK_user_friendships_no_self CHECK (requester_user_id != target_user_id)
);

CREATE INDEX IX_user_friendships_requester ON public.user_friendships(requester_user_id);
CREATE INDEX IX_user_friendships_target ON public.user_friendships(target_user_id);
CREATE INDEX IX_user_friendships_status ON public.user_friendships(status);
```

#### `activity_feed` (Denormalized feed for performance)
```sql
CREATE TABLE public.activity_feed (
    id uuid NOT NULL DEFAULT gen_random_uuid(),
    user_id uuid NOT NULL,                          -- Who performed the action
    activity_type character varying NOT NULL,       -- 'rated', 'added', 'published', 'joined_household'
    target_type character varying NOT NULL,         -- 'user_recipe', 'global_recipe', 'household'
    target_id uuid NOT NULL,
    metadata jsonb DEFAULT '{}'::jsonb,             -- Additional context (recipe name, rating score, etc.)
    created_at timestamp with time zone NOT NULL DEFAULT now(),

    CONSTRAINT activity_feed_pkey PRIMARY KEY (id),
    CONSTRAINT FK_activity_feed_user FOREIGN KEY (user_id) REFERENCES public.users(id) ON DELETE CASCADE
);

CREATE INDEX IX_activity_feed_user_id ON public.activity_feed(user_id);
CREATE INDEX IX_activity_feed_created_at ON public.activity_feed(created_at DESC);
CREATE INDEX IX_activity_feed_type ON public.activity_feed(activity_type);
```

### 3.2 Modified Tables

#### `users` - Add fields for personal settings
```sql
ALTER TABLE public.users
    ADD COLUMN is_profile_public boolean NOT NULL DEFAULT true,
    ADD COLUMN bio text,
    ADD COLUMN favorite_cuisines jsonb DEFAULT '[]'::jsonb;
```

#### `global_recipes` - Add published_from reference
```sql
ALTER TABLE public.global_recipes
    ADD COLUMN published_from_user_recipe_id uuid,
    ADD COLUMN is_editable boolean NOT NULL DEFAULT true;

-- HelloFresh recipes should not be editable
UPDATE public.global_recipes SET is_editable = false WHERE is_hellofresh = true;
```

#### `ratings` - Update to reference user_recipes instead of household_recipes
```sql
ALTER TABLE public.ratings
    ADD COLUMN user_recipe_id uuid;

ALTER TABLE public.ratings
    ADD CONSTRAINT FK_ratings_user_recipes FOREIGN KEY (user_recipe_id)
    REFERENCES public.user_recipes(id) ON DELETE CASCADE;

-- Note: household_recipe_id will be deprecated and removed in migration phase
```

### 3.3 Tables to Deprecate (Keep for Migration, Remove Later)

- `household_recipes` - Data migrated to `user_recipes`
- `household_friendships` - Optionally keep for household-level features, or migrate to user friendships

### 3.4 Entity Relationship Diagram (After Refactoring)

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           USER-CENTRIC MODEL                            │
└─────────────────────────────────────────────────────────────────────────┘

    ┌────────────┐         ┌──────────────────┐         ┌───────────────┐
    │   users    │ 1 ──→ N │  user_recipes    │ N ←── 1 │global_recipes │
    │            │         │                  │         │               │
    │ id         │         │ id               │         │ id            │
    │ email      │         │ user_id (FK)     │         │ title         │
    │ display_   │         │ global_recipe_id │         │ is_hellofresh │
    │ name       │         │ local_title      │         │ created_by_   │
    │ current_   │         │ visibility       │         │ user_id       │
    │ household_ │         │ personal_notes   │         │ published_    │
    │ id         │         │ is_archived      │         │ from_user_    │
    │ is_profile │         │                  │         │ recipe_id     │
    │ _public    │         │                  │         │               │
    └────────────┘         └──────────────────┘         └───────────────┘
          │                        │
          │                        │ N
          │                        ↓
          │                 ┌──────────────┐
          │              1  │   ratings    │
          │     ┌───────────│              │
          │     │           │ id           │
          │     │           │ user_id (FK) │
          │     │           │ user_recipe_ │
          │     │           │ id (FK)      │
          │     │           │ global_      │
          │     │           │ recipe_id    │
          │     │           │ score        │
          │     │           └──────────────┘
          │     │
          │     │
    ┌─────┴─────┴──────┐         ┌─────────────────────┐
    │user_friendships  │         │  household_members  │
    │                  │         │                     │
    │ requester_user_  │         │ id                  │
    │ id (FK)          │         │ household_id (FK)   │
    │ target_user_     │         │ user_id (FK)        │
    │ id (FK)          │         │ role                │
    │ status           │         │                     │
    └──────────────────┘         └─────────────────────┘
                                          │
                                          │ N
                                          ↓
                                 ┌──────────────────┐
                                 │   households     │
                                 │                  │
                                 │ id               │
                                 │ name             │
                                 │ leader_id        │
                                 └──────────────────┘
```

---

## 4. API Changes

### 4.1 New Endpoints

#### User Recipes Controller (`/api/user-recipes`)
```
GET    /api/user-recipes
       - Get current user's recipes
       - Query: ?visibility=all|private|household|public&includeArchived=false

GET    /api/user-recipes/{id}
       - Get single user recipe

POST   /api/user-recipes
       - Create new recipe (local or linked to global)
       - Body: CreateUserRecipeDto

PUT    /api/user-recipes/{id}
       - Update recipe

DELETE /api/user-recipes/{id}
       - Delete recipe

POST   /api/user-recipes/{id}/publish
       - Publish local recipe to global_recipes
       - Creates GlobalRecipe, links UserRecipe to it
       - Returns updated UserRecipeDto

POST   /api/user-recipes/{id}/detach
       - Detach from global recipe (hard fork)
       - Copies global data to local fields, clears global_recipe_id

POST   /api/user-recipes/{id}/rate
       - Rate a recipe
       - Body: { rating: 0-10, comment?: string }

POST   /api/user-recipes/{id}/archive
POST   /api/user-recipes/{id}/restore
```

#### User Friendships Controller (`/api/friendships`)
```
GET    /api/friendships
       - Get all friendships (accepted, pending sent, pending received)
       - Returns: FriendshipListDto

GET    /api/friendships/friends
       - Get accepted friends only
       - Returns: List<UserDto>

POST   /api/friendships/request
       - Send friend request
       - Body: { targetUserId?: Guid, targetShareId?: string, message?: string }

POST   /api/friendships/{id}/respond
       - Accept or reject friend request
       - Body: { action: "accept" | "reject" }

DELETE /api/friendships/{id}
       - Remove friendship or cancel pending request
```

#### Activity Feed Controller (`/api/feed`)
```
GET    /api/feed
       - Get activity feed (friend activities)
       - Query: ?page=1&pageSize=20&types=rated,added,published
       - Returns: ActivityFeedPagedResult

GET    /api/feed/my-activity
       - Get current user's activity history
```

#### Household Aggregation Endpoint (`/api/households/{id}/combined-recipes`)
```
GET    /api/households/{id}/combined-recipes
       - Get aggregated recipes from all household members
       - Query: ?filterByMembers=userId1,userId2&minRating=4
       - Returns: List<AggregatedRecipeDto>

GET    /api/households/{id}/common-favorites
       - Get recipes rated highly by multiple household members
       - Query: ?minMembers=2&minAverageRating=4
       - Returns: List<CommonFavoriteDto>
```

### 4.2 Modified Endpoints

#### Global Recipes (`/api/global-recipes`)
```
GET    /api/global-recipes
       - MODIFY: Add filter for community vs HelloFresh
       - Query: ?source=all|hellofresh|community&...

POST   /api/global-recipes
       - DEPRECATE: Use /api/user-recipes/{id}/publish instead
       - Keep for backward compatibility during transition
```

#### Household Recipes (`/api/household-recipes`)
```
       - DEPRECATE ENTIRE CONTROLLER
       - Redirect to /api/user-recipes or /api/households/{id}/combined-recipes
       - Keep for backward compatibility during transition
```

### 4.3 New DTOs

```csharp
// === User Recipe DTOs ===

public class UserRecipeDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserDisplayName { get; set; }
    public string UserAvatarUrl { get; set; }

    // Recipe data (resolved from local or global)
    public string Name { get; set; }
    public string? Description { get; set; }
    public List<string> ImageUrls { get; set; }
    public object? Ingredients { get; set; }

    // Link status
    public Guid? GlobalRecipeId { get; set; }
    public string? GlobalRecipeName { get; set; }
    public bool IsLinkedToGlobal => GlobalRecipeId.HasValue;
    public bool IsPublished { get; set; }  // True if this recipe IS a published global recipe

    // Metadata
    public string Visibility { get; set; }  // private, household, friends, public
    public string? PersonalNotes { get; set; }
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Ratings
    public int? MyRating { get; set; }
    public double AverageRating { get; set; }
    public int RatingCount { get; set; }
}

public class CreateUserRecipeDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string>? ImageUrls { get; set; }
    public object? Ingredients { get; set; }
    public string? PersonalNotes { get; set; }
    public Guid? GlobalRecipeId { get; set; }  // Link to existing global recipe
    public string Visibility { get; set; } = "private";
}

public class UpdateUserRecipeDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string>? ImageUrls { get; set; }
    public object? Ingredients { get; set; }
    public string? PersonalNotes { get; set; }
    public string? Visibility { get; set; }
}

// === Friendship DTOs ===

public class UserFriendshipDto
{
    public Guid Id { get; set; }
    public Guid FriendUserId { get; set; }
    public string FriendDisplayName { get; set; }
    public string? FriendAvatarUrl { get; set; }
    public string FriendShareId { get; set; }
    public string Status { get; set; }  // pending_sent, pending_received, accepted
    public string? Message { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
}

public class FriendshipListDto
{
    public List<UserFriendshipDto> Friends { get; set; }        // accepted
    public List<UserFriendshipDto> PendingSent { get; set; }    // pending, I sent
    public List<UserFriendshipDto> PendingReceived { get; set; } // pending, I received
}

public class SendFriendRequestDto
{
    public Guid? TargetUserId { get; set; }
    public string? TargetShareId { get; set; }
    public string? Message { get; set; }
}

// === Activity Feed DTOs ===

public class ActivityFeedItemDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserDisplayName { get; set; }
    public string? UserAvatarUrl { get; set; }
    public string ActivityType { get; set; }  // rated, added, published, joined_household
    public string TargetType { get; set; }    // user_recipe, global_recipe, household
    public Guid TargetId { get; set; }

    // Denormalized data for display
    public string? RecipeName { get; set; }
    public string? RecipeImageUrl { get; set; }
    public int? RatingScore { get; set; }
    public string? HouseholdName { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class ActivityFeedPagedResult
{
    public List<ActivityFeedItemDto> Items { get; set; }
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

// === Household Aggregation DTOs ===

public class AggregatedRecipeDto
{
    public Guid UserRecipeId { get; set; }
    public Guid OwnerUserId { get; set; }
    public string OwnerDisplayName { get; set; }
    public string? OwnerAvatarUrl { get; set; }

    public string Name { get; set; }
    public string? Description { get; set; }
    public List<string> ImageUrls { get; set; }

    public Guid? GlobalRecipeId { get; set; }
    public bool IsLinkedToGlobal { get; set; }

    // Ratings from household members
    public Dictionary<string, int?> HouseholdRatings { get; set; }  // MemberName -> Rating
    public double HouseholdAverageRating { get; set; }

    public DateTime CreatedAt { get; set; }
}

public class CommonFavoriteDto
{
    public Guid GlobalRecipeId { get; set; }
    public string Name { get; set; }
    public string? ImageUrl { get; set; }

    public List<MemberRatingDto> MemberRatings { get; set; }
    public double AverageRating { get; set; }
    public int MembersWhoRated { get; set; }
}

public class MemberRatingDto
{
    public Guid UserId { get; set; }
    public string DisplayName { get; set; }
    public int Rating { get; set; }
}
```

---

## 5. Frontend Changes

### 5.1 Service Changes

#### New: `IUserRecipeService` (Replaces `IFoodService`)
```csharp
public interface IUserRecipeService
{
    // My Recipes
    Task<List<UserRecipeDto>> GetMyRecipesAsync(bool includeArchived = false);
    Task<UserRecipeDto?> GetRecipeAsync(Guid id);
    Task<UserRecipeDto> CreateRecipeAsync(CreateUserRecipeDto dto);
    Task<UserRecipeDto> UpdateRecipeAsync(Guid id, UpdateUserRecipeDto dto);
    Task DeleteRecipeAsync(Guid id);

    // Publishing
    Task<UserRecipeDto> PublishRecipeAsync(Guid id);
    Task<UserRecipeDto> DetachRecipeAsync(Guid id);

    // Rating
    Task<UserRecipeDto> RateRecipeAsync(Guid id, int rating, string? comment = null);

    // Archive
    Task<UserRecipeDto> ArchiveRecipeAsync(Guid id);
    Task<UserRecipeDto> RestoreRecipeAsync(Guid id);

    // Caching
    List<UserRecipeDto>? CachedRecipes { get; }
    event Action OnRecipesChanged;
}
```

#### New: `IFriendshipService`
```csharp
public interface IFriendshipService
{
    Task<FriendshipListDto> GetFriendshipsAsync();
    Task<List<UserDto>> GetFriendsAsync();
    Task<UserFriendshipDto> SendFriendRequestAsync(SendFriendRequestDto dto);
    Task<UserFriendshipDto> RespondToRequestAsync(Guid requestId, string action);
    Task RemoveFriendshipAsync(Guid friendshipId);

    // Caching
    FriendshipListDto? CachedFriendships { get; }
    event Action OnFriendshipsChanged;
}
```

#### New: `IActivityFeedService`
```csharp
public interface IActivityFeedService
{
    Task<ActivityFeedPagedResult> GetFeedAsync(int page = 1, int pageSize = 20);
    Task<ActivityFeedPagedResult> GetMyActivityAsync(int page = 1, int pageSize = 20);

    // Real-time updates (future)
    event Action<ActivityFeedItemDto> OnNewActivity;
}
```

#### Modified: `IHouseholdStateService`
```csharp
public interface IHouseholdStateService
{
    // Existing properties
    HouseholdDto? CurrentHousehold { get; }
    List<HouseholdDto> UserHouseholds { get; }
    bool HasHousehold { get; }

    // NEW: Aggregated recipes
    Task<List<AggregatedRecipeDto>> GetHouseholdRecipesAsync(Guid? householdId = null);
    Task<List<CommonFavoriteDto>> GetCommonFavoritesAsync(Guid? householdId = null);

    // NEW: Optional household mode
    bool IsHouseholdModeEnabled { get; }  // User can disable household view
}
```

### 5.2 Page Changes

#### `Index.razor` (Home) - Major Refactor
```
BEFORE: Shows household recipes, blocked if no household
AFTER:  Shows personal recipe list OR social feed

New Layout:
┌─────────────────────────────────────┐
│  Tab Bar: [My Recipes] [Feed]       │
├─────────────────────────────────────┤
│                                     │
│  [My Recipes Tab]                   │
│  - User's personal recipe list      │
│  - Sort/filter options              │
│  - No household required!           │
│                                     │
│  [Feed Tab]                         │
│  - Friend activity cards            │
│  - "Sarah rated Spicy Tacos 9/10"   │
│  - "John added a new recipe"        │
│  - Click to view recipe             │
│                                     │
└─────────────────────────────────────┘
```

#### `RecipeDetail.razor` (New, replaces `FoodDetails.razor`)
```
- Shows user recipe detail
- "Publish" button for local recipes
- "Detach" button for linked recipes
- Visibility selector (private/household/friends/public)
- Rating from current user
- If linked: Shows "Based on [Global Recipe Name]"
- Personal notes section
```

#### `Browse.razor` - Modify
```
BEFORE: Separate tabs for HelloFresh and Community
AFTER:  Unified browse with source filter

Filters:
- Source: All | HelloFresh | Community
- (All other existing filters)

Community recipes = GlobalRecipes where is_hellofresh = false
```

#### `BrowseDetail.razor` - Modify
```
- Add "Add to My Recipes" button (was "Add to Household")
- Show who published it (if community recipe)
- Show publishing user's profile link
```

#### `Friends.razor` (New Page)
```
┌─────────────────────────────────────┐
│  Friends                            │
├─────────────────────────────────────┤
│  [Search by Share ID or Email]      │
│                                     │
│  Pending Requests (3)               │
│  ┌─────────────────────────────────┐│
│  │ 👤 Sarah wants to be friends    ││
│  │ [Accept] [Decline]              ││
│  └─────────────────────────────────┘│
│                                     │
│  My Friends                         │
│  ┌─────────────────────────────────┐│
│  │ 👤 John Doe                     ││
│  │    @john123 • 15 recipes        ││
│  └─────────────────────────────────┘│
│  ┌─────────────────────────────────┐│
│  │ 👤 Jane Smith                   ││
│  │    @jane456 • 23 recipes        ││
│  └─────────────────────────────────┘│
│                                     │
└─────────────────────────────────────┘
```

#### `Household.razor` - Modify
```
BEFORE: Primary recipe management
AFTER:  Household social features

New Layout:
┌─────────────────────────────────────┐
│  [Household Name] Settings ⚙️       │
├─────────────────────────────────────┤
│                                     │
│  Members (4)                        │
│  👤 Mom (Leader) | 👤 Dad           │
│  👤 Kid1        | 👤 Kid2           │
│                                     │
│  Family Cookbook (Aggregated View)  │
│  ┌─────────────────────────────────┐│
│  │ Recipe cards with owner badges  ││
│  │ "🍕 Pizza" - Added by Mom       ││
│  │ "🌮 Tacos" - Added by Dad       ││
│  └─────────────────────────────────┘│
│                                     │
│  Common Favorites                   │
│  Recipes everyone loves (4+ rating) │
│                                     │
└─────────────────────────────────────┘
```

#### `AddFood.razor` - Modify to `AddRecipe.razor`
```
- Remove household requirement
- Add visibility selector
- "Add to My Recipes" instead of "Add to Household"
```

#### `MainLayout.razor` - Modify Navigation
```
BEFORE:
[Home] [Browse] [+] [Households] [Archive]

AFTER:
[Home] [Browse] [+] [Friends] [Profile]

- "Households" moves to Profile/Settings
- New "Friends" tab
- Profile includes user settings + household management
```

### 5.3 Component Changes

#### `RecipeCard.razor` (New/Refactor)
```razor
- Shows recipe with owner badge
- Displays: Image, Name, Rating, Owner Avatar+Name
- Click to navigate to detail
- Visual indicator for:
  - 🔗 Linked to global recipe
  - 🌍 Published (is a global recipe)
  - 🏠 Household visible
  - 👥 Friend visible
```

#### `ActivityFeedCard.razor` (New)
```razor
- Shows friend activity
- Avatar + Name + Action + Target
- Example: "👤 Sarah rated 🍕 Pizza 9/10"
- Click to view the recipe
```

#### `FriendRequestCard.razor` (New)
```razor
- Shows pending friend request
- Avatar + Name + Message
- Accept/Decline buttons
```

#### `VisibilitySelector.razor` (New)
```razor
- Dropdown for recipe visibility
- Options: Private, Household, Friends, Public
- Icons for each level
```

### 5.4 State Management Changes

#### Remove Household Blocking
```csharp
// BEFORE (Index.razor)
@if (!HouseholdState.HasHousehold)
{
    <EmptyState Message="No household selected" />
}

// AFTER (Index.razor)
// Always show user's recipes, no household required
@if (!_recipes.Any())
{
    <EmptyState Message="No recipes yet. Add your first recipe!" />
}
```

#### Add Personal Recipe State
```csharp
// New service handles user's personal recipes
// HouseholdStateService only handles household aggregation views
```

---

## 6. Migration Strategy

### Phase 1: Database Migration (Non-Breaking)

1. **Create new tables** (`user_recipes`, `user_friendships`, `activity_feed`)
2. **Add new columns** to existing tables
3. **Migrate data** from `household_recipes` to `user_recipes`:
   ```sql
   INSERT INTO user_recipes (id, user_id, global_recipe_id, local_title, ...)
   SELECT id, added_by_user_id, global_recipe_id, local_title, ...
   FROM household_recipes
   WHERE added_by_user_id IS NOT NULL;
   ```
4. **Keep old tables** for backward compatibility

### Phase 2: API Migration (Parallel Endpoints)

1. **Add new endpoints** alongside existing ones
2. **Deprecate old endpoints** with warnings in responses
3. **Update frontend** to use new endpoints
4. **Monitor** for old endpoint usage

### Phase 3: Frontend Migration

1. **Create new components** (ActivityFeedCard, FriendRequestCard, etc.)
2. **Refactor services** (IUserRecipeService, IFriendshipService)
3. **Update pages** incrementally
4. **Test** each page independently

### Phase 4: Cleanup

1. **Remove deprecated endpoints**
2. **Drop old tables** (after data verification)
3. **Remove legacy components**

### Data Migration Script (Detailed)

```sql
-- Step 1: Create user_recipes from household_recipes
INSERT INTO user_recipes (
    id,
    user_id,
    global_recipe_id,
    local_title,
    local_description,
    local_ingredients,
    local_image_url,
    personal_notes,
    is_archived,
    archived_date,
    visibility,
    created_at,
    updated_at
)
SELECT
    hr.id,
    COALESCE(hr.added_by_user_id, h.leader_id) as user_id,  -- Fallback to household leader
    hr.global_recipe_id,
    hr.local_title,
    hr.local_description,
    hr.local_ingredients,
    hr.local_image_url,
    hr.personal_notes,
    hr.is_archived,
    hr.archived_date,
    CASE
        WHEN hr.is_public THEN 'public'
        ELSE 'household'
    END as visibility,
    hr.created_at,
    hr.updated_at
FROM household_recipes hr
JOIN households h ON hr.household_id = h.id
WHERE hr.added_by_user_id IS NOT NULL OR h.leader_id IS NOT NULL;

-- Step 2: Update ratings to reference user_recipes
UPDATE ratings r
SET user_recipe_id = r.household_recipe_id
WHERE r.household_recipe_id IS NOT NULL
  AND EXISTS (SELECT 1 FROM user_recipes ur WHERE ur.id = r.household_recipe_id);

-- Step 3: Migrate household friendships to user friendships (optional)
-- This creates friendships between household leaders
INSERT INTO user_friendships (
    requester_user_id,
    target_user_id,
    status,
    message,
    created_at,
    responded_at
)
SELECT DISTINCT
    rh.leader_id,
    th.leader_id,
    hf.status,
    hf.message,
    hf.created_at,
    hf.responded_at
FROM household_friendships hf
JOIN households rh ON hf.requester_household_id = rh.id
JOIN households th ON hf.target_household_id = th.id
WHERE rh.leader_id IS NOT NULL AND th.leader_id IS NOT NULL
  AND rh.leader_id != th.leader_id;
```

---

## 7. Implementation Phases

### Phase 1: Foundation (Week 1-2)
**Goal**: Database changes and core backend services

- [ ] Create database migrations for new tables
- [ ] Create `UserRecipe` entity and `UserRecipeService`
- [ ] Create `UserFriendship` entity and `UserFriendshipService`
- [ ] Create `ActivityFeed` entity and `ActivityFeedService`
- [ ] Write data migration scripts
- [ ] Add new API endpoints (parallel to existing)
- [ ] Unit tests for new services

### Phase 2: API Completion (Week 2-3)
**Goal**: Complete all new API functionality

- [ ] Implement `/api/user-recipes` controller
- [ ] Implement `/api/friendships` controller
- [ ] Implement `/api/feed` controller
- [ ] Implement `/api/households/{id}/combined-recipes`
- [ ] Update `/api/global-recipes` for unified browse
- [ ] Integration tests for new endpoints
- [ ] API documentation updates

### Phase 3: Frontend Services (Week 3-4)
**Goal**: New frontend services and state management

- [ ] Create `IUserRecipeService` and implementation
- [ ] Create `IFriendshipService` and implementation
- [ ] Create `IActivityFeedService` and implementation
- [ ] Update `ApiClient` with new endpoints
- [ ] Modify `HouseholdStateService` for aggregation
- [ ] Remove household blocking from app initialization

### Phase 4: Frontend Pages (Week 4-6)
**Goal**: Update all user-facing pages

- [ ] Refactor `Index.razor` with tabs (My Recipes / Feed)
- [ ] Create `RecipeDetail.razor` (replaces FoodDetails)
- [ ] Create `Friends.razor` page
- [ ] Update `Browse.razor` with unified source filter
- [ ] Update `BrowseDetail.razor` with "Add to My Recipes"
- [ ] Update `Household.razor` for aggregated view
- [ ] Update `AddFood.razor` → `AddRecipe.razor`
- [ ] Update `MainLayout.razor` navigation

### Phase 5: Components & Polish (Week 6-7)
**Goal**: New components and UI refinement

- [ ] Create `ActivityFeedCard.razor`
- [ ] Create `FriendRequestCard.razor`
- [ ] Create `VisibilitySelector.razor`
- [ ] Create `RecipeCard.razor` with owner badges
- [ ] Update `NotificationBell.razor` for friend requests
- [ ] CSS/styling updates
- [ ] Responsive design verification

### Phase 6: Testing & Migration (Week 7-8)
**Goal**: End-to-end testing and data migration

- [ ] End-to-end testing of all flows
- [ ] Run data migration on staging
- [ ] Verify data integrity
- [ ] Performance testing
- [ ] Bug fixes

### Phase 7: Cleanup (Week 8+)
**Goal**: Remove deprecated code

- [ ] Remove old `HouseholdRecipesController`
- [ ] Remove old frontend services
- [ ] Drop deprecated database tables
- [ ] Final documentation

---

## 8. Risk Assessment

### High Risk Items

| Risk | Impact | Mitigation |
|------|--------|------------|
| Data loss during migration | Critical | Multiple backups, staged rollout, verification scripts |
| Breaking existing users | High | Parallel endpoints, feature flags, gradual rollout |
| Performance degradation | High | Aggregation queries optimized, caching strategy |

### Medium Risk Items

| Risk | Impact | Mitigation |
|------|--------|------------|
| Complex merging of ratings | Medium | Clear rules for rating attribution |
| User confusion with new UI | Medium | Onboarding tooltips, help documentation |
| Increased API complexity | Medium | Clear documentation, TypeScript types |

### Low Risk Items

| Risk | Impact | Mitigation |
|------|--------|------------|
| Friend spam | Low | Rate limiting, block feature |
| Storage increase | Low | Monitor growth, optimize queries |

---

## Appendix A: Key Design Decisions

### Decision 1: User Recipes vs. Household Recipes
**Chosen**: User-owned recipes with household aggregation view
**Rationale**: Preserves user data ownership, enables true portability

### Decision 2: Publishing Flow
**Chosen**: Explicit "Publish" action creates GlobalRecipe
**Rationale**: User intent is clear, prevents accidental sharing

### Decision 3: Visibility Levels
**Chosen**: Four levels (private, household, friends, public)
**Rationale**: Granular control matches social app expectations

### Decision 4: Activity Feed Storage
**Chosen**: Denormalized `activity_feed` table
**Rationale**: Fast reads, easy pagination, acceptable write cost

### Decision 5: Friendship Model
**Chosen**: User-to-user (not household-to-household)
**Rationale**: More intuitive, aligns with social app patterns

---

## Appendix B: API Endpoint Summary

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/api/user-recipes` | Yes | Get my recipes |
| POST | `/api/user-recipes` | Yes | Create recipe |
| PUT | `/api/user-recipes/{id}` | Yes | Update recipe |
| DELETE | `/api/user-recipes/{id}` | Yes | Delete recipe |
| POST | `/api/user-recipes/{id}/publish` | Yes | Publish to global |
| POST | `/api/user-recipes/{id}/detach` | Yes | Detach from global |
| POST | `/api/user-recipes/{id}/rate` | Yes | Rate recipe |
| GET | `/api/friendships` | Yes | Get all friendships |
| POST | `/api/friendships/request` | Yes | Send request |
| POST | `/api/friendships/{id}/respond` | Yes | Accept/reject |
| GET | `/api/feed` | Yes | Get activity feed |
| GET | `/api/households/{id}/combined-recipes` | Yes | Aggregated view |

---

## Appendix C: File Changes Summary

### New Files (Backend)
- `Models/UserRecipe.cs`
- `Models/UserFriendship.cs`
- `Models/ActivityFeed.cs`
- `Services/IUserRecipeService.cs`
- `Services/UserRecipeService.cs`
- `Services/IUserFriendshipService.cs`
- `Services/UserFriendshipService.cs`
- `Services/IActivityFeedService.cs`
- `Services/ActivityFeedService.cs`
- `Controllers/UserRecipesController.cs`
- `Controllers/FriendshipsController.cs`
- `Controllers/FeedController.cs`
- `DTOs/UserRecipeDTOs.cs`
- `DTOs/FriendshipDTOs.cs`
- `DTOs/ActivityFeedDTOs.cs`
- `Migrations/[timestamp]_AddUserCentricTables.cs`

### New Files (Frontend)
- `Services/IUserRecipeService.cs`
- `Services/UserRecipeService.cs`
- `Services/IFriendshipService.cs`
- `Services/FriendshipService.cs`
- `Services/IActivityFeedService.cs`
- `Services/ActivityFeedService.cs`
- `Pages/Friends.razor`
- `Pages/RecipeDetail.razor`
- `Components/ActivityFeedCard.razor`
- `Components/FriendRequestCard.razor`
- `Components/VisibilitySelector.razor`
- `DTOs/UserRecipeDTOs.cs`
- `DTOs/FriendshipDTOs.cs`
- `DTOs/ActivityFeedDTOs.cs`

### Modified Files (Backend)
- `Data/AppDbContext.cs`
- `Models/User.cs`
- `Models/GlobalRecipe.cs`
- `Models/Rating.cs`
- `Controllers/GlobalRecipesController.cs`
- `Controllers/HouseholdsController.cs`

### Modified Files (Frontend)
- `Pages/Index.razor`
- `Pages/Browse.razor`
- `Pages/BrowseDetail.razor`
- `Pages/Household.razor`
- `Pages/AddFood.razor` → `AddRecipe.razor`
- `Shared/MainLayout.razor`
- `Services/HouseholdStateService.cs`
- `ApiClient/ApiClient.cs`
- `ApiClient/IApiClient.cs`
- `App.razor`

### Deprecated/Removed Files
- `Controllers/HouseholdRecipesController.cs` (deprecated, then removed)
- `Services/IFoodService.cs` (replaced by IUserRecipeService)
- `Services/FoodService.cs` (replaced by UserRecipeService)
- `Pages/FoodDetails.razor` (replaced by RecipeDetail.razor)

---

*Document Version: 1.0*
*Last Updated: December 2024*
*Author: AI Assistant*
