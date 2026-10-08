using EmergencyResponse.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergencyResponse.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261008120000_AddDurableJobOutbox")]
public sealed class AddDurableJobOutbox : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "OutboxMessages",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MessageType = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                PayloadJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                MessageKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                PublishedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                AttemptCount = table.Column<int>(type: "int", nullable: false),
                LastAttemptAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                LastError = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                IsPublished = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            }, constraints: table => table.PrimaryKey("PK_OutboxMessages", x => x.Id));

        migrationBuilder.CreateTable(
            name: "BackgroundJobExecutions",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                JobId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                JobType = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                AnalysisVersion = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                WorkItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                AttemptCount = table.Column<int>(type: "int", nullable: false),
                LeaseExpiresAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                CompletedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                LastFailureCategory = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            }, constraints: table => table.PrimaryKey("PK_BackgroundJobExecutions", x => x.Id));

        migrationBuilder.CreateIndex("IX_OutboxMessages_MessageKey", "OutboxMessages", "MessageKey", unique: true);
        migrationBuilder.CreateIndex("IX_OutboxMessages_IsPublished_OccurredAtUtc", "OutboxMessages", new[] { "IsPublished", "OccurredAtUtc" });
        migrationBuilder.CreateIndex("IX_BackgroundJobExecutions_JobId_AnalysisVersion", "BackgroundJobExecutions", new[] { "JobId", "AnalysisVersion" }, unique: true);
        migrationBuilder.CreateIndex("IX_BackgroundJobExecutions_Status_LeaseExpiresAtUtc", "BackgroundJobExecutions", new[] { "Status", "LeaseExpiresAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "BackgroundJobExecutions");
        migrationBuilder.DropTable(name: "OutboxMessages");
    }
}
