using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergencyResponse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentDuplicateMatching : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "EmbeddingGeneratedAtUtc",
                table: "Incidents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EmbeddingModel",
                table: "Incidents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "IncidentDuplicateMatches",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SourceIncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CandidateIncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SemanticSimilarity = table.Column<double>(type: "float(5)", precision: 5, scale: 4, nullable: false),
                    GeographicSimilarity = table.Column<double>(type: "float(5)", precision: 5, scale: 4, nullable: false),
                    TemporalSimilarity = table.Column<double>(type: "float(5)", precision: 5, scale: 4, nullable: false),
                    IncidentTypeSimilarity = table.Column<double>(type: "float(5)", precision: 5, scale: 4, nullable: false),
                    DuplicateScore = table.Column<double>(type: "float(5)", precision: 5, scale: 4, nullable: false),
                    Reviewed = table.Column<bool>(type: "bit", nullable: false),
                    ConfirmedDuplicate = table.Column<bool>(type: "bit", nullable: false),
                    ReviewDecision = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    ReviewedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ReviewedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentDuplicateMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentDuplicateMatches_Incidents_CandidateIncidentId",
                        column: x => x.CandidateIncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IncidentDuplicateMatches_Incidents_SourceIncidentId",
                        column: x => x.SourceIncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentDuplicateMatches_CandidateIncidentId",
                table: "IncidentDuplicateMatches",
                column: "CandidateIncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentDuplicateMatches_DuplicateScore",
                table: "IncidentDuplicateMatches",
                column: "DuplicateScore");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentDuplicateMatches_SourceIncidentId_CandidateIncidentId",
                table: "IncidentDuplicateMatches",
                columns: new[] { "SourceIncidentId", "CandidateIncidentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IncidentDuplicateMatches");

            migrationBuilder.DropColumn(
                name: "EmbeddingGeneratedAtUtc",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "EmbeddingModel",
                table: "Incidents");
        }
    }
}
