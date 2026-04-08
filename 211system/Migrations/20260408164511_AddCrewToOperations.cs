using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _211system.Migrations
{
    /// <inheritdoc />
    public partial class AddCrewToOperations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "PolicemanId",
                table: "PoliceOperations",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "FiremanId",
                table: "FireOperations",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PoliceOperations_PolicemanId",
                table: "PoliceOperations",
                column: "PolicemanId");

            migrationBuilder.CreateIndex(
                name: "IX_FireOperations_FiremanId",
                table: "FireOperations",
                column: "FiremanId");

            migrationBuilder.AddForeignKey(
                name: "FK_FireOperations_Firemen_FiremanId",
                table: "FireOperations",
                column: "FiremanId",
                principalTable: "Firemen",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PoliceOperations_Policemen_PolicemanId",
                table: "PoliceOperations",
                column: "PolicemanId",
                principalTable: "Policemen",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FireOperations_Firemen_FiremanId",
                table: "FireOperations");

            migrationBuilder.DropForeignKey(
                name: "FK_PoliceOperations_Policemen_PolicemanId",
                table: "PoliceOperations");

            migrationBuilder.DropIndex(
                name: "IX_PoliceOperations_PolicemanId",
                table: "PoliceOperations");

            migrationBuilder.DropIndex(
                name: "IX_FireOperations_FiremanId",
                table: "FireOperations");

            migrationBuilder.DropColumn(
                name: "PolicemanId",
                table: "PoliceOperations");

            migrationBuilder.DropColumn(
                name: "FiremanId",
                table: "FireOperations");
        }
    }
}
