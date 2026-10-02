using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergencyResponse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEnvironmentalContext : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EnvironmentalGridSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CellKey = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ObservedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TemperatureC = table.Column<double>(type: "float", nullable: true),
                    WindSpeedKph = table.Column<double>(type: "float", nullable: true),
                    PrecipitationMm = table.Column<double>(type: "float", nullable: true),
                    VisibilityKm = table.Column<double>(type: "float", nullable: true),
                    AirQualityIndex = table.Column<double>(type: "float", nullable: true),
                    HasSevereWeatherAlert = table.Column<bool>(type: "bit", nullable: false),
                    SevereAlertTypesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TrafficContextJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContextHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvironmentalGridSnapshots", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IncidentEnvironmentalContexts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ObservedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ProviderVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Latitude = table.Column<double>(type: "float", nullable: false),
                    Longitude = table.Column<double>(type: "float", nullable: false),
                    TemperatureC = table.Column<double>(type: "float", nullable: true),
                    ApparentTemperatureC = table.Column<double>(type: "float", nullable: true),
                    RelativeHumidity = table.Column<double>(type: "float", nullable: true),
                    WindSpeedKph = table.Column<double>(type: "float", nullable: true),
                    WindDirectionDegrees = table.Column<double>(type: "float", nullable: true),
                    PrecipitationMm = table.Column<double>(type: "float", nullable: true),
                    VisibilityKm = table.Column<double>(type: "float", nullable: true),
                    PressureHpa = table.Column<double>(type: "float", nullable: true),
                    CloudCoverPercent = table.Column<double>(type: "float", nullable: true),
                    WeatherPhrase = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HasSevereWeatherAlert = table.Column<bool>(type: "bit", nullable: false),
                    SevereWeatherAlertTypesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AirQualityCategory = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AirQualityIndex = table.Column<double>(type: "float", nullable: true),
                    TrafficIncidentsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawResponseJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ContextHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentEnvironmentalContexts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentEnvironmentalContexts_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentalGridSnapshots_CellKey_ObservedAtUtc",
                table: "EnvironmentalGridSnapshots",
                columns: new[] { "CellKey", "ObservedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentalGridSnapshots_ContextHash",
                table: "EnvironmentalGridSnapshots",
                column: "ContextHash");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentEnvironmentalContexts_ContextHash",
                table: "IncidentEnvironmentalContexts",
                column: "ContextHash");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentEnvironmentalContexts_IncidentId_ObservedAtUtc",
                table: "IncidentEnvironmentalContexts",
                columns: new[] { "IncidentId", "ObservedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnvironmentalGridSnapshots");

            migrationBuilder.DropTable(
                name: "IncidentEnvironmentalContexts");
        }
    }
}
