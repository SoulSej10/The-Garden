using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garden.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSettlementSoilHealthAndWildlife : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "SoilHealth",
                table: "Settlements",
                type: "double precision",
                nullable: false,
                defaultValue: 100.0);

            migrationBuilder.AddColumn<double>(
                name: "WildlifePopulation",
                table: "Settlements",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SoilHealth",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "WildlifePopulation",
                table: "Settlements");
        }
    }
}
