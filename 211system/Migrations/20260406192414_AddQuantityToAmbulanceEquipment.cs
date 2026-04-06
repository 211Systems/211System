using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _211system.Migrations
{
    /// <inheritdoc />
    public partial class AddQuantityToAmbulanceEquipment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ItemName",
                table: "AmbulanceEquipments",
                newName: "Name");

            migrationBuilder.AddColumn<int>(
                name: "Quantity",
                table: "AmbulanceEquipments",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Quantity",
                table: "AmbulanceEquipments");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "AmbulanceEquipments",
                newName: "ItemName");
        }
    }
}
