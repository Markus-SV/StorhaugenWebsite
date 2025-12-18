using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorhaugenEats.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCollectionVisibility : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ratings_household_recipes_household_recipe_id",
                table: "ratings");

            migrationBuilder.DropForeignKey(
                name: "FK_users_households_current_household_id",
                table: "users");

            migrationBuilder.DropTable(
                name: "household_friendships");

            migrationBuilder.DropTable(
                name: "household_invites");

            migrationBuilder.DropTable(
                name: "household_members");

            migrationBuilder.DropTable(
                name: "household_recipes");

            migrationBuilder.DropTable(
                name: "households");

            migrationBuilder.DropIndex(
                name: "IX_users_current_household_id",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_user_recipes_global_recipe_id",
                table: "user_recipes");

            migrationBuilder.DropIndex(
                name: "IX_ratings_household_recipe_id",
                table: "ratings");

            migrationBuilder.DropIndex(
                name: "IX_collections_owner_user_id_sort_order",
                table: "collections");

            migrationBuilder.DropIndex(
                name: "IX_collections_unique_share_id",
                table: "collections");

            migrationBuilder.DropColumn(
                name: "current_household_id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "household_recipe_id",
                table: "ratings");

            migrationBuilder.AddColumn<int>(
                name: "local_cook_time_minutes",
                table: "user_recipes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "local_cuisine",
                table: "user_recipes",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "local_difficulty",
                table: "user_recipes",
                type: "character varying(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "local_prep_time_minutes",
                table: "user_recipes",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "local_servings",
                table: "user_recipes",
                type: "integer",
                nullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "score",
                table: "ratings",
                type: "numeric",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.AddColumn<string>(
                name: "hellofresh_week",
                table: "global_recipes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "visibility",
                table: "collections",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "recipe_tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_recipe_tags", x => x.id);
                    table.ForeignKey(
                        name: "FK_recipe_tags_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "user_recipe_tags",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_recipe_tags", x => x.id);
                    table.ForeignKey(
                        name: "FK_user_recipe_tags_recipe_tags_tag_id",
                        column: x => x.tag_id,
                        principalTable: "recipe_tags",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_user_recipe_tags_user_recipes_user_recipe_id",
                        column: x => x.user_recipe_id,
                        principalTable: "user_recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_user_recipes_global_recipe_id",
                table: "user_recipes",
                column: "global_recipe_id");

            migrationBuilder.CreateIndex(
                name: "IX_collections_unique_share_id",
                table: "collections",
                column: "unique_share_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_recipe_tags_user_id",
                table: "recipe_tags",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "IX_recipe_tags_user_id_name",
                table: "recipe_tags",
                columns: new[] { "user_id", "name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_recipe_tags_tag_id",
                table: "user_recipe_tags",
                column: "tag_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_recipe_tags_user_recipe_id_tag_id",
                table: "user_recipe_tags",
                columns: new[] { "user_recipe_id", "tag_id" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_collection_members_collections_collection_id",
                table: "collection_members",
                column: "collection_id",
                principalTable: "collections",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_collection_members_users_invited_by_user_id",
                table: "collection_members",
                column: "invited_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_collection_members_users_user_id",
                table: "collection_members",
                column: "user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_collections_users_owner_user_id",
                table: "collections",
                column: "owner_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_recipe_collections_collections_collection_id",
                table: "user_recipe_collections",
                column: "collection_id",
                principalTable: "collections",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_recipe_collections_user_recipes_user_recipe_id",
                table: "user_recipe_collections",
                column: "user_recipe_id",
                principalTable: "user_recipes",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_user_recipe_collections_users_added_by_user_id",
                table: "user_recipe_collections",
                column: "added_by_user_id",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_collection_members_collections_collection_id",
                table: "collection_members");

            migrationBuilder.DropForeignKey(
                name: "FK_collection_members_users_invited_by_user_id",
                table: "collection_members");

            migrationBuilder.DropForeignKey(
                name: "FK_collection_members_users_user_id",
                table: "collection_members");

            migrationBuilder.DropForeignKey(
                name: "FK_collections_users_owner_user_id",
                table: "collections");

            migrationBuilder.DropForeignKey(
                name: "FK_user_recipe_collections_collections_collection_id",
                table: "user_recipe_collections");

            migrationBuilder.DropForeignKey(
                name: "FK_user_recipe_collections_user_recipes_user_recipe_id",
                table: "user_recipe_collections");

            migrationBuilder.DropForeignKey(
                name: "FK_user_recipe_collections_users_added_by_user_id",
                table: "user_recipe_collections");

            migrationBuilder.DropTable(
                name: "user_recipe_tags");

            migrationBuilder.DropTable(
                name: "recipe_tags");

            migrationBuilder.DropIndex(
                name: "IX_user_recipes_global_recipe_id",
                table: "user_recipes");

            migrationBuilder.DropIndex(
                name: "IX_collections_unique_share_id",
                table: "collections");

            migrationBuilder.DropColumn(
                name: "local_cook_time_minutes",
                table: "user_recipes");

            migrationBuilder.DropColumn(
                name: "local_cuisine",
                table: "user_recipes");

            migrationBuilder.DropColumn(
                name: "local_difficulty",
                table: "user_recipes");

            migrationBuilder.DropColumn(
                name: "local_prep_time_minutes",
                table: "user_recipes");

            migrationBuilder.DropColumn(
                name: "local_servings",
                table: "user_recipes");

            migrationBuilder.DropColumn(
                name: "hellofresh_week",
                table: "global_recipes");

            migrationBuilder.DropColumn(
                name: "visibility",
                table: "collections");

            migrationBuilder.AddColumn<Guid>(
                name: "current_household_id",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "score",
                table: "ratings",
                type: "integer",
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "numeric");

            migrationBuilder.AddColumn<Guid>(
                name: "household_recipe_id",
                table: "ratings",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "households",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    leader_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_private = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    settings = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                    unique_share_id = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_households", x => x.id);
                    table.ForeignKey(
                        name: "FK_households_users_leader_id",
                        column: x => x.leader_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "household_friendships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    requester_household_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_household_id = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    message = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    responded_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_household_friendships", x => x.id);
                    table.ForeignKey(
                        name: "FK_household_friendships_households_requester_household_id",
                        column: x => x.requester_household_id,
                        principalTable: "households",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_household_friendships_households_target_household_id",
                        column: x => x.target_household_id,
                        principalTable: "households",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_household_friendships_users_requested_by_user_id",
                        column: x => x.requested_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "household_invites",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    household_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invited_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    invited_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    invited_email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    merge_requested = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_household_invites", x => x.id);
                    table.ForeignKey(
                        name: "FK_household_invites_households_household_id",
                        column: x => x.household_id,
                        principalTable: "households",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_household_invites_users_invited_by_user_id",
                        column: x => x.invited_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_household_invites_users_invited_user_id",
                        column: x => x.invited_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "household_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    household_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "member")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_household_members", x => x.id);
                    table.ForeignKey(
                        name: "FK_household_members_households_household_id",
                        column: x => x.household_id,
                        principalTable: "households",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_household_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "household_recipes",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    ArchivedById = table.Column<Guid>(type: "uuid", nullable: true),
                    global_recipe_id = table.Column<Guid>(type: "uuid", nullable: true),
                    household_id = table.Column<Guid>(type: "uuid", nullable: false),
                    archived_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    archived_date = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    is_archived = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_public = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    local_description = table.Column<string>(type: "text", nullable: true),
                    local_image_url = table.Column<string>(type: "text", nullable: true),
                    local_ingredients = table.Column<string>(type: "jsonb", nullable: true),
                    local_title = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    personal_notes = table.Column<string>(type: "text", nullable: true),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
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
                    table.ForeignKey(
                        name: "FK_household_recipes_households_household_id",
                        column: x => x.household_id,
                        principalTable: "households",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_household_recipes_users_ArchivedById",
                        column: x => x.ArchivedById,
                        principalTable: "users",
                        principalColumn: "id");
                    table.ForeignKey(
                        name: "FK_household_recipes_users_added_by_user_id",
                        column: x => x.added_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_users_current_household_id",
                table: "users",
                column: "current_household_id");

            migrationBuilder.CreateIndex(
                name: "IX_user_recipes_global_recipe_id",
                table: "user_recipes",
                column: "global_recipe_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ratings_household_recipe_id",
                table: "ratings",
                column: "household_recipe_id");

            migrationBuilder.CreateIndex(
                name: "IX_collections_owner_user_id_sort_order",
                table: "collections",
                columns: new[] { "owner_user_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "IX_collections_unique_share_id",
                table: "collections",
                column: "unique_share_id",
                unique: true,
                filter: "unique_share_id IS NOT NULL");

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

            migrationBuilder.AddForeignKey(
                name: "FK_ratings_household_recipes_household_recipe_id",
                table: "ratings",
                column: "household_recipe_id",
                principalTable: "household_recipes",
                principalColumn: "id");

            migrationBuilder.AddForeignKey(
                name: "FK_users_households_current_household_id",
                table: "users",
                column: "current_household_id",
                principalTable: "households",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }
    }
}
