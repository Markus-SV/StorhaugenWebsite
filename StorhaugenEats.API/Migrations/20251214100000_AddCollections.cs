using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorhaugenEats.API.Migrations
{
    /// <inheritdoc />
    public partial class AddCollections : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create collections table
            migrationBuilder.CreateTable(
                name: "collections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    image_url = table.Column<string>(type: "text", nullable: true),
                    color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true),
                    icon = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    is_default = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    is_shared = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    unique_share_id = table.Column<string>(type: "character varying(12)", maxLength: 12, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_collections", x => x.id);
                    table.ForeignKey(
                        name: "fk_collections_users_owner_user_id",
                        column: x => x.owner_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            // Create collection_members table
            migrationBuilder.CreateTable(
                name: "collection_members",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    collection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    role = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "member"),
                    joined_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    invited_by_user_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_collection_members", x => x.id);
                    table.ForeignKey(
                        name: "fk_collection_members_collections_collection_id",
                        column: x => x.collection_id,
                        principalTable: "collections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_collection_members_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_collection_members_users_invited_by_user_id",
                        column: x => x.invited_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.SetNull);
                });

            // Create user_recipe_collections table (join table)
            migrationBuilder.CreateTable(
                name: "user_recipe_collections",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                    collection_id = table.Column<Guid>(type: "uuid", nullable: false),
                    added_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    added_by_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_user_recipe_collections", x => x.id);
                    table.ForeignKey(
                        name: "fk_user_recipe_collections_user_recipes_user_recipe_id",
                        column: x => x.user_recipe_id,
                        principalTable: "user_recipes",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_recipe_collections_collections_collection_id",
                        column: x => x.collection_id,
                        principalTable: "collections",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_user_recipe_collections_users_added_by_user_id",
                        column: x => x.added_by_user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Create indexes for collections
            migrationBuilder.CreateIndex(
                name: "ix_collections_owner_user_id",
                table: "collections",
                column: "owner_user_id");

            migrationBuilder.CreateIndex(
                name: "ix_collections_owner_user_id_sort_order",
                table: "collections",
                columns: new[] { "owner_user_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_collections_unique_share_id",
                table: "collections",
                column: "unique_share_id",
                unique: true,
                filter: "unique_share_id IS NOT NULL");

            // Create indexes for collection_members
            migrationBuilder.CreateIndex(
                name: "ix_collection_members_collection_id",
                table: "collection_members",
                column: "collection_id");

            migrationBuilder.CreateIndex(
                name: "ix_collection_members_user_id",
                table: "collection_members",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "ix_collection_members_collection_id_user_id",
                table: "collection_members",
                columns: new[] { "collection_id", "user_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_collection_members_invited_by_user_id",
                table: "collection_members",
                column: "invited_by_user_id");

            // Create indexes for user_recipe_collections
            migrationBuilder.CreateIndex(
                name: "ix_user_recipe_collections_user_recipe_id",
                table: "user_recipe_collections",
                column: "user_recipe_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_recipe_collections_collection_id",
                table: "user_recipe_collections",
                column: "collection_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_recipe_collections_user_recipe_id_collection_id",
                table: "user_recipe_collections",
                columns: new[] { "user_recipe_id", "collection_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_recipe_collections_collection_id_sort_order",
                table: "user_recipe_collections",
                columns: new[] { "collection_id", "sort_order" });

            migrationBuilder.CreateIndex(
                name: "ix_user_recipe_collections_added_by_user_id",
                table: "user_recipe_collections",
                column: "added_by_user_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Drop indexes first
            migrationBuilder.DropIndex(name: "ix_user_recipe_collections_added_by_user_id", table: "user_recipe_collections");
            migrationBuilder.DropIndex(name: "ix_user_recipe_collections_collection_id_sort_order", table: "user_recipe_collections");
            migrationBuilder.DropIndex(name: "ix_user_recipe_collections_user_recipe_id_collection_id", table: "user_recipe_collections");
            migrationBuilder.DropIndex(name: "ix_user_recipe_collections_collection_id", table: "user_recipe_collections");
            migrationBuilder.DropIndex(name: "ix_user_recipe_collections_user_recipe_id", table: "user_recipe_collections");

            migrationBuilder.DropIndex(name: "ix_collection_members_invited_by_user_id", table: "collection_members");
            migrationBuilder.DropIndex(name: "ix_collection_members_collection_id_user_id", table: "collection_members");
            migrationBuilder.DropIndex(name: "ix_collection_members_user_id", table: "collection_members");
            migrationBuilder.DropIndex(name: "ix_collection_members_collection_id", table: "collection_members");

            migrationBuilder.DropIndex(name: "ix_collections_unique_share_id", table: "collections");
            migrationBuilder.DropIndex(name: "ix_collections_owner_user_id_sort_order", table: "collections");
            migrationBuilder.DropIndex(name: "ix_collections_owner_user_id", table: "collections");

            // Drop tables in reverse order (due to foreign key constraints)
            migrationBuilder.DropTable(name: "user_recipe_collections");
            migrationBuilder.DropTable(name: "collection_members");
            migrationBuilder.DropTable(name: "collections");
        }
    }
}
