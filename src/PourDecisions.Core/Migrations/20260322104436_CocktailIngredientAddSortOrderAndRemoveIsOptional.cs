using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PourDecisions.Core.Migrations
{
    /// <inheritdoc />
    public partial class CocktailIngredientAddSortOrderAndRemoveIsOptional : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsOptional",
                table: "CocktailIngredients",
                newName: "SortOrder");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SortOrder",
                table: "CocktailIngredients",
                newName: "IsOptional");
        }
    }
}
