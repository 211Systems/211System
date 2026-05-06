using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _211system.Migrations
{
    /// <inheritdoc />
    public partial class AddIndependentVehicleGps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "PoliceCars",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "PoliceCars",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "PoliceCars",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "FireTrucks",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "FireTrucks",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "FireTrucks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Ambulances",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Ambulances",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Ambulances",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "PoliceCars");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "PoliceCars");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "PoliceCars");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "FireTrucks");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "FireTrucks");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "FireTrucks");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Ambulances");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Ambulances");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Ambulances");
        }
    }
}
