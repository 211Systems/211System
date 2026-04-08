using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _211system.Migrations
{
    /// <inheritdoc />
    public partial class AddDispatchFieldsToPoliceAndFire : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CurrentIncidentId",
                table: "PoliceCars",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "PoliceCars",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<Guid>(
                name: "PolicemanId",
                table: "PoliceCars",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "CurrentIncidentId",
                table: "FireTrucks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FiremanId",
                table: "FireTrucks",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsAvailable",
                table: "FireTrucks",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_PoliceCars_PolicemanId",
                table: "PoliceCars",
                column: "PolicemanId");

            migrationBuilder.CreateIndex(
                name: "IX_FireTrucks_FiremanId",
                table: "FireTrucks",
                column: "FiremanId");

            migrationBuilder.AddForeignKey(
                name: "FK_FireTrucks_Firemen_FiremanId",
                table: "FireTrucks",
                column: "FiremanId",
                principalTable: "Firemen",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PoliceCars_Policemen_PolicemanId",
                table: "PoliceCars",
                column: "PolicemanId",
                principalTable: "Policemen",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FireTrucks_Firemen_FiremanId",
                table: "FireTrucks");

            migrationBuilder.DropForeignKey(
                name: "FK_PoliceCars_Policemen_PolicemanId",
                table: "PoliceCars");

            migrationBuilder.DropIndex(
                name: "IX_PoliceCars_PolicemanId",
                table: "PoliceCars");

            migrationBuilder.DropIndex(
                name: "IX_FireTrucks_FiremanId",
                table: "FireTrucks");

            migrationBuilder.DropColumn(
                name: "CurrentIncidentId",
                table: "PoliceCars");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "PoliceCars");

            migrationBuilder.DropColumn(
                name: "PolicemanId",
                table: "PoliceCars");

            migrationBuilder.DropColumn(
                name: "CurrentIncidentId",
                table: "FireTrucks");

            migrationBuilder.DropColumn(
                name: "FiremanId",
                table: "FireTrucks");

            migrationBuilder.DropColumn(
                name: "IsAvailable",
                table: "FireTrucks");
        }
    }
}
