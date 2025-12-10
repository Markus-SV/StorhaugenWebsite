using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorhaugenEats.API.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "etl_sync_log",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    sync_type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "hellofresh"),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    recipes_added = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    recipes_updated = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    error_message = table.Column<string>(type: "text", nullable: true),
                    build_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    weeks_synced = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    started_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_etl_sync_log", x => x.id);
                });

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
                });

            migrationBuilder.CreateTable(
                name: "global_recipes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    ingredients = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                    nutrition_data = table.Column<string>(type: "jsonb", nullable: true),
                    cook_time_minutes = table.Column<int>(type: "integer", nullable: true),
                    difficulty = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_hellofresh = table.Column<bool>(type: "boolean", nullable: false),
                    hellofresh_uuid = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_public = table.Column<bool>(type: "boolean", nullable: false),
                    average_rating = table.Column<decimal>(type: "numeric(3,2)", nullable: false, defaultValue: 0.00m),
                    rating_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    tags = table.Column<string>(type: "jsonb", nullable: false),
                    cuisine = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    servings = table.Column<int>(type: "integer", nullable: true),
                    prep_time_minutes = table.Column<int>(type: "integer", nullable: true),
                    total_time_minutes = table.Column<int>(type: "integer", nullable: true),
                    hellofresh_slug = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    total_ratings = table.Column<int>(type: "integer", nullable: false),
                    total_times_added = table.Column<int>(type: "integer", nullable: false),
                    image_urls = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    published_from_user_recipe_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_editable = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_global_recipes", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "household_friendships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    requester_household_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_household_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    responded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    message = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_household_friendships", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "household_invites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    household_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invited_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    invited_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    invited_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    merge_requested = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_household_invites", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "household_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    household_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "member"),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_household_members", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "household_recipes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    household_id = table.Column<Guid>(type: "uuid", nullable: false),
                    global_recipe_id = table.Column<Guid>(type: "uuid", nullable: true),
                    local_title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    local_description = table.Column<string>(type: "text", nullable: true),
                    local_ingredients = table.Column<string>(type: "jsonb", nullable: true),
                    is_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    local_image_url = table.Column<string>(type: "text", nullable: true),
                    personal_notes = table.Column<string>(type: "text", nullable: true),
                    added_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    archived_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    archived_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ArchivedById = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_household_recipes", x => x.id);
                    table.ForeignKey(
                        name: "FK_household_recipes_global_recipes_global_recipe_id",
                        column: x => x.global_recipe_id,
                        principalTable: "global_recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "households",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    leader_id = table.Column<Guid>(type: "uuid", nullable: true),
                    settings = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_private = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    unique_share_id = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_households", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    display_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    avatar_url = table.Column<string>(type: "text", nullable: true),
                    unique_share_id = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: false),
                    current_household_id = table.Column<Guid>(type: "uuid", nullable: true),
                    supabase_user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_profile_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    bio = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    favorite_cuisines = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                    table.ForeignKey(
                        name: "FK_users_households_current_household_id",
                        column: x => x.current_household_id,
                        principalTable: "households",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

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
                    table.CheckConstraint("CK_UserFriendship_NoSelf", "requester_user_id != target_user_id");
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
                });

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
                        name: "FK_user_recipes_global_recipes_global_recipe_id",
                        column: x => x.global_recipe_id,
                        principalTable: "global_recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_user_recipes_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ratings",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    global_recipe_id = table.Column<Guid>(type: "uuid", nullable: true),
                    household_recipe_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_recipe_id = table.Column<Guid>(type: "uuid", nullable: true),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    score = table.Column<int>(type: "integer", nullable: false),
                    comment = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ratings", x => x.id);
                    table.CheckConstraint("CK_Rating_Score", "score >= 0 AND score <= 10");
                    table.ForeignKey(
                        name: "FK_ratings_global_recipes_global_recipe_id",
                        column: x => x.global_recipe_id,
                        principalTable: "global_recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ratings_household_recipes_household_recipe_id",
                        column: x => x.household_recipe_id,
                        principalTable: "household_recipes",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_ratings_user_recipes_user_recipe_id",
                        column: x => x.user_recipe_id,
                        principalTable: "user_recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ratings_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_activity_feed_activity_type",
                table: "activity_feed",
                column: "activity_type");

            migrationBuilder.CreateIndex(
                name: "IX_activity_feed_created_at",
                table: "activity_feed",
                column: "created_at",
                descending: new bool[0]);

            migrationBuilder.CreateIndex(
                name: "IX_activity_feed_user_id",
                table: "activity_feed",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_activity_feed_user_id_created_at",
                table: "activity_feed",
                columns: new[] { "user_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "IX_etl_sync_log_started_at",
                table: "etl_sync_log",
                column: "started_at");

            migrationBuilder.CreateIndex(
                name: "IX_global_recipes_average_rating",
                table: "global_recipes",
                column: "average_rating");

            migrationBuilder.CreateIndex(
                name: "IX_global_recipes_created_by_user_id",
                table: "global_recipes",
                column: "created_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_global_recipes_hellofresh_uuid",
                table: "global_recipes",
                column: "hellofresh_uuid",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_global_recipes_is_hellofresh",
                table: "global_recipes",
                column: "is_hellofresh",
                filter: "is_hellofresh = true");

            migrationBuilder.CreateIndex(
                name: "IX_global_recipes_is_public",
                table: "global_recipes",
                column: "is_public",
                filter: "is_public = true");

            migrationBuilder.CreateIndex(
                name: "IX_global_recipes_published_from_user_recipe_id",
                table: "global_recipes",
                column: "published_from_user_recipe_id");

            migrationBuilder.CreateIndex(
                name: "IX_household_friendships_requested_by_user_id",
                table: "household_friendships",
                column: "requested_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_household_friendships_requester_household_id_target_househo~",
                table: "household_friendships",
                columns: new[] { "requester_household_id", "target_household_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_household_friendships_target_household_id",
                table: "household_friendships",
                column: "target_household_id");

            migrationBuilder.CreateIndex(
                name: "IX_household_invites_household_id",
                table: "household_invites",
                column: "household_id");

            migrationBuilder.CreateIndex(
                name: "IX_household_invites_invited_by_user_id",
                table: "household_invites",
                column: "invited_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_household_invites_invited_user_id",
                table: "household_invites",
                column: "invited_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_household_members_household_id_user_id",
                table: "household_members",
                columns: new[] { "household_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_household_members_user_id",
                table: "household_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_household_recipes_added_by_user_id",
                table: "household_recipes",
                column: "added_by_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_household_recipes_ArchivedById",
                table: "household_recipes",
                column: "ArchivedById");

            migrationBuilder.CreateIndex(
                name: "IX_household_recipes_global_recipe_id",
                table: "household_recipes",
                column: "global_recipe_id");

            migrationBuilder.CreateIndex(
                name: "IX_household_recipes_household_id_global_recipe_id",
                table: "household_recipes",
                columns: new[] { "household_id", "global_recipe_id" },
                unique: true,
                filter: "global_recipe_id IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_household_recipes_is_public",
                table: "household_recipes",
                column: "is_public",
                filter: "is_public = true");

            migrationBuilder.CreateIndex(
                name: "IX_households_leader_id",
                table: "households",
                column: "leader_id");

            migrationBuilder.CreateIndex(
                name: "IX_households_unique_share_id",
                table: "households",
                column: "unique_share_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ratings_global_recipe_id_user_id",
                table: "ratings",
                columns: new[] { "global_recipe_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ratings_household_recipe_id",
                table: "ratings",
                column: "household_recipe_id");

            migrationBuilder.CreateIndex(
                name: "IX_ratings_user_id",
                table: "ratings",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_ratings_user_recipe_id",
                table: "ratings",
                column: "user_recipe_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_friendships_requester_user_id",
                table: "user_friendships",
                column: "requester_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_friendships_requester_user_id_target_user_id",
                table: "user_friendships",
                columns: new[] { "requester_user_id", "target_user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_friendships_status",
                table: "user_friendships",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "IX_user_friendships_target_user_id",
                table: "user_friendships",
                column: "target_user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_recipes_created_at",
                table: "user_recipes",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "IX_user_recipes_global_recipe_id",
                table: "user_recipes",
                column: "global_recipe_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_recipes_user_id",
                table: "user_recipes",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_recipes_visibility",
                table: "user_recipes",
                column: "visibility");

            migrationBuilder.CreateIndex(
                name: "IX_users_current_household_id",
                table: "users",
                column: "current_household_id");

            migrationBuilder.CreateIndex(
                name: "IX_users_email",
                table: "users",
                column: "email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_unique_share_id",
                table: "users",
                column: "unique_share_id",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_activity_feed_users_user_id",
                table: "activity_feed",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_global_recipes_user_recipes_published_from_user_recipe_id",
                table: "global_recipes",
                column: "published_from_user_recipe_id",
                principalTable: "user_recipes",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_global_recipes_users_created_by_user_id",
                table: "global_recipes",
                column: "created_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_household_friendships_households_requester_household_id",
                table: "household_friendships",
                column: "requester_household_id",
                principalTable: "households",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_household_friendships_households_target_household_id",
                table: "household_friendships",
                column: "target_household_id",
                principalTable: "households",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_household_friendships_users_requested_by_user_id",
                table: "household_friendships",
                column: "requested_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_household_invites_households_household_id",
                table: "household_invites",
                column: "household_id",
                principalTable: "households",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_household_invites_users_invited_by_user_id",
                table: "household_invites",
                column: "invited_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_household_invites_users_invited_user_id",
                table: "household_invites",
                column: "invited_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_household_members_households_household_id",
                table: "household_members",
                column: "household_id",
                principalTable: "households",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_household_members_users_user_id",
                table: "household_members",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_household_recipes_households_household_id",
                table: "household_recipes",
                column: "household_id",
                principalTable: "households",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_household_recipes_users_ArchivedById",
                table: "household_recipes",
                column: "ArchivedById",
                principalTable: "users",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_household_recipes_users_added_by_user_id",
                table: "household_recipes",
                column: "added_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_households_users_leader_id",
                table: "households",
                column: "leader_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_global_recipes_users_created_by_user_id",
                table: "global_recipes");

            migrationBuilder.DropForeignKey(
                name: "FK_households_users_leader_id",
                table: "households");

            migrationBuilder.DropForeignKey(
                name: "FK_user_recipes_users_user_id",
                table: "user_recipes");

            migrationBuilder.DropForeignKey(
                name: "FK_global_recipes_user_recipes_published_from_user_recipe_id",
                table: "global_recipes");

            migrationBuilder.DropTable(
                name: "activity_feed");

            migrationBuilder.DropTable(
                name: "etl_sync_log");

            migrationBuilder.DropTable(
                name: "household_friendships");

            migrationBuilder.DropTable(
                name: "household_invites");

            migrationBuilder.DropTable(
                name: "household_members");

            migrationBuilder.DropTable(
                name: "ratings");

            migrationBuilder.DropTable(
                name: "user_friendships");

            migrationBuilder.DropTable(
                name: "household_recipes");

            migrationBuilder.DropTable(
                name: "users");

            migrationBuilder.DropTable(
                name: "households");

            migrationBuilder.DropTable(
                name: "user_recipes");

            migrationBuilder.DropTable(
                name: "global_recipes");
        }
    }
}
