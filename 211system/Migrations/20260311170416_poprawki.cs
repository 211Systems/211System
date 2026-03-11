using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _211system.Migrations
{
    /// <inheritdoc />
    public partial class poprawki : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Firemen_FireDepartments_PDepartmentId",
                table: "Firemen");

            migrationBuilder.DropForeignKey(
                name: "FK_FireTrucks_FireDepartments_DepartmentPDepartmentId",
                table: "FireTrucks");

            migrationBuilder.DropIndex(
                name: "IX_FireTrucks_DepartmentPDepartmentId",
                table: "FireTrucks");

            migrationBuilder.DropColumn(
                name: "DepartmentPDepartmentId",
                table: "FireTrucks");

            migrationBuilder.RenameColumn(
                name: "PDepartmentId",
                table: "FireTrucks",
                newName: "FDepartmentId");

            migrationBuilder.RenameColumn(
                name: "PDepartmentId",
                table: "Firemen",
                newName: "FDepartmentId");

            migrationBuilder.RenameIndex(
                name: "IX_Firemen_PDepartmentId",
                table: "Firemen",
                newName: "IX_Firemen_FDepartmentId");

            migrationBuilder.RenameColumn(
                name: "PDepartmentId",
                table: "FireDepartments",
                newName: "FDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FireTrucks_FDepartmentId",
                table: "FireTrucks",
                column: "FDepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Firemen_FireDepartments_FDepartmentId",
                table: "Firemen",
                column: "FDepartmentId",
                principalTable: "FireDepartments",
                principalColumn: "FDepartmentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FireTrucks_FireDepartments_FDepartmentId",
                table: "FireTrucks",
                column: "FDepartmentId",
                principalTable: "FireDepartments",
                principalColumn: "FDepartmentId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Firemen_FireDepartments_FDepartmentId",
                table: "Firemen");

            migrationBuilder.DropForeignKey(
                name: "FK_FireTrucks_FireDepartments_FDepartmentId",
                table: "FireTrucks");

            migrationBuilder.DropIndex(
                name: "IX_FireTrucks_FDepartmentId",
                table: "FireTrucks");

            migrationBuilder.RenameColumn(
                name: "FDepartmentId",
                table: "FireTrucks",
                newName: "PDepartmentId");

            migrationBuilder.RenameColumn(
                name: "FDepartmentId",
                table: "Firemen",
                newName: "PDepartmentId");

            migrationBuilder.RenameIndex(
                name: "IX_Firemen_FDepartmentId",
                table: "Firemen",
                newName: "IX_Firemen_PDepartmentId");

            migrationBuilder.RenameColumn(
                name: "FDepartmentId",
                table: "FireDepartments",
                newName: "PDepartmentId");

            migrationBuilder.AddColumn<Guid>(
                name: "DepartmentPDepartmentId",
                table: "FireTrucks",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_FireTrucks_DepartmentPDepartmentId",
                table: "FireTrucks",
                column: "DepartmentPDepartmentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Firemen_FireDepartments_PDepartmentId",
                table: "Firemen",
                column: "PDepartmentId",
                principalTable: "FireDepartments",
                principalColumn: "PDepartmentId",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FireTrucks_FireDepartments_DepartmentPDepartmentId",
                table: "FireTrucks",
                column: "DepartmentPDepartmentId",
                principalTable: "FireDepartments",
                principalColumn: "PDepartmentId",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
