using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergencyResponse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIncidentConsolidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "CanonicalIncidentId",
                table: "Incidents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsCanonical",
                table: "Incidents",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateTable(
                name: "IncidentRelations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParentIncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelatedIncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RelationType = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IncidentRelations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IncidentRelations_Incidents_ParentIncidentId",
                        column: x => x.ParentIncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_IncidentRelations_Incidents_RelatedIncidentId",
                        column: x => x.RelatedIncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Incidents_CanonicalIncidentId",
                table: "Incidents",
                column: "CanonicalIncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_IncidentRelations_ParentIncidentId_RelatedIncidentId_RelationType",
                table: "IncidentRelations",
                columns: new[] { "ParentIncidentId", "RelatedIncidentId", "RelationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IncidentRelations_RelatedIncidentId",
                table: "IncidentRelations",
                column: "RelatedIncidentId");

            migrationBuilder.AddForeignKey(
                name: "FK_Incidents_Incidents_CanonicalIncidentId",
                table: "Incidents",
                column: "CanonicalIncidentId",
                principalTable: "Incidents",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Incidents_Incidents_CanonicalIncidentId",
                table: "Incidents");

            migrationBuilder.DropTable(
                name: "IncidentRelations");

            migrationBuilder.DropIndex(
                name: "IX_Incidents_CanonicalIncidentId",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "CanonicalIncidentId",
                table: "Incidents");

            migrationBuilder.DropColumn(
                name: "IsCanonical",
                table: "Incidents");
        }
    }
}
