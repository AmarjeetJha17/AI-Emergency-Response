using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergencyResponse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentDuplication : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DuplicateDetectionCompletedAtUtc",
                table: "Incidents",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DuplicateDetectionFailureReason",
                table: "Incidents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DuplicateDetectionStatus",
                table: "Incidents",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DuplicateDetectionCompletedAtUtc",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "DuplicateDetectionFailureReason",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "DuplicateDetectionStatus",
                table: "Incidents");
        }
    }
}
