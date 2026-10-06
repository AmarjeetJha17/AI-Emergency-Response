using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergencyResponse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCopilotMemoryAndFeedback : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CopilotConversations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    IncidentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SituationId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    LastActivityAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ArchivedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Summary = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopilotConversations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CopilotMessages",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CopilotConversationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Role = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ToolCallsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CitationsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GroundingStatus = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    SequenceNumber = table.Column<int>(type: "int", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopilotMessages", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CopilotMessages_CopilotConversations_CopilotConversationId",
                        column: x => x.CopilotConversationId,
                        principalTable: "CopilotConversations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CopilotFeedback",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CopilotMessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Rating = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CopilotFeedback", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CopilotFeedback_CopilotMessages_CopilotMessageId",
                        column: x => x.CopilotMessageId,
                        principalTable: "CopilotMessages",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CopilotConversations_IncidentId_LastActivityAtUtc",
                table: "CopilotConversations",
                columns: new[] { "IncidentId", "LastActivityAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CopilotConversations_SituationId_LastActivityAtUtc",
                table: "CopilotConversations",
                columns: new[] { "SituationId", "LastActivityAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CopilotConversations_UserId_LastActivityAtUtc",
                table: "CopilotConversations",
                columns: new[] { "UserId", "LastActivityAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_CopilotFeedback_CopilotMessageId",
                table: "CopilotFeedback",
                column: "CopilotMessageId");

            migrationBuilder.CreateIndex(
                name: "IX_CopilotMessages_CopilotConversationId_SequenceNumber",
                table: "CopilotMessages",
                columns: new[] { "CopilotConversationId", "SequenceNumber" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CopilotFeedback");

            migrationBuilder.DropTable(
                name: "CopilotMessages");

            migrationBuilder.DropTable(
                name: "CopilotConversations");
        }
    }
}
