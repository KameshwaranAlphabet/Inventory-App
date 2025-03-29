using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventree_App.Migrations
{
    /// <inheritdoc />
    public partial class UnitTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SubUnitType",
                table: "Stocks",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "UnitCapacity",
                table: "Stocks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UnitQuantity",
                table: "Stocks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UnitType",
                table: "Stocks",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SubUnitType",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "UnitCapacity",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "UnitQuantity",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "UnitType",
                table: "Stocks");
        }
    }
}
