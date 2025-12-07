using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorhaugenEats.API.Migrations
{
    /// <inheritdoc />
    public partial class AddPublicRecipesAndFriendships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Guid>(
                name: "global_recipe_id",
                table: "ratings",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddColumn<bool>(
                name: "is_private",
                table: "households",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "unique_share_id",
                table: "households",
                type: "character varying(12)",
                maxLength: 12,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "is_public",
                table: "household_recipes",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "household_friendships",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    requester_household_id = table.Column<Guid>(type: "uuid", nullable: false),
                    target_household_id = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "pending"),
                    requested_by_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    responded_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
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
                        principalColumn: "id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_households_unique_share_id",
                table: "households",
                column: "unique_share_id",
                unique: true);

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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "household_friendships");

            migrationBuilder.DropIndex(
                name: "IX_households_unique_share_id",
                table: "households");

            migrationBuilder.DropColumn(
                name: "is_private",
                table: "households");

            migrationBuilder.DropColumn(
                name: "unique_share_id",
                table: "households");

            migrationBuilder.DropColumn(
                name: "is_public",
                table: "household_recipes");

            migrationBuilder.AlterColumn<Guid>(
                name: "global_recipe_id",
                table: "ratings",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);
        }
    }
}
