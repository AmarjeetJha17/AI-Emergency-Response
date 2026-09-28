using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergencyResponse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddReliabilityAndAIExecutionMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AttemptNumber",
                table: "VisualAnalyses",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "FailureCategory",
                table: "VisualAnalyses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAttemptAtUtc",
                table: "VisualAnalyses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAtUtc",
                table: "VisualAnalyses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "VisualAnalyses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AttemptNumber",
                table: "AIAnalyses",
                type: "int",
                nullable: false,
                defaultValue: 1);

            migrationBuilder.AddColumn<string>(
                name: "FailureCategory",
                table: "AIAnalyses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InputTokens",
                table: "AIAnalyses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastAttemptAtUtc",
                table: "AIAnalyses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextRetryAtUtc",
                table: "AIAnalyses",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OutputTokens",
                table: "AIAnalyses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RetryCount",
                table: "AIAnalyses",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "TotalTokens",
                table: "AIAnalyses",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttemptNumber",
                table: "VisualAnalyses");

            migrationBuilder.DropColumn(
                name: "FailureCategory",
                table: "VisualAnalyses");

            migrationBuilder.DropColumn(
                name: "LastAttemptAtUtc",
                table: "VisualAnalyses");

            migrationBuilder.DropColumn(
                name: "NextRetryAtUtc",
                table: "VisualAnalyses");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "VisualAnalyses");

            migrationBuilder.DropColumn(
                name: "AttemptNumber",
                table: "AIAnalyses");

            migrationBuilder.DropColumn(
                name: "FailureCategory",
                table: "AIAnalyses");

            migrationBuilder.DropColumn(
                name: "InputTokens",
                table: "AIAnalyses");

            migrationBuilder.DropColumn(
                name: "LastAttemptAtUtc",
                table: "AIAnalyses");

            migrationBuilder.DropColumn(
                name: "NextRetryAtUtc",
                table: "AIAnalyses");

            migrationBuilder.DropColumn(
                name: "OutputTokens",
                table: "AIAnalyses");

            migrationBuilder.DropColumn(
                name: "RetryCount",
                table: "AIAnalyses");

            migrationBuilder.DropColumn(
                name: "TotalTokens",
                table: "AIAnalyses");
        }
    }
}
