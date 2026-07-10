using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Garden.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCitizenEmotions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Emotions_Curiosity",
                table: "Citizens",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Emotions_Fear",
                table: "Citizens",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Emotions_Joy",
                table: "Citizens",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Emotions_Loneliness",
                table: "Citizens",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Emotions_Sadness",
                table: "Citizens",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "Emotions_Trust",
                table: "Citizens",
                type: "double precision",
                nullable: false,
                defaultValue: 0.0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Emotions_Curiosity",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "Emotions_Fear",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "Emotions_Joy",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "Emotions_Loneliness",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "Emotions_Sadness",
                table: "Citizens");

            migrationBuilder.DropColumn(
                name: "Emotions_Trust",
                table: "Citizens");
        }
    }
}
