using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergencyResponse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPredictionTrainingSnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DatasetVersion",
                table: "PredictiveAnalyses",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FeatureSchemaVersion",
                table: "PredictiveAnalyses",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "IncidentPredictionSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PredictionType = table.Column<int>(type: "int", nullable: false),
                    SnapshotAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FeatureSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Label = table.Column<bool>(type: "bit", nullable: true),
                    LabelEvaluationAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    OutcomeAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OutcomeEscalationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    FeatureHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentPredictionSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentPredictionSnapshots_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentPredictionSnapshots_FeatureHash",
                table: "IncidentPredictionSnapshots",
                column: "FeatureHash");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentPredictionSnapshots_IncidentId_PredictionType_SnapshotAtUtc",
                table: "IncidentPredictionSnapshots",
                columns: new[] { "IncidentId", "PredictionType", "SnapshotAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IncidentPredictionSnapshots");

            migrationBuilder.DropColumn(
                name: "DatasetVersion",
                table: "PredictiveAnalyses");

            migrationBuilder.DropColumn(
                name: "FeatureSchemaVersion",
                table: "PredictiveAnalyses");
        }
    }
}
