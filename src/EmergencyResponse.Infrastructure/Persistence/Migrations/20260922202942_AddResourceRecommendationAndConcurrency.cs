using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergencyResponse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceRecommendationAndConcurrency : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Resources",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "IncidentTypeResourceRequirements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IncidentTypeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceType = table.Column<int>(type: "int", nullable: false),
                    MinimumCount = table.Column<int>(type: "int", nullable: false),
                    PreferredCount = table.Column<int>(type: "int", nullable: false),
                    IsMandatory = table.Column<bool>(type: "bit", nullable: false),
                    PriorityOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentTypeResourceRequirements", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentTypeResourceRequirements_IncidentTypes_IncidentTypeId",
                        column: x => x.IncidentTypeId,
                        principalTable: "IncidentTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IncidentTypeResourceRequirements_IncidentTypeId_ResourceType",
                table: "IncidentTypeResourceRequirements",
                columns: new[] { "IncidentTypeId", "ResourceType" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IncidentTypeResourceRequirements");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Resources");
        }
    }
}
