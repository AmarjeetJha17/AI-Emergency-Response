using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergencyResponse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddResourceMapLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LocationUpdatedAtUtc",
                table: "Resources",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ShowOnOperationalMap",
                table: "Resources",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "StorageKey",
                table: "IncidentMedia",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "ContentHash",
                table: "IncidentMedia",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Resources_Latitude_Longitude",
                table: "Resources",
                columns: new[] { "Latitude", "Longitude" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Resources_Latitude_Longitude",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "LocationUpdatedAtUtc",
                table: "Resources");

            migrationBuilder.DropColumn(
                name: "ShowOnOperationalMap",
                table: "Resources");

            migrationBuilder.AlterColumn<string>(
                name: "StorageKey",
                table: "IncidentMedia",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AlterColumn<string>(
                name: "ContentHash",
                table: "IncidentMedia",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);
        }
    }
}
