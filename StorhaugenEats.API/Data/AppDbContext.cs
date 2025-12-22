using Microsoft.EntityFrameworkCore;
using StorhaugenEats.API.Models;

namespace StorhaugenEats.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // Core entities
    public DbSet<User> Users { get; set; }
    public DbSet<GlobalRecipe> GlobalRecipes { get; set; }
    public DbSet<Rating> Ratings { get; set; }
    public DbSet<EtlSyncLog> EtlSyncLogs { get; set; }

    // User-centric entities
    public DbSet<UserRecipe> UserRecipes { get; set; }
    public DbSet<UserFriendship> UserFriendships { get; set; }
    public DbSet<ActivityFeedItem> ActivityFeedItems { get; set; }

    // Tags for personal recipe organization
    public DbSet<RecipeTag> RecipeTags { get; set; }
    public DbSet<UserRecipeTag> UserRecipeTags { get; set; }

    // Collections for recipe sharing
    public DbSet<Collection> Collections { get; set; }
    public DbSet<CollectionMember> CollectionMembers { get; set; }
    public DbSet<UserRecipeCollection> UserRecipeCollections { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure table names
        modelBuilder.Entity<User>().ToTable("users");
        modelBuilder.Entity<GlobalRecipe>().ToTable("global_recipes");
        modelBuilder.Entity<Rating>().ToTable("ratings");
        modelBuilder.Entity<EtlSyncLog>().ToTable("etl_sync_log");
        modelBuilder.Entity<UserRecipe>().ToTable("user_recipes");
        modelBuilder.Entity<UserFriendship>().ToTable("user_friendships");
        modelBuilder.Entity<ActivityFeedItem>().ToTable("activity_feed");
        modelBuilder.Entity<RecipeTag>().ToTable("recipe_tags");
        modelBuilder.Entity<UserRecipeTag>().ToTable("user_recipe_tags");
        modelBuilder.Entity<Collection>().ToTable("collections");
        modelBuilder.Entity<CollectionMember>().ToTable("collection_members");
        modelBuilder.Entity<UserRecipeCollection>().ToTable("user_recipe_collections");

        // ==========================================
        // USER CONFIGURATION
        // ==========================================
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.DisplayName).IsRequired().HasMaxLength(255);
            entity.Property(e => e.UniqueShareId).IsRequired().HasMaxLength(12);
            entity.Property(e => e.IsProfilePublic).HasDefaultValue(true);
            entity.Property(e => e.Bio).HasMaxLength(500);
            entity.Property(e => e.FavoriteCuisines).HasColumnType("jsonb").HasDefaultValue("[]");

            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.UniqueShareId).IsUnique();
        });

        // ==========================================
        // GLOBAL RECIPE CONFIGURATION
        // ==========================================
        modelBuilder.Entity<GlobalRecipe>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Title).IsRequired().HasMaxLength(255);
            entity.Property(e => e.Ingredients).HasColumnType("jsonb").IsRequired().HasDefaultValue("[]");
            entity.Property(e => e.NutritionData).HasColumnType("jsonb");
            entity.Property(e => e.AverageRating).HasColumnType("decimal(3,2)").HasDefaultValue(0.00m);
            entity.Property(e => e.RatingCount).HasDefaultValue(0);
            entity.Property(e => e.HellofreshUuid).HasMaxLength(255);
            entity.Property(e => e.IsEditable).HasDefaultValue(true);

            entity.HasIndex(e => e.IsHellofresh).HasFilter("is_hellofresh = true");
            entity.HasIndex(e => e.IsPublic).HasFilter("is_public = true");
            entity.HasIndex(e => e.AverageRating);
            entity.HasIndex(e => e.HellofreshUuid).IsUnique();

            entity.HasOne(e => e.CreatedByUser)
                .WithMany()
                .HasForeignKey(e => e.CreatedByUserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.PublishedFromUserRecipe)
                .WithMany()
                .HasForeignKey(e => e.PublishedFromUserRecipeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ==========================================
        // RATING CONFIGURATION
        // ==========================================
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

            entity.HasOne(e => e.UserRecipe)
                .WithMany(r => r.Ratings)
                .HasForeignKey(e => e.UserRecipeId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ==========================================
        // ETL SYNC LOG CONFIGURATION
        // ==========================================
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
        // USER RECIPE CONFIGURATION
        // ==========================================
        modelBuilder.Entity<UserRecipe>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LocalTitle).HasMaxLength(255);
            entity.Property(e => e.LocalIngredients).HasColumnType("jsonb");
            entity.Property(e => e.LocalImageUrls).HasColumnType("jsonb").HasDefaultValue("[]");
            entity.Property(e => e.LocalNutritionDataJson).HasColumnType("jsonb");
            entity.Property(e => e.Visibility).IsRequired().HasMaxLength(20).HasDefaultValue("private");
            entity.Property(e => e.IsArchived).HasDefaultValue(false);

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.GlobalRecipeId);
            entity.HasIndex(e => e.Visibility);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.User)
                .WithMany(u => u.UserRecipes)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.GlobalRecipe)
                .WithMany()
                .HasForeignKey(e => e.GlobalRecipeId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ==========================================
        // USER FRIENDSHIP CONFIGURATION
        // ==========================================
        modelBuilder.Entity<UserFriendship>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(20).HasDefaultValue("pending");
            entity.Property(e => e.Message).HasMaxLength(255);

            entity.HasIndex(e => new { e.RequesterUserId, e.TargetUserId }).IsUnique();
            entity.HasIndex(e => e.RequesterUserId);
            entity.HasIndex(e => e.TargetUserId);
            entity.HasIndex(e => e.Status);

            entity.ToTable(t => t.HasCheckConstraint("CK_UserFriendship_NoSelf", "requester_user_id != target_user_id"));

            entity.HasOne(e => e.RequesterUser)
                .WithMany(u => u.SentFriendRequests)
                .HasForeignKey(e => e.RequesterUserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.TargetUser)
                .WithMany(u => u.ReceivedFriendRequests)
                .HasForeignKey(e => e.TargetUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ==========================================
        // ACTIVITY FEED CONFIGURATION
        // ==========================================
        modelBuilder.Entity<ActivityFeedItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ActivityType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.TargetType).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Metadata).HasColumnType("jsonb").HasDefaultValue("{}");

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CreatedAt).IsDescending();
            entity.HasIndex(e => e.ActivityType);
            entity.HasIndex(e => new { e.UserId, e.CreatedAt });

            entity.HasOne(e => e.User)
                .WithMany(u => u.Activities)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ==========================================
        // RECIPE TAGS CONFIGURATION
        // ==========================================
        modelBuilder.Entity<RecipeTag>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Color).HasMaxLength(7);
            entity.Property(e => e.Icon).HasMaxLength(50);

            entity.HasIndex(e => new { e.UserId, e.Name }).IsUnique();
            entity.HasIndex(e => e.UserId);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserRecipeTag>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.HasIndex(e => new { e.UserRecipeId, e.TagId }).IsUnique();

            entity.HasOne(e => e.UserRecipe)
                .WithMany(r => r.UserRecipeTags)
                .HasForeignKey(e => e.UserRecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Tag)
                .WithMany(t => t.UserRecipeTags)
                .HasForeignKey(e => e.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ==========================================
        // COLLECTIONS CONFIGURATION
        // ==========================================
        modelBuilder.Entity<Collection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Color).HasMaxLength(7);
            entity.Property(e => e.Icon).HasMaxLength(50);
            entity.Property(e => e.UniqueShareId).HasMaxLength(12);
            entity.Property(e => e.IsDefault).HasDefaultValue(false);
            entity.Property(e => e.IsShared).HasDefaultValue(false);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);

            entity.HasIndex(e => e.OwnerUserId);
            entity.HasIndex(e => e.UniqueShareId).IsUnique();

            entity.HasOne(e => e.Owner)
                .WithMany(u => u.OwnedCollections)
                .HasForeignKey(e => e.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // CollectionMember with id primary key
        modelBuilder.Entity<CollectionMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(20).HasDefaultValue("member");

            entity.HasIndex(e => e.UserId);
            entity.HasIndex(e => e.CollectionId);
            entity.HasIndex(e => new { e.CollectionId, e.UserId }).IsUnique();

            entity.HasOne(e => e.Collection)
                .WithMany(c => c.Members)
                .HasForeignKey(e => e.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(u => u.CollectionMemberships)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.InvitedByUser)
                .WithMany()
                .HasForeignKey(e => e.InvitedByUserId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // UserRecipeCollection with id primary key
        modelBuilder.Entity<UserRecipeCollection>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.Property(e => e.SortOrder).HasDefaultValue(0);

            entity.HasIndex(e => e.CollectionId);
            entity.HasIndex(e => e.UserRecipeId);
            entity.HasIndex(e => new { e.UserRecipeId, e.CollectionId }).IsUnique();
            entity.HasIndex(e => new { e.CollectionId, e.SortOrder });

            // Deleting a collection deletes only the link rows, not the recipes
            entity.HasOne(e => e.Collection)
                .WithMany(c => c.UserRecipeCollections)
                .HasForeignKey(e => e.CollectionId)
                .OnDelete(DeleteBehavior.Cascade);

            // Deleting a recipe deletes only the link rows
            entity.HasOne(e => e.UserRecipe)
                .WithMany(r => r.UserRecipeCollections)
                .HasForeignKey(e => e.UserRecipeId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.AddedByUser)
                .WithMany()
                .HasForeignKey(e => e.AddedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
