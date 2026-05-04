using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _211system.Migrations
{
    /// <inheritdoc />
    public partial class mapki : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "PoliceDepartments",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "PoliceDepartments",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "OperatingRadiusKm",
                table: "PoliceDepartments",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Hospitals",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Hospitals",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "OperatingRadiusKm",
                table: "Hospitals",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "FireDepartments",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "FireDepartments",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "OperatingRadiusKm",
                table: "FireDepartments",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "PoliceDepartments");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "PoliceDepartments");

            migrationBuilder.DropColumn(
                name: "OperatingRadiusKm",
                table: "PoliceDepartments");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Hospitals");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Hospitals");

            migrationBuilder.DropColumn(
                name: "OperatingRadiusKm",
                table: "Hospitals");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "FireDepartments");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "FireDepartments");

            migrationBuilder.DropColumn(
                name: "OperatingRadiusKm",
                table: "FireDepartments");
        }
    }
}
