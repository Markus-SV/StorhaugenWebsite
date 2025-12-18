using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace StorhaugenEats.API.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalRecipeMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
