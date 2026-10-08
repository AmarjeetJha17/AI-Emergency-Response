using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergencyResponse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddKnowledgeSecurityScan : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KnowledgeDocuments_Status_DepartmentId_IsGlobal",
                table: "KnowledgeDocuments");

            migrationBuilder.AddColumn<string>(
                name: "SecurityRiskReason",
                table: "KnowledgeDocuments",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "SecurityScanCompletedAtUtc",
                table: "KnowledgeDocuments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SecurityScanStatus",
                table: "KnowledgeDocuments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SecuritySignalsJson",
                table: "KnowledgeDocuments",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_Status_SecurityScanStatus_DepartmentId_IsGlobal",
                table: "KnowledgeDocuments",
                columns: new[] { "Status", "SecurityScanStatus", "DepartmentId", "IsGlobal" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_KnowledgeDocuments_Status_SecurityScanStatus_DepartmentId_IsGlobal",
                table: "KnowledgeDocuments");

            migrationBuilder.DropColumn(
                name: "SecurityRiskReason",
                table: "KnowledgeDocuments");

            migrationBuilder.DropColumn(
                name: "SecurityScanCompletedAtUtc",
                table: "KnowledgeDocuments");

            migrationBuilder.DropColumn(
                name: "SecurityScanStatus",
                table: "KnowledgeDocuments");

            migrationBuilder.DropColumn(
                name: "SecuritySignalsJson",
                table: "KnowledgeDocuments");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_Status_DepartmentId_IsGlobal",
                table: "KnowledgeDocuments",
                columns: new[] { "Status", "DepartmentId", "IsGlobal" });
        }
    }
}
