using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergencyResponse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEmergencyKnowledgeBase : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "KnowledgeDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    DocumentType = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Version = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ContentType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StorageKey = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    SizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ContentHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveFromUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DepartmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    IsGlobal = table.Column<bool>(type: "bit", nullable: false),
                    RequiresOperatorRole = table.Column<bool>(type: "bit", nullable: false),
                    AllowedRolesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AllowedDepartmentsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IndexedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IndexName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    FailureReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeDocuments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KnowledgeDocumentChunks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    KnowledgeDocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ChunkNumber = table.Column<int>(type: "int", nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PageNumber = table.Column<int>(type: "int", nullable: false),
                    SectionHeading = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ChunkHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    SearchDocumentId = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    TokenEstimate = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KnowledgeDocumentChunks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KnowledgeDocumentChunks_KnowledgeDocuments_KnowledgeDocumentId",
                        column: x => x.KnowledgeDocumentId,
                        principalTable: "KnowledgeDocuments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocumentChunks_ChunkHash",
                table: "KnowledgeDocumentChunks",
                column: "ChunkHash");

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocumentChunks_KnowledgeDocumentId_ChunkNumber",
                table: "KnowledgeDocumentChunks",
                columns: new[] { "KnowledgeDocumentId", "ChunkNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocumentChunks_SearchDocumentId",
                table: "KnowledgeDocumentChunks",
                column: "SearchDocumentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_ContentHash_Version",
                table: "KnowledgeDocuments",
                columns: new[] { "ContentHash", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_DocumentNumber",
                table: "KnowledgeDocuments",
                column: "DocumentNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_KnowledgeDocuments_Status_DepartmentId_IsGlobal",
                table: "KnowledgeDocuments",
                columns: new[] { "Status", "DepartmentId", "IsGlobal" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "KnowledgeDocumentChunks");

            migrationBuilder.DropTable(
                name: "KnowledgeDocuments");
        }
    }
}
