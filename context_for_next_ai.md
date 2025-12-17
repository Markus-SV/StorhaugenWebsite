# Context Prompt for Next AI - Recipe Edit & AddFood Page Improvements

## Overview

You are working on **Storhaugen Eats**, a Blazor WASM + ASP.NET Core recipe management app. The app was recently refactored from a "Household" system to a "Collection" system for sharing recipes. The previous AI implemented collection visibility settings and management pages.

**Your primary task:** Implement recipe editing functionality and improve the AddFood page UX.

---

## App Architecture Summary

### Tech Stack
- **Frontend:** Blazor WebAssembly (MudBlazor UI)
- **Backend:** ASP.NET Core Web API + Entity Framework Core
- **Database:** PostgreSQL
- **Language:** Norwegian UI text

### Key Service Files
- `StorhaugenEats.API/Services/UserRecipeService.cs` - Recipe business logic
- `StorhaugenWebsite/Services/IUserRecipeService.cs` - Frontend recipe service interface
- `StorhaugenWebsite/ApiClient/ApiClient.cs` - HTTP client for API calls
- `StorhaugenWebsite.Shared/DTOs/UserRecipeDTOs.cs` - Shared DTOs

### Key Pages
- `/add` - `AddFood.razor` - Create new recipes
- `/food/{id}` - `FoodDetails.razor` - View recipe details
- `/collections/{id}` - `CollectionDetails.razor` - Collection management (just implemented)
- `/` - `CookBook.razor` - Recipe list/home

---

## Current State: What Needs Implementation

### 1. RECIPE EDITING (Critical - Currently Broken)

**Problem:** The "Edit" button on FoodDetails.razor shows a toast message "Redigering kommer snart" (Coming soon). Users cannot edit their recipes after creation.

**Location of placeholder:**
```csharp
// FoodDetails.razor:651
private void EditRecipe() => Snackbar.Add("Redigering kommer snart", Severity.Info);
```

**What needs to happen:**
- Create an Edit page/modal that pre-populates with existing recipe data
- Allow editing: Name, Description, Images, Visibility, Prep time, Servings, Difficulty, Cuisine
- Handle image changes (add new, remove existing)
- Save changes via `UpdateUserRecipeAsync`

**Recommended approach - Option A (New Page):**
Create `/food/{id}/edit` → `EditFood.razor` that reuses AddFood structure but:
- Pre-loads existing recipe data
- Shows existing images with remove capability
- Calls `UpdateRecipeAsync` instead of `CreateRecipeAsync`

**Recommended approach - Option B (Modal):**
Add edit mode to FoodDetails.razor with inline editing or show a dialog with form fields.

### 2. ADDFOOD PAGE - INGREDIENTS UI

**Current state:** Ingredients are stored in `_newFood.Description` as plain text. The Ingredients field is a simple multiline text field.

**Problem:**
- No structured ingredient entry
- HelloFresh imports have structured ingredient data (JSON array with name, amount, unit, image)
- Manual recipe entry doesn't support structured ingredients

**Data structure for ingredients (from HelloFresh):**
```json
[
  { "name": "Gulrot", "amount": "2", "unit": "stk", "image": "/path/to/image.png" },
  { "name": "Hvitløk", "amount": "1", "unit": "fedd", "image": null }
]
```

**What user wants:**
- Ingredients section should be **hidden by default** for manual recipes
- Expand/collapse ingredients section when needed
- Most users won't add structured ingredients unless importing from HelloFresh/Browse

**Suggested implementation:**
- Keep the existing Description field for simple text
- Add an expandable "Structured Ingredients" section (collapsed by default)
- Allow adding ingredient rows with: Name, Amount, Unit
- When submitting, combine into Ingredients JSON if any are added

### 3. ADDFOOD PAGE - FIELD ORGANIZATION

**Current layout:**
1. Mode toggle (HelloFresh / Annet)
2. Name + Description fields
3. Expandable "Tilleggsinfo" (prep time, servings, difficulty, cuisine)
4. Images section
5. Sharing (Collection selection, public toggle)
6. Ratings section
7. Submit button

**What works well:**
- HelloFresh scanning/OCR for auto-fill
- Collection selection with quick-create
- Member rating system

**What could be improved:**
- Metadata (prep time, servings) in expandable panel is good - keep it
- Ingredients should follow similar pattern (collapsed by default)
- Consider: Only show "Tilleggsinfo" expanded if importing from HelloFresh (has the data)

---

## Key DTOs for Reference

### UserRecipeDto (Read)
```csharp
public class UserRecipeDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string> ImageUrls { get; set; }
    public object? Ingredients { get; set; }  // JSON array or null
    public string? PersonalNotes { get; set; }
    public string Visibility { get; set; }  // "private", "public", "friends"

    // Metadata
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public int? Servings { get; set; }
    public string? Difficulty { get; set; }
    public string? Cuisine { get; set; }
    public List<string> RecipeTags { get; set; }
    public object? NutritionData { get; set; }

    // Ratings
    public int? MyRating { get; set; }
    public double AverageRating { get; set; }
    public Dictionary<string, int?> MemberRatings { get; set; }

    // HelloFresh linking
    public Guid? GlobalRecipeId { get; set; }
    public bool IsHellofresh { get; set; }
    public bool IsPublished { get; set; }
}
```

