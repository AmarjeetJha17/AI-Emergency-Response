using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergencyResponse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPredictiveIntelligence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PredictiveAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PredictionType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModelVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    FeatureSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PredictionJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Probability = table.Column<double>(type: "float(5)", precision: 5, scale: 4, nullable: true),
                    RiskScore = table.Column<double>(type: "float(5)", precision: 5, scale: 2, nullable: true),
                    PredictedEscalation = table.Column<bool>(type: "bit", nullable: true),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ValidUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    HumanReviewed = table.Column<bool>(type: "bit", nullable: false),
                    HumanDecision = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PredictiveAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PredictiveAnalyses_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PredictiveAnalyses_IncidentId_PredictionType_GeneratedAtUtc",
                table: "PredictiveAnalyses",
                columns: new[] { "IncidentId", "PredictionType", "GeneratedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PredictiveAnalyses");
        }
    }
}
