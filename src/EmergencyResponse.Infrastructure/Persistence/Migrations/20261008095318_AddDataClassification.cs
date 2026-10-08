using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergencyResponse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDataClassification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DataClassification",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "DataClassification",
                table: "KnowledgeDocuments",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<int>(
                name: "DataClassification",
                table: "Incidents",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "DataClassification",
                table: "IncidentMedia",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "DataClassification",
                table: "IncidentEnvironmentalContexts",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "DataClassification",
                table: "CopilotMessages",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "DataClassification",
                table: "CopilotInteractions",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "DataClassification",
                table: "CopilotConversations",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "DataClassification",
                table: "CopilotActionProposals",
                type: "int",
                nullable: false,
                defaultValue: 2);

            migrationBuilder.AddColumn<int>(
                name: "DataClassification",
                table: "AuditLogs",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "DataClassification",
                table: "AspNetUsers",
                type: "int",
                nullable: false,
                defaultValue: 3);

            migrationBuilder.AddColumn<int>(
                name: "DataClassification",
                table: "AIAnalyses",
                type: "int",
                nullable: false,
                defaultValue: 2);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DataClassification",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "DataClassification",
                table: "KnowledgeDocuments");

            migrationBuilder.DropColumn(
                name: "DataClassification",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "DataClassification",
                table: "IncidentMedia");

            migrationBuilder.DropColumn(
                name: "DataClassification",
                table: "IncidentEnvironmentalContexts");

            migrationBuilder.DropColumn(
                name: "DataClassification",
                table: "CopilotMessages");

            migrationBuilder.DropColumn(
                name: "DataClassification",
                table: "CopilotInteractions");

            migrationBuilder.DropColumn(
                name: "DataClassification",
                table: "CopilotConversations");

            migrationBuilder.DropColumn(
                name: "DataClassification",
                table: "CopilotActionProposals");

            migrationBuilder.DropColumn(
                name: "DataClassification",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "DataClassification",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DataClassification",
                table: "AIAnalyses");
        }
    }
}