### CreateUserRecipeDto
```csharp
public class CreateUserRecipeDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string>? ImageUrls { get; set; }
    public object? Ingredients { get; set; }
    public string? PersonalNotes { get; set; }
    public Guid? GlobalRecipeId { get; set; }
    public string Visibility { get; set; } = "private";

    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public int? Servings { get; set; }
    public string? Difficulty { get; set; }
    public string? Cuisine { get; set; }

    public Dictionary<Guid, int>? MemberRatings { get; set; }
}
```

### UpdateUserRecipeDto
```csharp
public class UpdateUserRecipeDto
{
    public string? Name { get; set; }
    public string? Description { get; set; }
    public List<string>? ImageUrls { get; set; }
    public object? Ingredients { get; set; }
    public string? PersonalNotes { get; set; }
    public string? Visibility { get; set; }
    public List<Guid>? TagIds { get; set; }
    public List<Guid>? CollectionIds { get; set; }
}
```

---

## Existing API Methods Available

### IUserRecipeService (Frontend)
```csharp
Task<UserRecipeDto> CreateRecipeAsync(CreateUserRecipeDto dto);
Task<UserRecipeDto> UpdateRecipeAsync(Guid id, UpdateUserRecipeDto dto);
Task<UserRecipeDto?> GetRecipeAsync(Guid id);
Task<UserRecipeDto> RateRecipeAsync(Guid id, int rating, string? comment = null);
Task ArchiveRecipeAsync(Guid id);
```

### IApiClient (HTTP)
```csharp
Task<UserRecipeDto> UpdateUserRecipeAsync(Guid id, UpdateUserRecipeDto dto);
Task<UploadImageResultDto> UploadImageAsync(byte[] imageData, string fileName);
Task DeleteImageAsync(string fileName);
```

---

## UI/UX Patterns to Follow

### Form Section Pattern (used throughout app)
```html
<div class="form-section">
    <p class="form-section-title">Section Title</p>
    <!-- Content -->
</div>
```

### Expandable Panel Pattern
```html
<MudExpansionPanels Elevation="0">
    <MudExpansionPanel Text="Title" Style="background: var(--mud-palette-background); border-radius: 12px;">
        <!-- Content -->
    </MudExpansionPanel>
</MudExpansionPanels>
```

### Button Styling
```html
<MudButton Variant="Variant.Filled" Color="Color.Primary" Style="border-radius: 12px;">
```

---

## Implementation Checklist

### Phase 1: Recipe Edit (High Priority)
- [ ] Create EditFood.razor page (or modal)
- [ ] Route: `/food/{id}/edit` or inline in FoodDetails
- [ ] Pre-load recipe data on initialization
- [ ] Handle existing images (show with remove buttons)
- [ ] Handle new image uploads
- [ ] Call `UpdateRecipeAsync` on save
- [ ] Navigate back to FoodDetails after save
- [ ] Update the "Rediger" button in FoodDetails to navigate/open editor

### Phase 2: AddFood Improvements (Medium Priority)
- [ ] Make ingredients section collapsible (hidden by default)
- [ ] Add optional structured ingredient input rows
- [ ] Keep metadata section (Tilleggsinfo) as-is (already expandable)
- [ ] Consider auto-expanding Tilleggsinfo when importing from HelloFresh

### Phase 3: Missing Fields in UpdateUserRecipeDto (Optional)
The UpdateUserRecipeDto doesn't currently include:
- PrepTimeMinutes, CookTimeMinutes, Servings, Difficulty, Cuisine, NutritionData

If editing these is required, extend the DTO and backend handler.

---

## Important Considerations

### Image Handling
- Existing images come as URLs from `_recipe.ImageUrls`
- New images need to be uploaded via `UploadImageAsync`
- When updating, combine: kept existing URLs + newly uploaded URLs
- Deleted images should be removed from the list (optionally call `DeleteImageAsync` to clean storage)

### Draft System
AddFood.razor has a draft system (`FoodService.DraftRecipe`) for copying recipes. This could potentially be reused for editing by loading the recipe as a "draft" and modifying the save logic.

### Visibility Options
- "private" - Only owner can see
- "public" - Anyone can see
- "friends" - Owner's friends can see
- Legacy "household" - Treated as private (backwards compatibility)

### Collection Integration
When editing, consider:
- Recipe may belong to collections
- UpdateUserRecipeDto has `CollectionIds` to replace collection membership
- Might want to show which collections recipe is in and allow changing

---

## Files to Modify

1. **FoodDetails.razor** - Change EditRecipe() from placeholder to navigation/modal
2. **Create EditFood.razor** (new file) - Or add edit mode to existing page
3. **AddFood.razor** - Refactor ingredients section to be collapsible
4. **UpdateUserRecipeDto** - Possibly extend with metadata fields

---

## Testing Considerations

- Test editing a recipe with images (keep some, add some, remove some)
- Test editing a HelloFresh-imported recipe (has structured ingredients)
- Test editing a manually created recipe (text-only description)
- Test visibility changes during edit
- Test that ratings/notes are preserved after edit

---

## Branch & Git

Working branch: `claude/implement-refactoring-fixes-JyTpK`

Always commit with descriptive messages in this format:
```
feat: add recipe editing functionality

- Create EditFood.razor page for editing recipes
- Pre-populate form with existing recipe data
- Handle image additions and removals
- Update FoodDetails.razor to navigate to edit page
```

---

## Summary of Priority

1. **MUST DO:** Recipe editing - users literally cannot fix typos or update recipes
2. **SHOULD DO:** Collapsible ingredients section in AddFood
3. **NICE TO HAVE:** Structured ingredient input for manual recipes
4. **NICE TO HAVE:** Extend UpdateUserRecipeDto with metadata fields
