using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inventree_App.Migrations
{
    /// <inheritdoc />
    public partial class labequipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaxQuantity",
                table: "LabEquipment",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "LabEquipment",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaxQuantity",
                table: "LabEquipment");

            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "LabEquipment");
        }
    }
}
