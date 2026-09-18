using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergencyResponse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVisualAnalysis : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "AIAnalysisCompletedAtUtc",
                table: "IncidentMedia",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "AIAnalysisRequested",
                table: "IncidentMedia",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ContentHash",
                table: "IncidentMedia",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "VisualAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncidentMediaId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ModelVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PromptVersion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    FindingsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RawOutputJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Confidence = table.Column<double>(type: "float", nullable: true),
                    ExecutionTimeMs = table.Column<long>(type: "bigint", nullable: true),
                    HumanReviewed = table.Column<bool>(type: "bit", nullable: false),
                    HumanDecision = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CompletedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VisualAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VisualAnalyses_IncidentMedia_IncidentMediaId",
                        column: x => x.IncidentMediaId,
                        principalTable: "IncidentMedia",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_VisualAnalyses_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_VisualAnalyses_IncidentId",
                table: "VisualAnalyses",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_VisualAnalyses_IncidentMediaId",
                table: "VisualAnalyses",
                column: "IncidentMediaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "VisualAnalyses");

            migrationBuilder.DropColumn(
                name: "AIAnalysisCompletedAtUtc",
                table: "IncidentMedia");

            migrationBuilder.DropColumn(
                name: "AIAnalysisRequested",
                table: "IncidentMedia");

            migrationBuilder.DropColumn(
                name: "ContentHash",
                table: "IncidentMedia");
        }
    }
}
