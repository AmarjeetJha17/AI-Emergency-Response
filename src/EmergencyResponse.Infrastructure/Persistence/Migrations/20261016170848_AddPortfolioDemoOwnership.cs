using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EmergencyResponse.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPortfolioDemoOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PortfolioDemoOwnershipKey",
                table: "OperationalSituations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PortfolioDemoOwnershipKey",
                table: "OperationalSituationAnalyses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PortfolioDemoOwnershipKey",
                table: "OperationalSituations");

            migrationBuilder.DropColumn(
                name: "PortfolioDemoOwnershipKey",
                table: "OperationalSituationAnalyses");
        }
    }
}
