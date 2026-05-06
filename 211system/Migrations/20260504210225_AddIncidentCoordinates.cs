using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _211system.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentCoordinates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Incidents_Encs_LocationId",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_LocationId",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "LocationId",
                table: "Incidents");

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Incidents",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Incidents",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Incidents");

            migrationBuilder.AddColumn<Guid>(
                name: "LocationId",
                table: "Incidents",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_LocationId",
                table: "Incidents",
                column: "LocationId");

            migrationBuilder.AddForeignKey(
                name: "FK_Incidents_Encs_LocationId",
                table: "Incidents",
                column: "LocationId",
                principalTable: "Encs",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
