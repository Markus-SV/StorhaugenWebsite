namespace StorhaugenEats.API.Services;

/// <summary>
/// Service for verifying data integrity after migration.
/// </summary>
public interface IMigrationVerificationService
{
    /// <summary>
    /// Runs all verification checks and returns a comprehensive report.
    /// </summary>
    Task<VerificationReport> RunAllVerificationsAsync();

    /// <summary>
    /// Verifies that all household recipes have been migrated to user recipes.
    /// </summary>
    Task<VerificationResult> VerifyRecipeMigrationAsync();

    /// <summary>
    /// Verifies that all ratings reference the correct user recipes.
    /// </summary>
    Task<VerificationResult> VerifyRatingMigrationAsync();

    /// <summary>
    /// Verifies that recipe data matches between household and user recipes.
    /// </summary>
    Task<VerificationResult> VerifyDataIntegrityAsync();

    /// <summary>
    /// Checks for orphaned records (ratings without recipes, etc.).
    /// </summary>
    Task<VerificationResult> CheckOrphanedRecordsAsync();
}

public class VerificationReport
{
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public bool AllPassed { get; set; }
    public List<VerificationResult> Results { get; set; } = new();
    public VerificationSummary Summary { get; set; } = new();
}

public class VerificationResult
{
    public string CheckName { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Status { get; set; } = string.Empty;
    public int ItemsChecked { get; set; }
    public int IssuesFound { get; set; }
    public List<VerificationIssue> Issues { get; set; } = new();
    public TimeSpan Duration { get; set; }
}

public class VerificationIssue
{
    public string Severity { get; set; } = "warning"; // "error", "warning", "info"
    public string Description { get; set; } = string.Empty;
    public string? RecordId { get; set; }
    public string? RecordType { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
}

public class VerificationSummary
{
    public int TotalChecks { get; set; }
    public int PassedChecks { get; set; }
    public int FailedChecks { get; set; }
    public int TotalIssues { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }
}
