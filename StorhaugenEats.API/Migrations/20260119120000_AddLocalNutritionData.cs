using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorhaugenEats.API.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalNutritionData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "local_nutrition_data",
                table: "user_recipes",
                type: "jsonb",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "local_nutrition_data",
                table: "user_recipes");
        }
    }
}
