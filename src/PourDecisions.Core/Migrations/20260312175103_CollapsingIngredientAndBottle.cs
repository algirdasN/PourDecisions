using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PourDecisions.Core.Migrations
{
    /// <inheritdoc />
    public partial class CollapsingIngredientAndBottle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bottles_Ingredients_IngredientId",
                table: "Bottles");

            migrationBuilder.DropTable(
                name: "Ingredients");

            migrationBuilder.DropIndex(
                name: "IX_Bottles_IngredientId",
                table: "Bottles");

            migrationBuilder.RenameColumn(
                name: "IngredientId",
                table: "Bottles",
                newName: "Volume");

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "Bottles",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "TypeId",
                table: "Bottles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Bottles_TypeId",
                table: "Bottles",
                column: "TypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bottles_IngredientTypes_TypeId",
                table: "Bottles",
                column: "TypeId",
                principalTable: "IngredientTypes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bottles_IngredientTypes_TypeId",
                table: "Bottles");

            migrationBuilder.DropIndex(
                name: "IX_Bottles_TypeId",
                table: "Bottles");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "Bottles");

            migrationBuilder.DropColumn(
                name: "TypeId",
                table: "Bottles");

            migrationBuilder.RenameColumn(
                name: "Volume",
                table: "Bottles",
                newName: "IngredientId");

            migrationBuilder.CreateTable(
                name: "Ingredients",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    TypeId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ingredients", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ingredients_IngredientTypes_TypeId",
                        column: x => x.TypeId,
                        principalTable: "IngredientTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bottles_IngredientId",
                table: "Bottles",
                column: "IngredientId");

            migrationBuilder.CreateIndex(
                name: "IX_Ingredients_TypeId",
                table: "Ingredients",
                column: "TypeId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bottles_Ingredients_IngredientId",
                table: "Bottles",
                column: "IngredientId",
                principalTable: "Ingredients",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
