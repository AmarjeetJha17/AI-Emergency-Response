using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergencyResponse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOperationalNotifications : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Notifications",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450);

            migrationBuilder.AlterColumn<int>(
                name: "Type",
                table: "Notifications",
                type: "int",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100);

            migrationBuilder.AddColumn<string>(
                name: "DeduplicationKey",
                table: "Notifications",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAtUtc",
                table: "Notifications",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IncidentEscalationId",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "IncidentId",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MetadataJson",
                table: "Notifications",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "OperationalSituationId",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Notifications",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<Guid>(
                name: "TargetDepartmentId",
                table: "Notifications",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TargetRole",
                table: "Notifications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_DeduplicationKey_UserId_Status",
                table: "Notifications",
                columns: new[] { "DeduplicationKey", "UserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IncidentEscalationId",
                table: "Notifications",
                column: "IncidentEscalationId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_IncidentId",
                table: "Notifications",
                column: "IncidentId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_OperationalSituationId",
                table: "Notifications",
                column: "OperationalSituationId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_TargetRole_Status_CreatedAtUtc",
                table: "Notifications",
                columns: new[] { "TargetRole", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_Status_CreatedAtUtc",
                table: "Notifications",
                columns: new[] { "UserId", "Status", "CreatedAtUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_IncidentEscalations_IncidentEscalationId",
                table: "Notifications",
                column: "IncidentEscalationId",
                principalTable: "IncidentEscalations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Incidents_IncidentId",
                table: "Notifications",
                column: "IncidentId",
                principalTable: "Incidents",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_OperationalSituations_OperationalSituationId",
                table: "Notifications",
                column: "OperationalSituationId",
                principalTable: "OperationalSituations",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_IncidentEscalations_IncidentEscalationId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Incidents_IncidentId",
                table: "Notifications");

            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_OperationalSituations_OperationalSituationId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_DeduplicationKey_UserId_Status",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_IncidentEscalationId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_IncidentId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_OperationalSituationId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_TargetRole_Status_CreatedAtUtc",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_UserId_Status_CreatedAtUtc",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "DeduplicationKey",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "ExpiresAtUtc",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "IncidentEscalationId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "IncidentId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "MetadataJson",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "OperationalSituationId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "TargetDepartmentId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "TargetRole",
                table: "Notifications");

            migrationBuilder.AlterColumn<string>(
                name: "UserId",
                table: "Notifications",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(450)",
                oldMaxLength: 450,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Type",
                table: "Notifications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_UserId_IsRead",
                table: "Notifications",
                columns: new[] { "UserId", "IsRead" });
        }
    }
}
