using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Models;

namespace StorhaugenEats.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Household> Households { get; set; }
    public DbSet<GlobalRecipe> GlobalRecipes { get; set; }
    public DbSet<HouseholdRecipe> HouseholdRecipes { get; set; }
    public DbSet<Rating> Ratings { get; set; }
    public DbSet<HouseholdInvite> HouseholdInvites { get; set; }
    public DbSet<EtlSyncLog> EtlSyncLogs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure table names (match PostgreSQL schema)
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Household>().ToTable("households");
        modelBuilder.Entity<GlobalRecipe>().ToTable("global_recipes");
        modelBuilder.Entity<HouseholdRecipe>().ToTable("household_recipes");
        modelBuilder.Entity<Rating>().ToTable("ratings");
        modelBuilder.Entity<HouseholdInvite>().ToTable("household_invites");
        modelBuilder.Entity<EtlSyncLog>().ToTable("etl_sync_log");

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.UniqueShareId).IsRequired().HasMaxLength(12);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.UniqueShareId).IsUnique();

            entity.HasOne(e => e.CurrentHousehold)
                .WithMany(h => h.Members)
                .HasForeignKey(e => e.CurrentHouseholdId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Household configuration
        modelBuilder.Entity<Household>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Settings)
                .HasColumnType("jsonb")
                .HasDefaultValue("{}");

            entity.HasOne(e => e.Leader)
                .WithMany()
                .HasForeignKey(e => e.LeaderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // GlobalRecipe configuration
        modelBuilder.Entity<GlobalRecipe>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Ingredients)
                .HasColumnType("jsonb")
                .IsRequired()
                .HasDefaultValue("[]");
            entity.Property(e => e.NutritionData).HasColumnType("jsonb");
            entity.Property(e => e.AverageRating).HasColumnType("decimal(3,2)").HasDefaultValue(0.00m);
            entity.Property(e => e.RatingCount).HasDefaultValue(0);
            entity.Property(e => e.HellofreshUuid).HasMaxLength(255);

            entity.HasIndex(e => e.IsHellofresh).HasFilter("is_hellofresh = true");
            entity.HasIndex(e => e.IsPublic).HasFilter("is_public = true");
            entity.HasIndex(e => e.AverageRating);
            entity.HasIndex(e => e.HellofreshUuid).IsUnique();

            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // HouseholdRecipe configuration
        modelBuilder.Entity<HouseholdRecipe>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LocalTitle).HasMaxLength(255);
            entity.Property(e => e.LocalIngredients).HasColumnType("jsonb");
            entity.Property(e => e.IsArchived).HasDefaultValue(false);

            // Unique constraint: household can't have duplicate global recipes
            entity.HasIndex(e => new { e.HouseholdId, e.GlobalRecipeId })
                .IsUnique()
                .HasFilter("global_recipe_id IS NOT NULL");

            entity.HasOne(e => e.Household)
                .WithMany(h => h.HouseholdRecipes)
                .HasForeignKey(e => e.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.GlobalRecipe)
                .WithMany()
                .HasForeignKey(e => e.GlobalRecipeId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.AddedByUser)
                .WithMany()
                .HasForeignKey(e => e.AddedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Rating configuration
        modelBuilder.Entity<Rating>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Score).IsRequired();
            entity.HasCheckConstraint("CK_Rating_Score", "score >= 0 AND score <= 10");

            // Unique constraint: one rating per user per recipe
            entity.HasIndex(e => new { e.GlobalRecipeId, e.UserId }).IsUnique();

            entity.HasOne(e => e.GlobalRecipe)
                .WithMany(r => r.Ratings)
                .HasForeignKey(e => e.GlobalRecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // HouseholdInvite configuration
        modelBuilder.Entity<HouseholdInvite>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("pending");
            entity.Property(e => e.MergeRequested).HasDefaultValue(false);

            entity.HasIndex(e => new { e.HouseholdId, e.InvitedUserId }).IsUnique();

            entity.HasOne(e => e.Household)
                .WithMany()
                .HasForeignKey(e => e.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.InvitedUser)
                .WithMany()
                .HasForeignKey(e => e.InvitedUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.InvitedByUser)
                .WithMany()
                .HasForeignKey(e => e.InvitedByUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // EtlSyncLog configuration
        modelBuilder.Entity<EtlSyncLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.SyncType).HasMaxLength(50).HasDefaultValue("hellofresh");
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.RecipesAdded).HasDefaultValue(0);
            entity.Property(e => e.RecipesUpdated).HasDefaultValue(0);
            entity.Property(e => e.BuildId).HasMaxLength(255);
            entity.Property(e => e.WeeksSynced).HasMaxLength(255);

            entity.HasIndex(e => e.StartedAt);
        });
    }
}
