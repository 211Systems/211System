using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace _211system.Migrations
{
    /// <inheritdoc />
    public partial class RefaktorIncydentowNaSlowniki : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Severity",
                table: "Incidents");

            migrationBuilder.AddColumn<int>(
                name: "IncidentTypeId",
                table: "Incidents",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SeverityLevelId",
                table: "Incidents",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "IncidentStatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uuid", nullable: false),
                    OperatorId = table.Column<Guid>(type: "uuid", nullable: true),
                    OldStatus = table.Column<string>(type: "text", nullable: false),
                    NewStatus = table.Column<string>(type: "text", nullable: false),
                    ChangedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentStatusHistories_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IncidentTypes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    RequiresPolice = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresFire = table.Column<bool>(type: "boolean", nullable: false),
                    RequiresMedic = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SeverityLevels",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    ColorCode = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SeverityLevels", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "IncidentTypes",
                columns: new[] { "Id", "Name", "RequiresFire", "RequiresMedic", "RequiresPolice" },
                values: new object[,]
                {
                    { 1, "Wypadek drogowy", true, true, true },
                    { 2, "Pożar budynku", true, true, true },
                    { 3, "Zatrzymanie krążenia", false, true, false },
                    { 4, "Kradzież / Włamanie", false, false, true },
                    { 5, "Zagrożenie miejscowe (drzewo/woda)", true, false, false }

                });

            migrationBuilder.InsertData(
                table: "SeverityLevels",
                columns: new[] { "Id", "ColorCode", "Name" },
                values: new object[,]
                {
                    { 1, "info", "Niski" },
                    { 2, "warning", "Średni" },
                    { 3, "danger", "Wysoki" },
                    { 4, "dark", "Krytyczny" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_IncidentTypeId",
                table: "Incidents",
                column: "IncidentTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_SeverityLevelId",
                table: "Incidents",
                column: "SeverityLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentStatusHistories_IncidentId",
                table: "IncidentStatusHistories",
                column: "IncidentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Incidents_IncidentTypes_IncidentTypeId",
                table: "Incidents",
                column: "IncidentTypeId",
                principalTable: "IncidentTypes",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Incidents_SeverityLevels_SeverityLevelId",
                table: "Incidents",
                column: "SeverityLevelId",
                principalTable: "SeverityLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Incidents_IncidentTypes_IncidentTypeId",
                table: "Incidents");

            migrationBuilder.DropForeignKey(
                name: "FK_Incidents_SeverityLevels_SeverityLevelId",
                table: "Incidents");

            migrationBuilder.DropTable(
                name: "IncidentStatusHistories");

            migrationBuilder.DropTable(
                name: "IncidentTypes");

            migrationBuilder.DropTable(
                name: "SeverityLevels");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_IncidentTypeId",
                table: "Incidents");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_SeverityLevelId",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "IncidentTypeId",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "SeverityLevelId",
                table: "Incidents");

            migrationBuilder.AddColumn<string>(
                name: "Severity",
                table: "Incidents",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
