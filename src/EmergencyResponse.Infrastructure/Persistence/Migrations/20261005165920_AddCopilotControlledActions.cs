using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergencyResponse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCopilotControlledActions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CopilotActionProposals",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CopilotInteractionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ActionType = table.Column<int>(type: "int", nullable: false),
                    RiskLevel = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    ArgumentsJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SituationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    EscalationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProposedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    ProposedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovalDecision = table.Column<int>(type: "int", nullable: false),
                    ApprovedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    ApprovedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovalComment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ExecutedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExecutionResultJson = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PolicySnapshotJson = table.Column<string>(type: "nvarchar(max)", maxLength: 10000, nullable: true),
                    CorrelationId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopilotActionProposals", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IncidentNotes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentNotes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IncidentTags",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tag = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    AddedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentTags", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CopilotActionProposals_CopilotInteractionId_ProposedAtUtc",
                table: "CopilotActionProposals",
                columns: new[] { "CopilotInteractionId", "ProposedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CopilotActionProposals_CorrelationId",
                table: "CopilotActionProposals",
                column: "CorrelationId");

            migrationBuilder.CreateIndex(
                name: "IX_CopilotActionProposals_EscalationId",
                table: "CopilotActionProposals",
                column: "EscalationId");

            migrationBuilder.CreateIndex(
                name: "IX_CopilotActionProposals_IncidentId",
                table: "CopilotActionProposals",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_CopilotActionProposals_ResourceId",
                table: "CopilotActionProposals",
                column: "ResourceId");

            migrationBuilder.CreateIndex(
                name: "IX_CopilotActionProposals_SituationId",
                table: "CopilotActionProposals",
                column: "SituationId");

            migrationBuilder.CreateIndex(
                name: "IX_CopilotActionProposals_Status_ExpiresAtUtc",
                table: "CopilotActionProposals",
                columns: new[] { "Status", "ExpiresAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentNotes_IncidentId_CreatedAtUtc",
                table: "IncidentNotes",
                columns: new[] { "IncidentId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentTags_IncidentId_Tag",
                table: "IncidentTags",
                columns: new[] { "IncidentId", "Tag" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CopilotActionProposals");

            migrationBuilder.DropTable(
                name: "IncidentNotes");

            migrationBuilder.DropTable(
                name: "IncidentTags");
        }
    }
}
