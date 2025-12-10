using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using StorhaugenEats.API.Services;
using Xunit;

namespace StorhaugenEats.API.Tests.Services;

public class MigrationVerificationServiceTests
{
    private readonly Mock<ILogger<MigrationVerificationService>> _loggerMock;

    public MigrationVerificationServiceTests()
    {
        _loggerMock = new Mock<ILogger<MigrationVerificationService>>();
    }

    [Fact]
    public async Task VerifyRecipeMigration_BeforeMigration_ShouldFail()
    {
        // Arrange
        var context = await TestDbContextFactory.CreateSeededContextAsync();
        var service = new MigrationVerificationService(context, _loggerMock.Object);

        // Act
        var result = await service.VerifyRecipeMigrationAsync();

        // Assert
        result.Passed.Should().BeFalse();
        result.IssuesFound.Should().Be(2);
        result.Issues.All(i => i.Severity == "error").Should().BeTrue();
        result.Issues.All(i => i.RecordType == "HouseholdRecipe").Should().BeTrue();
    }

    [Fact]
    public async Task VerifyRecipeMigration_AfterMigration_ShouldPass()
    {
        // Arrange
        var context = await TestDbContextFactory.CreateSeededContextAsync();
        var migrationLogger = new Mock<ILogger<DataMigrationService>>();
        var migrationService = new DataMigrationService(context, migrationLogger.Object);
        var verificationService = new MigrationVerificationService(context, _loggerMock.Object);

        // Migrate first
        await migrationService.MigrateHouseholdRecipesToUserRecipesAsync(dryRun: false);

        // Act
        var result = await verificationService.VerifyRecipeMigrationAsync();

        // Assert
        result.Passed.Should().BeTrue();
        result.IssuesFound.Should().Be(0);
    }

    [Fact]
    public async Task VerifyRatingMigration_BeforeMigration_ShouldHaveWarnings()
    {
        // Arrange
        var context = await TestDbContextFactory.CreateSeededContextAsync();
        var migrationLogger = new Mock<ILogger<DataMigrationService>>();
        var migrationService = new DataMigrationService(context, migrationLogger.Object);
        var verificationService = new MigrationVerificationService(context, _loggerMock.Object);

        // Migrate recipes but not ratings
        await migrationService.MigrateHouseholdRecipesToUserRecipesAsync(dryRun: false);

        // Act
        var result = await verificationService.VerifyRatingMigrationAsync();

        // Assert
        // Ratings with HouseholdRecipeId but without UserRecipeId are warnings if user recipe exists
        result.Passed.Should().BeTrue(); // Warnings don't fail the check
        result.Issues.Count(i => i.Severity == "warning").Should().Be(2);
    }

    [Fact]
    public async Task VerifyRatingMigration_AfterMigration_ShouldPass()
    {
        // Arrange
        var context = await TestDbContextFactory.CreateSeededContextAsync();
        var migrationLogger = new Mock<ILogger<DataMigrationService>>();
        var migrationService = new DataMigrationService(context, migrationLogger.Object);
        var verificationService = new MigrationVerificationService(context, _loggerMock.Object);

        // Complete migration
        await migrationService.RunCompleteMigrationAsync(dryRun: false);

        // Act
        var result = await verificationService.VerifyRatingMigrationAsync();

        // Assert
        result.Passed.Should().BeTrue();
        result.IssuesFound.Should().Be(0);
    }

    [Fact]
    public async Task VerifyDataIntegrity_AfterMigration_ShouldMatchData()
    {
        // Arrange
        var context = await TestDbContextFactory.CreateSeededContextAsync();
        var migrationLogger = new Mock<ILogger<DataMigrationService>>();
        var migrationService = new DataMigrationService(context, migrationLogger.Object);
        var verificationService = new MigrationVerificationService(context, _loggerMock.Object);

        // Migrate
        await migrationService.MigrateHouseholdRecipesToUserRecipesAsync(dryRun: false);

        // Act
        var result = await verificationService.VerifyDataIntegrityAsync();

        // Assert
        result.Passed.Should().BeTrue();
        result.ItemsChecked.Should().Be(2);
        // Should have no errors (critical mismatches)
        result.Issues.Count(i => i.Severity == "error").Should().Be(0);
    }

    [Fact]
    public async Task CheckOrphanedRecords_NoOrphans_ShouldPass()
    {
        // Arrange
        var context = await TestDbContextFactory.CreateSeededContextAsync();
        var migrationLogger = new Mock<ILogger<DataMigrationService>>();
        var migrationService = new DataMigrationService(context, migrationLogger.Object);
        var verificationService = new MigrationVerificationService(context, _loggerMock.Object);

        // Complete migration
        await migrationService.RunCompleteMigrationAsync(dryRun: false);

        // Act
        var result = await verificationService.CheckOrphanedRecordsAsync();

        // Assert
        result.Passed.Should().BeTrue();
    }

    [Fact]
    public async Task RunAllVerifications_AfterMigration_ShouldAllPass()
    {
        // Arrange
        var context = await TestDbContextFactory.CreateSeededContextAsync();
        var migrationLogger = new Mock<ILogger<DataMigrationService>>();
        var migrationService = new DataMigrationService(context, migrationLogger.Object);
        var verificationService = new MigrationVerificationService(context, _loggerMock.Object);

        // Complete migration
        await migrationService.RunCompleteMigrationAsync(dryRun: false);

        // Act
        var report = await verificationService.RunAllVerificationsAsync();

        // Assert
        report.AllPassed.Should().BeTrue();
        report.Summary.TotalChecks.Should().Be(4);
        report.Summary.PassedChecks.Should().Be(4);
        report.Summary.FailedChecks.Should().Be(0);
        report.Summary.ErrorCount.Should().Be(0);
    }

    [Fact]
    public async Task RunAllVerifications_BeforeMigration_ShouldHaveFailures()
    {
        // Arrange
        var context = await TestDbContextFactory.CreateSeededContextAsync();
        var verificationService = new MigrationVerificationService(context, _loggerMock.Object);

        // Act - Run without any migration
        var report = await verificationService.RunAllVerificationsAsync();

        // Assert
        report.AllPassed.Should().BeFalse();
        report.Summary.ErrorCount.Should().BeGreaterThan(0);
    }
}
