using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace _211system.Migrations
{
    /// <inheritdoc />
    public partial class AddWeatherToIncident : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "WeatherCondition",
                table: "Incidents",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "WeatherTemperature",
                table: "Incidents",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "WeatherCondition",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "WeatherTemperature",
                table: "Incidents");
        }
    }
}
