using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _211system.Migrations
{
    /// <inheritdoc />
    public partial class g2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalOperations_Incidents_IncidentId",
                table: "MedicalOperations");

            migrationBuilder.AlterColumn<Guid>(
                name: "IncidentId",
                table: "MedicalOperations",
                type: "uuid",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uuid");

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalOperations_Incidents_IncidentId",
                table: "MedicalOperations",
                column: "IncidentId",
                principalTable: "Incidents",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MedicalOperations_Incidents_IncidentId",
                table: "MedicalOperations");

            migrationBuilder.AlterColumn<Guid>(
                name: "IncidentId",
                table: "MedicalOperations",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MedicalOperations_Incidents_IncidentId",
                table: "MedicalOperations",
                column: "IncidentId",
                principalTable: "Incidents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
