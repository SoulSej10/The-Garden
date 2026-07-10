using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garden.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSettlementGovernanceFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AuthoritySource",
                table: "Settlements",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<long>(
                name: "LastGovernmentChangeTick",
                table: "Settlements",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<double>(
                name: "Legitimacy",
                table: "Settlements",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AuthoritySource",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "LastGovernmentChangeTick",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "Legitimacy",
                table: "Settlements");
        }
    }
}
