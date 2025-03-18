using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventree_App.Migrations
{
    /// <inheritdoc />
    public partial class AddStockCategoryTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
              name: "Email",
              table: "Stocks",
              type: "longtext",
              nullable: true)
              .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "Stocks",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Stocks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedOn",
                table: "Stocks",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "LocationId",
                table: "Stocks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedOn",
                table: "Stocks",
                type: "datetime(6)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "CreatedOn",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "Stocks");

            migrationBuilder.DropColumn(
                name: "UpdatedOn",
                table: "Stocks");
            migrationBuilder.DropColumn(
           name: "Email",
           table: "Stocks");


        }
    }
}
