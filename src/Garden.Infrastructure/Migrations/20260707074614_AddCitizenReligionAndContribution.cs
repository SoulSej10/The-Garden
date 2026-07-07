using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garden.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCitizenReligionAndContribution : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GovernmentType",
                table: "Settlements",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "KingdomId",
                table: "Settlements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "LeaderId",
                table: "Settlements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LeaderName",
                table: "Settlements",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<Guid>(
                name: "ReligionId",
                table: "Settlements",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReligionName",
                table: "Settlements",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "TechnologyProgress",
                table: "Settlements",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "ContributionScore",
                table: "Citizens",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<Guid>(
                name: "ReligionId",
                table: "Citizens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReligionName",
                table: "Citizens",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<double>(
                name: "Reputation",
                table: "Citizens",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "CulturalTrait",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Category = table.Column<string>(type: "text", nullable: false),
                    Strength = table.Column<double>(type: "double precision", nullable: false),
                    EstablishedTick = table.Column<long>(type: "bigint", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CulturalTrait", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CulturalTrait_Settlements_SettlementId",
                        column: x => x.SettlementId,
                        principalTable: "Settlements",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CulturalTrait_SettlementId",
                table: "CulturalTrait",
                column: "SettlementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CulturalTrait");

            migrationBuilder.DropColumn(
                name: "GovernmentType",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "KingdomId",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "LeaderId",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "LeaderName",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "ReligionId",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "ReligionName",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "TechnologyProgress",
                table: "Settlements");

            migrationBuilder.DropColumn(
                name: "ContributionScore",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "ReligionId",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "ReligionName",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "Reputation",
                table: "Citizens");
        }
    }
}
