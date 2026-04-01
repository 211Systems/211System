using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _211system.Migrations
{
    /// <inheritdoc />
    public partial class UpdateAmbulanceStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrentIncidentId",
                table: "Ambulances",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "Ambulances",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentIncidentId",
                table: "Ambulances");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "Ambulances");
        }
    }
}
