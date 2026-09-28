using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergencyResponse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalSituations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OperationalSituations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SituationNumber = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    Level = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    SituationScore = table.Column<double>(type: "float(5)", precision: 5, scale: 2, nullable: true),
                    IncidentCount = table.Column<int>(type: "int", nullable: false),
                    CriticalIncidentCount = table.Column<int>(type: "int", nullable: false),
                    HighIncidentCount = table.Column<int>(type: "int", nullable: false),
                    PeopleAffected = table.Column<int>(type: "int", nullable: false),
                    Casualties = table.Column<int>(type: "int", nullable: false),
                    ResourceShortageDetected = table.Column<bool>(type: "bit", nullable: false),
                    RequiresHumanReview = table.Column<bool>(type: "bit", nullable: false),
                    LastEvaluatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastAIAnalysisAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationalSituations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OperationalSituationAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationalSituationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PromptVersion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InputSnapshotJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    OutputJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    GeneratedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    HumanReviewed = table.Column<bool>(type: "bit", nullable: false),
                    HumanDecision = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationalSituationAnalyses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperationalSituationAnalyses_OperationalSituations_OperationalSituationId",
                        column: x => x.OperationalSituationId,
                        principalTable: "OperationalSituations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OperationalSituationIncidents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OperationalSituationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsPrimaryIncident = table.Column<bool>(type: "bit", nullable: false),
                    AddedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OperationalSituationIncidents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OperationalSituationIncidents_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OperationalSituationIncidents_OperationalSituations_OperationalSituationId",
                        column: x => x.OperationalSituationId,
                        principalTable: "OperationalSituations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalSituationAnalyses_OperationalSituationId_GeneratedAtUtc",
                table: "OperationalSituationAnalyses",
                columns: new[] { "OperationalSituationId", "GeneratedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_OperationalSituationIncidents_IncidentId",
                table: "OperationalSituationIncidents",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_OperationalSituationIncidents_OperationalSituationId_IncidentId",
                table: "OperationalSituationIncidents",
                columns: new[] { "OperationalSituationId", "IncidentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperationalSituations_SituationNumber",
                table: "OperationalSituations",
                column: "SituationNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_OperationalSituations_Status_Level_CreatedAtUtc",
                table: "OperationalSituations",
                columns: new[] { "Status", "Level", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OperationalSituationAnalyses");

            migrationBuilder.DropTable(
                name: "OperationalSituationIncidents");

            migrationBuilder.DropTable(
                name: "OperationalSituations");
        }
    }
}
