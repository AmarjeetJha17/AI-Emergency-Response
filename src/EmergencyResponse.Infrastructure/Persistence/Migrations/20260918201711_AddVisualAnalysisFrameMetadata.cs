using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergencyResponse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddVisualAnalysisFrameMetadata : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FrameTimestampsJson",
                table: "VisualAnalyses",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FramesAnalyzed",
                table: "VisualAnalyses",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "VideoDurationSeconds",
                table: "VisualAnalyses",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "MediaType",
                table: "IncidentMedia",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "SizeBytes",
                table: "IncidentMedia",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<string>(
                name: "StorageKey",
                table: "IncidentMedia",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FrameTimestampsJson",
                table: "VisualAnalyses");

            migrationBuilder.DropColumn(
                name: "FramesAnalyzed",
                table: "VisualAnalyses");

            migrationBuilder.DropColumn(
                name: "VideoDurationSeconds",
                table: "VisualAnalyses");

            migrationBuilder.DropColumn(
                name: "MediaType",
                table: "IncidentMedia");

            migrationBuilder.DropColumn(
                name: "SizeBytes",
                table: "IncidentMedia");

            migrationBuilder.DropColumn(
                name: "StorageKey",
                table: "IncidentMedia");
        }
    }
}
