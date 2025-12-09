using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorhaugenEats.API.Migrations
{
    /// <summary>
    /// Migration to add user-centric tables for the social recipe app refactoring.
    /// This migration adds:
    /// - user_recipes table (replaces household-centric recipe ownership)
    /// - user_friendships table (user-to-user friendships)
    /// - activity_feed table (social activity feed)
    /// - New columns to users, global_recipes, and ratings tables
    /// </summary>
    public partial class AddUserCentricTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ==========================================
            // 1. ADD NEW COLUMNS TO EXISTING TABLES
            // ==========================================

            // Users table - Add profile fields
            migrationBuilder.AddColumn<bool>(
                name: "is_profile_public",
                table: "users",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "bio",
                table: "users",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "favorite_cuisines",
                table: "users",
                type: "jsonb",
                nullable: false,
                defaultValue: "[]");

            // Global Recipes table - Add publishing fields
            migrationBuilder.AddColumn<Guid>(
                name: "published_from_user_recipe_id",
                table: "global_recipes",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_editable",
                table: "global_recipes",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            // Ratings table - Add user_recipe_id
            migrationBuilder.AddColumn<Guid>(
                name: "user_recipe_id",
                table: "ratings",
                type: "uuid",
                nullable: true);

            // ==========================================
            // 2. CREATE USER_RECIPES TABLE
            // ==========================================

            migrationBuilder.CreateTable(
                name: "user_recipes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    global_recipe_id = table.Column<Guid>(type: "uuid", nullable: true),
                    local_title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    local_description = table.Column<string>(type: "text", nullable: true),
                    local_ingredients = table.Column<string>(type: "jsonb", nullable: true),
                    local_image_url = table.Column<string>(type: "text", nullable: true),
                    local_image_urls = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    personal_notes = table.Column<string>(type: "text", nullable: true),
                    visibility = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "private"),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    archived_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_recipes", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_recipes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_recipes_global_recipes_global_recipe_id",
                        column: x => x.global_recipe_id,
                        principalTable: "global_recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            // ==========================================
            // 3. CREATE USER_FRIENDSHIPS TABLE
            // ==========================================

            migrationBuilder.CreateTable(
                name: "user_friendships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    requester_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    message = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_friendships", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_friendships_users_requester_user_id",
                        column: x => x.requester_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_friendships_users_target_user_id",
                        column: x => x.target_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.CheckConstraint("CK_UserFriendship_NoSelf", "requester_user_id != target_user_id");
                });

            // ==========================================
            // 4. CREATE ACTIVITY_FEED TABLE
            // ==========================================

            migrationBuilder.CreateTable(
                name: "activity_feed",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    activity_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    target_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    target_id = table.Column<Guid>(type: "uuid", nullable: false),
                    metadata = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_activity_feed", x => x.id);
                    table.ForeignKey(
                        name: "FK_activity_feed_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ==========================================
            // 5. CREATE INDEXES
            // ==========================================

            // User Recipes indexes
            migrationBuilder.CreateIndex(
                name: "IX_user_recipes_user_id",
                table: "user_recipes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_recipes_global_recipe_id",
                table: "user_recipes",
                column: "global_recipe_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_recipes_visibility",
                table: "user_recipes",
                column: "visibility");

            migrationBuilder.CreateIndex(
                name: "IX_user_recipes_created_at",
                table: "user_recipes",
                column: "created_at");

            // User Friendships indexes
            migrationBuilder.CreateIndex(
                name: "IX_user_friendships_requester_user_id",
                table: "user_friendships",
                column: "requester_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_friendships_target_user_id",
                table: "user_friendships",
                column: "target_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_friendships_status",
                table: "user_friendships",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_user_friendships_requester_target_unique",
                table: "user_friendships",
                columns: new[] { "requester_user_id", "target_user_id" },
                unique: true);

            // Activity Feed indexes
            migrationBuilder.CreateIndex(
                name: "IX_activity_feed_user_id",
                table: "activity_feed",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_activity_feed_created_at",
                table: "activity_feed",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_activity_feed_activity_type",
                table: "activity_feed",
                column: "activity_type");

            migrationBuilder.CreateIndex(
                name: "IX_activity_feed_user_id_created_at",
                table: "activity_feed",
                columns: new[] { "user_id", "created_at" });

            // Ratings FK index
            migrationBuilder.CreateIndex(
                name: "IX_ratings_user_recipe_id",
                table: "ratings",
                column: "user_recipe_id");

            // Global Recipes FK index
            migrationBuilder.CreateIndex(
                name: "IX_global_recipes_published_from_user_recipe_id",
                table: "global_recipes",
                column: "published_from_user_recipe_id");

            // ==========================================
            // 6. ADD FOREIGN KEY CONSTRAINTS
            // ==========================================

            migrationBuilder.AddForeignKey(
                name: "FK_ratings_user_recipes_user_recipe_id",
                table: "ratings",
                column: "user_recipe_id",
                principalTable: "user_recipes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_global_recipes_user_recipes_published_from",
                table: "global_recipes",
                column: "published_from_user_recipe_id",
                principalTable: "user_recipes",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            // ==========================================
            // 7. SET is_editable = false FOR HELLOFRESH RECIPES
            // ==========================================

            migrationBuilder.Sql(
                "UPDATE global_recipes SET is_editable = false WHERE is_hellofresh = true");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop foreign keys first
            migrationBuilder.DropForeignKey(
                name: "FK_ratings_user_recipes_user_recipe_id",
                table: "ratings");

            migrationBuilder.DropForeignKey(
                name: "FK_global_recipes_user_recipes_published_from",
                table: "global_recipes");

            // Drop tables
            migrationBuilder.DropTable(name: "activity_feed");
            migrationBuilder.DropTable(name: "user_friendships");
            migrationBuilder.DropTable(name: "user_recipes");

            // Drop indexes
            migrationBuilder.DropIndex(
                name: "IX_ratings_user_recipe_id",
                table: "ratings");

            migrationBuilder.DropIndex(
                name: "IX_global_recipes_published_from_user_recipe_id",
                table: "global_recipes");

            // Drop columns from ratings
            migrationBuilder.DropColumn(
                name: "user_recipe_id",
                table: "ratings");

            // Drop columns from global_recipes
            migrationBuilder.DropColumn(
                name: "published_from_user_recipe_id",
                table: "global_recipes");

            migrationBuilder.DropColumn(
                name: "is_editable",
                table: "global_recipes");

            // Drop columns from users
            migrationBuilder.DropColumn(
                name: "is_profile_public",
                table: "users");

            migrationBuilder.DropColumn(
                name: "bio",
                table: "users");

            migrationBuilder.DropColumn(
                name: "favorite_cuisines",
                table: "users");
        }
    }
}
