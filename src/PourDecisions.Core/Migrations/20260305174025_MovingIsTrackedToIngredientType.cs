using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PourDecisions.Core.Migrations
{
    /// <inheritdoc />
    public partial class MovingIsTrackedToIngredientType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTracked",
                table: "Ingredients");

            migrationBuilder.RenameColumn(
                name: "IsTrackedByDefault",
                table: "IngredientTypes",
                newName: "IsTracked");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsTracked",
                table: "IngredientTypes",
                newName: "IsTrackedByDefault");

            migrationBuilder.AddColumn<bool>(
                name: "IsTracked",
                table: "Ingredients",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);
        }
    }
}
