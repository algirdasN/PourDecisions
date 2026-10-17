using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PourDecisions.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddIngredientTypeDeleteRestriction : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CocktailIngredients_IngredientTypes_TypeId",
                table: "CocktailIngredients");

            migrationBuilder.AddForeignKey(
                name: "FK_CocktailIngredients_IngredientTypes_TypeId",
                table: "CocktailIngredients",
                column: "TypeId",
                principalTable: "IngredientTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_CocktailIngredients_IngredientTypes_TypeId",
                table: "CocktailIngredients");

            migrationBuilder.AddForeignKey(
                name: "FK_CocktailIngredients_IngredientTypes_TypeId",
                table: "CocktailIngredients",
                column: "TypeId",
                principalTable: "IngredientTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
