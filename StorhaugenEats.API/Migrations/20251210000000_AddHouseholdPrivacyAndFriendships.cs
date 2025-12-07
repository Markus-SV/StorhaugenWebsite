using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorhaugenEats.API.Migrations
{
    /// <inheritdoc />
    public partial class AddHouseholdPrivacyAndFriendships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                });

            migrationBuilder.CreateIndex(
                name: "IX_households_unique_share_id",
                table: "households",
                column: "unique_share_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_household_friendships_requester_household_id_target_household_id",
                table: "household_friendships",
                columns: new[] { "requester_household_id", "target_household_id" },
                unique: true);
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
        }
    }
}
