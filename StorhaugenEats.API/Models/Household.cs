using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace StorhaugenEats.API.Models;

[Table("households")]
public class Household
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("name")]
    [MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Column("leader_id")]
    public Guid? LeaderId { get; set; }

    [Column("settings", TypeName = "jsonb")]
    public string Settings { get; set; } = "{}";

    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public User? Leader { get; set; }
    public ICollection<User> Members { get; set; } = new List<User>();
    public ICollection<HouseholdRecipe> HouseholdRecipes { get; set; } = new List<HouseholdRecipe>();
    public ICollection<HouseholdMember> HouseholdMembers { get; set; } = new List<HouseholdMember>();

    // Backward compatibility properties
    [NotMapped]
    public Guid? CreatedById
    {
        get => LeaderId;
        set => LeaderId = value;
    }
}
