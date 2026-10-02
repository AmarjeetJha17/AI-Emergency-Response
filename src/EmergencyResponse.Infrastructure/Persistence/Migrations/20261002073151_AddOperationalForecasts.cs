using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergencyResponse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalForecasts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OperationalForecasts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ForecastType = table.Column<int>(type: "int", nullable: false),
                    Granularity = table.Column<int>(type: "int", nullable: false),
                    SeriesKey = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModelVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FeatureSchemaVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ForecastGeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ForecastStartAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ForecastEndAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Horizon = table.Column<int>(type: "int", nullable: false),
                    ForecastJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LowerBoundJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UpperBoundJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MAE = table.Column<double>(type: "float(18)", precision: 18, scale: 4, nullable: true),
                    RMSE = table.Column<double>(type: "float(18)", precision: 18, scale: 4, nullable: true),
                    HumanReviewed = table.Column<bool>(type: "bit", nullable: false),
                    HumanDecision = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationalForecasts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ForecastEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationalForecastId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EvaluationAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MAE = table.Column<double>(type: "float(18)", precision: 18, scale: 4, nullable: false),
                    RMSE = table.Column<double>(type: "float(18)", precision: 18, scale: 4, nullable: false),
                    DetailsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ForecastEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ForecastEvaluations_OperationalForecasts_OperationalForecastId",
                        column: x => x.OperationalForecastId,
                        principalTable: "OperationalForecasts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ForecastEvaluations_OperationalForecastId_EvaluationAtUtc",
                table: "ForecastEvaluations",
                columns: new[] { "OperationalForecastId", "EvaluationAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalForecasts_ForecastType_SeriesKey_ForecastGeneratedAtUtc",
                table: "OperationalForecasts",
                columns: new[] { "ForecastType", "SeriesKey", "ForecastGeneratedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalForecasts_ForecastType_SeriesKey_ForecastStartAtUtc",
                table: "OperationalForecasts",
                columns: new[] { "ForecastType", "SeriesKey", "ForecastStartAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ForecastEvaluations");

            migrationBuilder.DropTable(
                name: "OperationalForecasts");
        }
    }
}
