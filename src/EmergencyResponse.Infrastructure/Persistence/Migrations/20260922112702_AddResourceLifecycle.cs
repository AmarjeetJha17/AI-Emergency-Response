using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergencyResponse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceLifecycle : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ResourceAssignments_ResourceId",
                table: "ResourceAssignments");

            migrationBuilder.AddColumn<string>(
                name: "AssignedByUserId",
                table: "ResourceAssignments",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DispatchedAtUtc",
                table: "ResourceAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EnRouteAtUtc",
                table: "ResourceAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "ResourceAssignments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "OnSceneAtUtc",
                table: "ResourceAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReleaseNotes",
                table: "ResourceAssignments",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ReturningAtUtc",
                table: "ResourceAssignments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "ResourceAssignments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "ResourceStatusHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ResourceId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PreviousStatus = table.Column<int>(type: "int", nullable: false),
                    NewStatus = table.Column<int>(type: "int", nullable: false),
                    IncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ChangedByUserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ResourceStatusHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ResourceStatusHistories_Incidents_IncidentId",
                        column: x => x.IncidentId,
                        principalTable: "Incidents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ResourceStatusHistories_Resources_ResourceId",
                        column: x => x.ResourceId,
                        principalTable: "Resources",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ResourceAssignments_ResourceId",
                table: "ResourceAssignments",
                column: "ResourceId",
                unique: true,
                filter: "IsActive = 1");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceStatusHistories_IncidentId",
                table: "ResourceStatusHistories",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceStatusHistories_ResourceId_ChangedAtUtc",
                table: "ResourceStatusHistories",
                columns: new[] { "ResourceId", "ChangedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ResourceStatusHistories");

            migrationBuilder.DropIndex(
                name: "IX_ResourceAssignments_ResourceId",
                table: "ResourceAssignments");

            migrationBuilder.DropColumn(
                name: "AssignedByUserId",
                table: "ResourceAssignments");

            migrationBuilder.DropColumn(
                name: "DispatchedAtUtc",
                table: "ResourceAssignments");

            migrationBuilder.DropColumn(
                name: "EnRouteAtUtc",
                table: "ResourceAssignments");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "ResourceAssignments");

            migrationBuilder.DropColumn(
                name: "OnSceneAtUtc",
                table: "ResourceAssignments");

            migrationBuilder.DropColumn(
                name: "ReleaseNotes",
                table: "ResourceAssignments");

            migrationBuilder.DropColumn(
                name: "ReturningAtUtc",
                table: "ResourceAssignments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "ResourceAssignments");

            migrationBuilder.CreateIndex(
                name: "IX_ResourceAssignments_ResourceId",
                table: "ResourceAssignments",
                column: "ResourceId");
        }
    }
}
