using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;

namespace StorhaugenEats.API.Tests;

/// <summary>
/// Factory for creating in-memory database contexts for testing.
/// </summary>
public static class TestDbContextFactory
{
    public static AppDbContext CreateInMemoryContext(string? databaseName = null)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName ?? Guid.NewGuid().ToString())
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static async Task<AppDbContext> CreateSeededContextAsync(string? databaseName = null)
    {
        var context = CreateInMemoryContext(databaseName);
        await SeedTestDataAsync(context);
        return context;
    }

    private static async Task SeedTestDataAsync(AppDbContext context)
    {
        // Create test users
        var user1 = new Models.User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Email = "user1@test.com",
            DisplayName = "Test User 1",
            CreatedAt = DateTime.UtcNow
        };

        var user2 = new Models.User
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Email = "user2@test.com",
            DisplayName = "Test User 2",
            CreatedAt = DateTime.UtcNow
        };

        context.Users.AddRange(user1, user2);

        // Create test household
        var household = new Models.Household
        {
            Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
            Name = "Test Household",
            CreatedById = user1.Id,
            CreatedAt = DateTime.UtcNow
        };

        context.Households.Add(household);

        // Create test global recipe
        var globalRecipe = new Models.GlobalRecipe
        {
            Id = Guid.Parse("gggggggg-gggg-gggg-gggg-gggggggggggg"),
            Title = "Test Global Recipe",
            Description = "A test recipe for testing",
            CreatedAt = DateTime.UtcNow
        };

        context.GlobalRecipes.Add(globalRecipe);

        // Create test household recipes (to be migrated)
        var householdRecipe1 = new Models.HouseholdRecipe
        {
            Id = Guid.Parse("hhhhhhhh-1111-1111-1111-hhhhhhhhhhhh"),
            HouseholdId = household.Id,
            GlobalRecipeId = globalRecipe.Id,
            LocalTitle = "Local Recipe 1",
            LocalDescription = "Description 1",
            AddedByUserId = user1.Id,
            IsPublic = false,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var householdRecipe2 = new Models.HouseholdRecipe
        {
            Id = Guid.Parse("hhhhhhhh-2222-2222-2222-hhhhhhhhhhhh"),
            HouseholdId = household.Id,
            GlobalRecipeId = globalRecipe.Id,
            LocalTitle = "Local Recipe 2",
            LocalDescription = "Description 2",
            AddedByUserId = user2.Id,
            IsPublic = true,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.HouseholdRecipes.AddRange(householdRecipe1, householdRecipe2);

        // Create test ratings (pointing to household recipes)
        var rating1 = new Models.Rating
        {
            Id = Guid.Parse("rrrrrrrr-1111-1111-1111-rrrrrrrrrrrr"),
            HouseholdRecipeId = householdRecipe1.Id,
            UserId = user1.Id,
            Score = 8,
            Comment = "Great recipe!",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var rating2 = new Models.Rating
        {
            Id = Guid.Parse("rrrrrrrr-2222-2222-2222-rrrrrrrrrrrr"),
            HouseholdRecipeId = householdRecipe2.Id,
            UserId = user2.Id,
            Score = 7,
            Comment = "Pretty good",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        context.Ratings.AddRange(rating1, rating2);

        await context.SaveChangesAsync();
    }
}
