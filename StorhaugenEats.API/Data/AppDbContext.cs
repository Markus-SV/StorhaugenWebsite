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
    public DbSet<HouseholdMember> HouseholdMembers { get; set; }
    public DbSet<GlobalRecipe> GlobalRecipes { get; set; }
    public DbSet<HouseholdRecipe> HouseholdRecipes { get; set; }
    public DbSet<Rating> Ratings { get; set; }
    public DbSet<HouseholdInvite> HouseholdInvites { get; set; }
    public DbSet<HouseholdFriendship> HouseholdFriendships { get; set; }
    public DbSet<EtlSyncLog> EtlSyncLogs { get; set; }

    // New user-centric tables
    public DbSet<UserRecipe> UserRecipes { get; set; }
    public DbSet<UserFriendship> UserFriendships { get; set; }
    public DbSet<ActivityFeedItem> ActivityFeedItems { get; set; }

    // Tags for personal recipe organization
    public DbSet<RecipeTag> RecipeTags { get; set; }
    public DbSet<UserRecipeTag> UserRecipeTags { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure table names (match PostgreSQL schema)
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<Household>().ToTable("households");
        modelBuilder.Entity<HouseholdMember>().ToTable("household_members");
        modelBuilder.Entity<GlobalRecipe>().ToTable("global_recipes");
        modelBuilder.Entity<HouseholdRecipe>().ToTable("household_recipes");
        modelBuilder.Entity<Rating>().ToTable("ratings");
        modelBuilder.Entity<HouseholdInvite>().ToTable("household_invites");
        modelBuilder.Entity<HouseholdFriendship>().ToTable("household_friendships");
        modelBuilder.Entity<EtlSyncLog>().ToTable("etl_sync_log");

        // New user-centric tables
        modelBuilder.Entity<UserRecipe>().ToTable("user_recipes");
        modelBuilder.Entity<UserFriendship>().ToTable("user_friendships");
        modelBuilder.Entity<ActivityFeedItem>().ToTable("activity_feed");

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
            entity.Property(e => e.IsPrivate).HasDefaultValue(false);
            entity.Property(e => e.UniqueShareId).HasMaxLength(12);
            entity.HasIndex(e => e.UniqueShareId).IsUnique();
            entity.Property(e => e.Settings)
                .HasColumnType("jsonb")
                .HasDefaultValue("{}");

            entity.HasOne(e => e.Leader)
                .WithMany()
                .HasForeignKey(e => e.LeaderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // HouseholdMember configuration
        modelBuilder.Entity<HouseholdMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(20).HasDefaultValue("member");

            entity.HasOne(e => e.Household)
                .WithMany(h => h.HouseholdMembers)
                .HasForeignKey(e => e.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => new { e.HouseholdId, e.UserId }).IsUnique();
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
            entity.Property(e => e.IsPublic).HasDefaultValue(false);

            // Unique constraint: household can't have duplicate global recipes
            entity.HasIndex(e => new { e.HouseholdId, e.GlobalRecipeId })
                .IsUnique()
                .HasFilter("global_recipe_id IS NOT NULL");

            entity.HasIndex(e => e.IsPublic).HasFilter("is_public = true");

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

        // HouseholdFriendship configuration
        modelBuilder.Entity<HouseholdFriendship>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("pending");

            // Prevent duplicate requests between the same two households
            entity.HasIndex(e => new { e.RequesterHouseholdId, e.TargetHouseholdId }).IsUnique();

            entity.HasOne(e => e.RequesterHousehold)
                .WithMany(h => h.SentFriendRequests)
                .HasForeignKey(e => e.RequesterHouseholdId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.TargetHousehold)
                .WithMany(h => h.ReceivedFriendRequests)
                .HasForeignKey(e => e.TargetHouseholdId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Rating configuration
        modelBuilder.Entity<Rating>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Score).IsRequired();
            entity.ToTable(t => t.HasCheckConstraint("CK_Rating_Score", "score >= 0 AND score <= 10"));

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
            entity.Property(e => e.InvitedEmail).HasMaxLength(255);

            entity.HasOne(e => e.Household)
                .WithMany()
                .HasForeignKey(e => e.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.InvitedUser)
                .WithMany()
                .HasForeignKey(e => e.InvitedUserId)
                .OnDelete(DeleteBehavior.SetNull)
                .IsRequired(false);

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

        // ==========================================
        // NEW USER-CENTRIC ENTITY CONFIGURATIONS
        // ==========================================

        // UserRecipe configuration
        modelBuilder.Entity<UserRecipe>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LocalTitle).HasMaxLength(255);
            entity.Property(e => e.LocalIngredients).HasColumnType("jsonb");
            entity.Property(e => e.LocalImageUrls).HasColumnType("jsonb").HasDefaultValue("[]");
            entity.Property(e => e.Visibility).IsRequired().HasMaxLength(20).HasDefaultValue("private");
            entity.Property(e => e.IsArchived).HasDefaultValue(false);

            // Indexes for common queries
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.GlobalRecipeId);
            entity.HasIndex(e => e.Visibility);
            entity.HasIndex(e => e.CreatedAt);

            // Relationships
            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRecipes)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.GlobalRecipe)
                .WithMany()
                .HasForeignKey(e => e.GlobalRecipeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // UserFriendship configuration
        modelBuilder.Entity<UserFriendship>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("pending");
            entity.Property(e => e.Message).HasMaxLength(255);

            // Prevent duplicate friend requests between the same two users
            entity.HasIndex(e => new { e.RequesterUserId, e.TargetUserId }).IsUnique();

            // Indexes for common queries
            entity.HasIndex(e => e.RequesterUserId);
            entity.HasIndex(e => e.TargetUserId);
            entity.HasIndex(e => e.Status);

            // Prevent self-friending at database level
            entity.ToTable(t => t.HasCheckConstraint("CK_UserFriendship_NoSelf", "requester_user_id != target_user_id"));

            // Relationships
            entity.HasOne(e => e.RequesterUser)
                .WithMany(u => u.SentFriendRequests)
                .HasForeignKey(e => e.RequesterUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.TargetUser)
                .WithMany(u => u.ReceivedFriendRequests)
                .HasForeignKey(e => e.TargetUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ActivityFeedItem configuration
        modelBuilder.Entity<ActivityFeedItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ActivityType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TargetType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Metadata).HasColumnType("jsonb").HasDefaultValue("{}");

            // Indexes for efficient feed queries
            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt).IsDescending();
            entity.HasIndex(e => e.ActivityType);

            // Composite index for feed pagination
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });

            // Relationships
            entity.HasOne(e => e.User)
                .WithMany(u => u.Activities)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Update Rating to support UserRecipe
        modelBuilder.Entity<Rating>(entity =>
        {
            entity.HasOne(e => e.UserRecipe)
                .WithMany(r => r.Ratings)
                .HasForeignKey(e => e.UserRecipeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Update GlobalRecipe for publishing relationship
        modelBuilder.Entity<GlobalRecipe>(entity =>
        {
            entity.Property(e => e.IsEditable).HasDefaultValue(true);

            entity.HasOne(e => e.PublishedFromUserRecipe)
                .WithMany()
                .HasForeignKey(e => e.PublishedFromUserRecipeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // Update User with new fields
        modelBuilder.Entity<User>(entity =>
        {
            entity.Property(e => e.IsProfilePublic).HasDefaultValue(true);
            entity.Property(e => e.Bio).HasMaxLength(500);
            entity.Property(e => e.FavoriteCuisines).HasColumnType("jsonb").HasDefaultValue("[]");
        });

        // ==========================================
        // RECIPE TAGS FOR PERSONAL ORGANIZATION
        // ==========================================

        modelBuilder.Entity<RecipeTag>().ToTable("recipe_tags");
        modelBuilder.Entity<UserRecipeTag>().ToTable("user_recipe_tags");

        // RecipeTag configuration
        modelBuilder.Entity<RecipeTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Color).HasMaxLength(7);
            entity.Property(e => e.Icon).HasMaxLength(50);

            // Unique tag names per user
            entity.HasIndex(e => new { e.UserId, e.Name }).IsUnique();

            // Index for user's tags
            entity.HasIndex(e => e.UserId);

            // Relationship
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // UserRecipeTag (join table) configuration
        modelBuilder.Entity<UserRecipeTag>(entity =>
        {
            entity.HasKey(e => e.Id);

            // Unique constraint: a recipe can only have each tag once
            entity.HasIndex(e => new { e.UserRecipeId, e.TagId }).IsUnique();

            // Relationships
            entity.HasOne(e => e.UserRecipe)
                .WithMany(r => r.UserRecipeTags)
                .HasForeignKey(e => e.UserRecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Tag)
                .WithMany(t => t.UserRecipeTags)
                .HasForeignKey(e => e.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
