using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorhaugenEats.API.Models;

[Table("etl_sync_log")]
public class EtlSyncLog
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Column("sync_type")]
    [MaxLength(50)]
    public string SyncType { get; set; } = "hellofresh";

    [Column("status")]
    [MaxLength(20)]
    public string? Status { get; set; } // "success", "failed", "partial"

    [Column("recipes_added")]
    public int RecipesAdded { get; set; } = 0;

    [Column("recipes_updated")]
    public int RecipesUpdated { get; set; } = 0;

    [Column("error_message")]
    public string? ErrorMessage { get; set; }

    [Column("build_id")]
    [MaxLength(255)]
    public string? BuildId { get; set; }

    [Column("weeks_synced")]
    [MaxLength(255)]
    public string? WeeksSynced { get; set; }

    [Column("started_at")]
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;

    [Column("completed_at")]
    public DateTime? CompletedAt { get; set; }
}
