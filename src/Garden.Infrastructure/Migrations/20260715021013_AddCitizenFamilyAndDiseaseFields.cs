using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garden.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCitizenFamilyAndDiseaseFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "DiseaseResistance",
                table: "Citizens",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<long>(
                name: "LastCareForFamilyDay",
                table: "Citizens",
                type: "bigint",
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentAId",
                table: "Citizens",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentBId",
                table: "Citizens",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DiseaseResistance",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "LastCareForFamilyDay",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "ParentAId",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "ParentBId",
                table: "Citizens");
        }
    }
}
