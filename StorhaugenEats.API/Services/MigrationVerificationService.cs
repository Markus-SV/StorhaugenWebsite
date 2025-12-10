using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Data;

namespace StorhaugenEats.API.Services;

public class MigrationVerificationService : IMigrationVerificationService
{
    private readonly AppDbContext _context;
    private readonly ILogger<MigrationVerificationService> _logger;

    public MigrationVerificationService(AppDbContext context, ILogger<MigrationVerificationService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<VerificationReport> RunAllVerificationsAsync()
    {
        _logger.LogInformation("Starting comprehensive migration verification");

        var report = new VerificationReport();
        var results = new List<VerificationResult>();

        // Run all verification checks
        results.Add(await VerifyRecipeMigrationAsync());
        results.Add(await VerifyRatingMigrationAsync());
        results.Add(await VerifyDataIntegrityAsync());
        results.Add(await CheckOrphanedRecordsAsync());

        report.Results = results;
        report.AllPassed = results.All(r => r.Passed);

        // Generate summary
        report.Summary = new VerificationSummary
        {
            TotalChecks = results.Count,
            PassedChecks = results.Count(r => r.Passed),
            FailedChecks = results.Count(r => !r.Passed),
            TotalIssues = results.Sum(r => r.IssuesFound),
            ErrorCount = results.SelectMany(r => r.Issues).Count(i => i.Severity == "error"),
            WarningCount = results.SelectMany(r => r.Issues).Count(i => i.Severity == "warning")
        };

        _logger.LogInformation(
            "Verification complete. Passed: {Passed}/{Total}, Issues: {Issues}",
            report.Summary.PassedChecks, report.Summary.TotalChecks, report.Summary.TotalIssues);

        return report;
    }

    public async Task<VerificationResult> VerifyRecipeMigrationAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new VerificationResult { CheckName = "Recipe Migration Verification" };

        try
        {
            // Get all household recipes with AddedByUserId
            var householdRecipes = await _context.HouseholdRecipes
                .Where(hr => hr.AddedByUserId != null)
                .Select(hr => new { hr.Id, hr.LocalTitle, hr.AddedByUserId })
                .ToListAsync();

            result.ItemsChecked = householdRecipes.Count;

            // Get all user recipe IDs
            var userRecipeIds = await _context.UserRecipes
                .Select(ur => ur.Id)
                .ToHashSetAsync();

            // Check each household recipe has a corresponding user recipe
            foreach (var hr in householdRecipes)
            {
                if (!userRecipeIds.Contains(hr.Id))
                {
                    result.Issues.Add(new VerificationIssue
                    {
                        Severity = "error",
                        Description = $"Household recipe '{hr.LocalTitle ?? "Unnamed"}' has no corresponding user recipe",
                        RecordId = hr.Id.ToString(),
                        RecordType = "HouseholdRecipe",
                        Details = { ["AddedByUserId"] = hr.AddedByUserId! }
                    });
                }
            }

            result.IssuesFound = result.Issues.Count;
            result.Passed = result.Issues.Count(i => i.Severity == "error") == 0;
            result.Status = result.Passed ? "All recipes migrated successfully" : $"{result.IssuesFound} recipes not migrated";
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.Status = $"Check failed: {ex.Message}";
            _logger.LogError(ex, "Recipe migration verification failed");
        }

        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;
        return result;
    }

    public async Task<VerificationResult> VerifyRatingMigrationAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new VerificationResult { CheckName = "Rating Migration Verification" };

