using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StorhaugenEats.API.Services;
using Xunit;

namespace StorhaugenEats.API.Tests.Services;

public class DataMigrationServiceTests
{
    private readonly Mock<ILogger<DataMigrationService>> _loggerMock;

    public DataMigrationServiceTests()
    {
        _loggerMock = new Mock<ILogger<DataMigrationService>>();
    }

    [Fact]
    public async Task MigrateHouseholdRecipesToUserRecipes_DryRun_ShouldNotMakeChanges()
    {
        // Arrange
        var context = await TestDbContextFactory.CreateSeededContextAsync();
        var service = new DataMigrationService(context, _loggerMock.Object);

        var initialUserRecipeCount = await context.UserRecipes.CountAsync();

        // Act
        var result = await service.MigrateHouseholdRecipesToUserRecipesAsync(dryRun: true);

        // Assert
        result.Success.Should().BeTrue();
        result.WasDryRun.Should().BeTrue();
        result.ItemsProcessed.Should().Be(2);
        result.ItemsMigrated.Should().Be(2);
        result.ItemsFailed.Should().Be(0);

        // Verify no actual changes were made
        var finalUserRecipeCount = await context.UserRecipes.CountAsync();
        finalUserRecipeCount.Should().Be(initialUserRecipeCount);
    }

    [Fact]
    public async Task MigrateHouseholdRecipesToUserRecipes_RealRun_ShouldCreateUserRecipes()
    {
        // Arrange
        var context = await TestDbContextFactory.CreateSeededContextAsync();
        var service = new DataMigrationService(context, _loggerMock.Object);

        // Act
        var result = await service.MigrateHouseholdRecipesToUserRecipesAsync(dryRun: false);

        // Assert
        result.Success.Should().BeTrue();
        result.WasDryRun.Should().BeFalse();
        result.ItemsMigrated.Should().Be(2);

        // Verify user recipes were created
        var userRecipes = await context.UserRecipes.ToListAsync();
        userRecipes.Should().HaveCount(2);

        // Verify the recipes have correct data
        var firstRecipe = userRecipes.First(r => r.LocalTitle == "Local Recipe 1");
        firstRecipe.UserId.Should().Be(Guid.Parse("11111111-1111-1111-1111-111111111111"));
        firstRecipe.Visibility.Should().Be("household"); // IsPublic = false maps to "household"

        var secondRecipe = userRecipes.First(r => r.LocalTitle == "Local Recipe 2");
        secondRecipe.UserId.Should().Be(Guid.Parse("22222222-2222-2222-2222-222222222222"));
        secondRecipe.Visibility.Should().Be("public"); // IsPublic = true maps to "public"
    }

    [Fact]
    public async Task MigrateHouseholdRecipesToUserRecipes_ShouldSkipAlreadyMigrated()
    {
        // Arrange
        var context = await TestDbContextFactory.CreateSeededContextAsync();
        var service = new DataMigrationService(context, _loggerMock.Object);

        // First migration
        await service.MigrateHouseholdRecipesToUserRecipesAsync(dryRun: false);

        // Act - Second migration
        var result = await service.MigrateHouseholdRecipesToUserRecipesAsync(dryRun: false);

        // Assert
        result.Success.Should().BeTrue();
        result.ItemsMigrated.Should().Be(0);
        result.ItemsSkipped.Should().Be(2);

        // Verify no duplicates
        var userRecipes = await context.UserRecipes.ToListAsync();
        userRecipes.Should().HaveCount(2);
    }

    [Fact]
    public async Task MigrateRatings_ShouldLinkToUserRecipes()
    {
        // Arrange
        var context = await TestDbContextFactory.CreateSeededContextAsync();
        var service = new DataMigrationService(context, _loggerMock.Object);

        // First migrate recipes
        await service.MigrateHouseholdRecipesToUserRecipesAsync(dryRun: false);

        // Act - Migrate ratings
        var result = await service.MigrateRatingsAsync(dryRun: false);

        // Assert
        result.Success.Should().BeTrue();
        result.ItemsMigrated.Should().Be(2);

        // Verify ratings now have UserRecipeId
        var ratings = await context.Ratings.ToListAsync();
        ratings.All(r => r.UserRecipeId != null).Should().BeTrue();
        ratings.All(r => r.UserRecipeId == r.HouseholdRecipeId).Should().BeTrue();
    }

    [Fact]
    public async Task MigrateRatings_DryRun_ShouldNotMakeChanges()
    {
        // Arrange
        var context = await TestDbContextFactory.CreateSeededContextAsync();
        var service = new DataMigrationService(context, _loggerMock.Object);

        // First migrate recipes
        await service.MigrateHouseholdRecipesToUserRecipesAsync(dryRun: false);

        // Act - Dry run rating migration
        var result = await service.MigrateRatingsAsync(dryRun: true);

        // Assert
        result.Success.Should().BeTrue();
        result.WasDryRun.Should().BeTrue();
        result.ItemsMigrated.Should().Be(2);

        // Verify no actual changes
        var ratings = await context.Ratings.ToListAsync();
        ratings.All(r => r.UserRecipeId == null).Should().BeTrue();
    }

    [Fact]
    public async Task GetMigrationStats_ShouldReturnCorrectCounts()
    {
        // Arrange
        var context = await TestDbContextFactory.CreateSeededContextAsync();
        var service = new DataMigrationService(context, _loggerMock.Object);

        // Act
        var stats = await service.GetMigrationStatsAsync();

        // Assert
        stats.TotalHouseholdRecipes.Should().Be(2);
        stats.TotalUserRecipes.Should().Be(0);
        stats.HouseholdRecipesNotMigrated.Should().Be(2);
        stats.RatingsWithHouseholdRecipeId.Should().Be(2);
        stats.RatingsWithUserRecipeId.Should().Be(0);
        stats.MigrationComplete.Should().BeFalse();
    }

    [Fact]
    public async Task GetMigrationStats_AfterMigration_ShouldShowComplete()
    {
        // Arrange
        var context = await TestDbContextFactory.CreateSeededContextAsync();
        var service = new DataMigrationService(context, _loggerMock.Object);

        // Perform full migration
        await service.RunCompleteMigrationAsync(dryRun: false);

        // Act
        var stats = await service.GetMigrationStatsAsync();

        // Assert
        stats.TotalUserRecipes.Should().Be(2);
        stats.HouseholdRecipesNotMigrated.Should().Be(0);
        stats.RatingsWithUserRecipeId.Should().Be(2);
        stats.MigrationComplete.Should().BeTrue();
    }

    [Fact]
    public async Task RunCompleteMigration_ShouldMigrateEverything()
    {
        // Arrange
        var context = await TestDbContextFactory.CreateSeededContextAsync();
        var service = new DataMigrationService(context, _loggerMock.Object);

        // Act
        var result = await service.RunCompleteMigrationAsync(dryRun: false);

        // Assert
        result.Success.Should().BeTrue();
        result.WasDryRun.Should().BeFalse();
        result.RecipesMigration.Success.Should().BeTrue();
        result.RatingsMigration.Success.Should().BeTrue();
        result.FriendshipsMigration.Success.Should().BeTrue();

        // Verify final state
        var userRecipes = await context.UserRecipes.CountAsync();
        userRecipes.Should().Be(2);

        var ratingsWithUserRecipe = await context.Ratings.CountAsync(r => r.UserRecipeId != null);
        ratingsWithUserRecipe.Should().Be(2);
    }
}
