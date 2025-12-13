using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorhaugenEats.API.Migrations
{
    /// <inheritdoc />
    public partial class AddHellofreshWeekColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "hellofresh_week",
                table: "global_recipes",
                type: "character varying(20)",
                maxLength: 20,
                nullable: true);

            // Add index for week filtering
            migrationBuilder.CreateIndex(
                name: "ix_global_recipes_hellofresh_week",
                table: "global_recipes",
                column: "hellofresh_week");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_global_recipes_hellofresh_week",
                table: "global_recipes");

            migrationBuilder.DropColumn(
                name: "hellofresh_week",
                table: "global_recipes");
        }
    }
}
