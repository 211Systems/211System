using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _211system.Migrations
{
    /// <inheritdoc />
    public partial class AddParamedicToAmbulance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ParamedicId",
                table: "Ambulances",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ambulances_ParamedicId",
                table: "Ambulances",
                column: "ParamedicId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ambulances_Paramedics_ParamedicId",
                table: "Ambulances",
                column: "ParamedicId",
                principalTable: "Paramedics",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ambulances_Paramedics_ParamedicId",
                table: "Ambulances");

            migrationBuilder.DropIndex(
                name: "IX_Ambulances_ParamedicId",
                table: "Ambulances");

            migrationBuilder.DropColumn(
                name: "ParamedicId",
                table: "Ambulances");
        }
    }
}