        try
        {
            // Get ratings that have household_recipe_id but no user_recipe_id
            var unmigrated = await _context.Ratings
                .Where(r => r.HouseholdRecipeId != null && r.UserRecipeId == null)
                .Select(r => new { r.Id, r.HouseholdRecipeId, r.UserId })
                .ToListAsync();

            // Get ratings with user_recipe_id
            var migratedCount = await _context.Ratings
                .Where(r => r.UserRecipeId != null)
                .CountAsync();

            result.ItemsChecked = unmigrated.Count + migratedCount;

            foreach (var rating in unmigrated)
            {
                // Check if the corresponding user recipe exists
                var userRecipeExists = await _context.UserRecipes
                    .AnyAsync(ur => ur.Id == rating.HouseholdRecipeId);

                result.Issues.Add(new VerificationIssue
                {
                    Severity = userRecipeExists ? "warning" : "error",
                    Description = userRecipeExists
                        ? "Rating has household_recipe_id but user_recipe_id not set (can be migrated)"
                        : "Rating references non-existent user recipe",
                    RecordId = rating.Id.ToString(),
                    RecordType = "Rating",
                    Details = {
                        ["HouseholdRecipeId"] = rating.HouseholdRecipeId!,
                        ["UserRecipeExists"] = userRecipeExists
                    }
                });
            }

            result.IssuesFound = result.Issues.Count;
            result.Passed = result.Issues.Count(i => i.Severity == "error") == 0;
            result.Status = result.Passed
                ? $"All ratings verified ({migratedCount} migrated)"
                : $"{result.Issues.Count(i => i.Severity == "error")} critical issues found";
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.Status = $"Check failed: {ex.Message}";
            _logger.LogError(ex, "Rating migration verification failed");
        }

        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;
        return result;
    }

    public async Task<VerificationResult> VerifyDataIntegrityAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new VerificationResult { CheckName = "Data Integrity Verification" };

        try
        {
            // Get matched pairs of household and user recipes
            var pairs = await (
                from hr in _context.HouseholdRecipes
                join ur in _context.UserRecipes on hr.Id equals ur.Id
                select new
                {
                    RecipeId = hr.Id,
                    HR_Title = hr.LocalTitle,
                    UR_Title = ur.LocalTitle,
                    HR_Description = hr.LocalDescription,
                    UR_Description = ur.LocalDescription,
                    HR_GlobalRecipeId = hr.GlobalRecipeId,
                    UR_GlobalRecipeId = ur.GlobalRecipeId,
                    HR_AddedByUserId = hr.AddedByUserId,
                    UR_UserId = ur.UserId
                }
            ).ToListAsync();

            result.ItemsChecked = pairs.Count;

            foreach (var pair in pairs)
            {
                // Check title matches
                if (pair.HR_Title != pair.UR_Title)
                {
                    result.Issues.Add(new VerificationIssue
                    {
                        Severity = "warning",
                        Description = "Title mismatch between household and user recipe",
                        RecordId = pair.RecipeId.ToString(),
                        RecordType = "Recipe",
                        Details = {
                            ["HouseholdTitle"] = pair.HR_Title ?? "(null)",
                            ["UserTitle"] = pair.UR_Title ?? "(null)"
                        }
                    });
                }

                // Check GlobalRecipeId matches
                if (pair.HR_GlobalRecipeId != pair.UR_GlobalRecipeId)
                {
                    result.Issues.Add(new VerificationIssue
                    {
                        Severity = "error",
                        Description = "GlobalRecipeId mismatch",
                        RecordId = pair.RecipeId.ToString(),
                        RecordType = "Recipe",
                        Details = {
                            ["HouseholdGlobalRecipeId"] = pair.HR_GlobalRecipeId?.ToString() ?? "(null)",
                            ["UserGlobalRecipeId"] = pair.UR_GlobalRecipeId?.ToString() ?? "(null)"
                        }
                    });
                }

                // Check user ID matches
                if (pair.HR_AddedByUserId != pair.UR_UserId)
                {
                    result.Issues.Add(new VerificationIssue
                    {
                        Severity = "error",
                        Description = "User ID mismatch",
                        RecordId = pair.RecipeId.ToString(),
                        RecordType = "Recipe",
                        Details = {
                            ["HouseholdAddedByUserId"] = pair.HR_AddedByUserId?.ToString() ?? "(null)",
                            ["UserRecipeUserId"] = pair.UR_UserId.ToString()
                        }
                    });
                }
            }

            result.IssuesFound = result.Issues.Count;
            result.Passed = result.Issues.Count(i => i.Severity == "error") == 0;
            result.Status = result.Passed
                ? $"Data integrity verified for {pairs.Count} recipes"
                : $"{result.Issues.Count(i => i.Severity == "error")} integrity errors found";
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.Status = $"Check failed: {ex.Message}";
            _logger.LogError(ex, "Data integrity verification failed");
        }

        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;
        return result;
    }

    public async Task<VerificationResult> CheckOrphanedRecordsAsync()
    {
        var stopwatch = Stopwatch.StartNew();
        var result = new VerificationResult { CheckName = "Orphaned Records Check" };

        try
        {
            // Check for ratings without valid recipe references
            var orphanedRatings = await _context.Ratings
                .Where(r =>
                    (r.HouseholdRecipeId != null && !_context.HouseholdRecipes.Any(hr => hr.Id == r.HouseholdRecipeId)) ||
                    (r.UserRecipeId != null && !_context.UserRecipes.Any(ur => ur.Id == r.UserRecipeId)) ||
                    (r.GlobalRecipeId != null && !_context.GlobalRecipes.Any(gr => gr.Id == r.GlobalRecipeId))
                )
                .Select(r => new { r.Id, r.HouseholdRecipeId, r.UserRecipeId, r.GlobalRecipeId })
                .Take(100) // Limit for performance
                .ToListAsync();

            result.ItemsChecked = orphanedRatings.Count;

            foreach (var rating in orphanedRatings)
            {
                result.Issues.Add(new VerificationIssue
                {
                    Severity = "warning",
                    Description = "Rating references non-existent recipe",
                    RecordId = rating.Id.ToString(),
                    RecordType = "Rating",
                    Details = {
                        ["HouseholdRecipeId"] = rating.HouseholdRecipeId?.ToString() ?? "(null)",
                        ["UserRecipeId"] = rating.UserRecipeId?.ToString() ?? "(null)",
                        ["GlobalRecipeId"] = rating.GlobalRecipeId?.ToString() ?? "(null)"
                    }
                });
            }

            // Check for user recipes without valid users
            var orphanedUserRecipes = await _context.UserRecipes
                .Where(ur => !_context.Users.Any(u => u.Id == ur.UserId))
                .Select(ur => new { ur.Id, ur.UserId, ur.LocalTitle })
                .Take(100)
                .ToListAsync();

            foreach (var recipe in orphanedUserRecipes)
            {
                result.Issues.Add(new VerificationIssue
                {
                    Severity = "error",
                    Description = "User recipe belongs to non-existent user",
                    RecordId = recipe.Id.ToString(),
                    RecordType = "UserRecipe",
                    Details = {
                        ["UserId"] = recipe.UserId.ToString(),
                        ["Title"] = recipe.LocalTitle ?? "(null)"
                    }
                });
            }

            result.IssuesFound = result.Issues.Count;
            result.Passed = result.Issues.Count(i => i.Severity == "error") == 0;
            result.Status = result.Passed
                ? "No orphaned records found"
                : $"{result.IssuesFound} orphaned records found";
        }
        catch (Exception ex)
        {
            result.Passed = false;
            result.Status = $"Check failed: {ex.Message}";
            _logger.LogError(ex, "Orphaned records check failed");
        }

        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;
        return result;
    }
}
