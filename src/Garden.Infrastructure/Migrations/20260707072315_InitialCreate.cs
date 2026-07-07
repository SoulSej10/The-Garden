using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Garden.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Citizens",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Age = table.Column<int>(type: "integer", nullable: false),
                    BiologicalSex = table.Column<string>(type: "text", nullable: false),
                    Stage = table.Column<int>(type: "integer", nullable: false),
                    TileX = table.Column<int>(type: "integer", nullable: false),
                    TileY = table.Column<int>(type: "integer", nullable: false),
                    HomeSettlementId = table.Column<Guid>(type: "uuid", nullable: true),
                    CurrentActivity = table.Column<string>(type: "text", nullable: false),
                    CurrentGoal = table.Column<string>(type: "text", nullable: false),
                    IsAlive = table.Column<bool>(type: "boolean", nullable: false),
                    BirthTick = table.Column<long>(type: "bigint", nullable: false),
                    DeathTick = table.Column<long>(type: "bigint", nullable: true),
                    CauseOfDeath = table.Column<string>(type: "text", nullable: false),
                    Attributes_Strength = table.Column<double>(type: "double precision", nullable: false),
                    Attributes_Endurance = table.Column<double>(type: "double precision", nullable: false),
                    Attributes_Intelligence = table.Column<double>(type: "double precision", nullable: false),
                    Attributes_Dexterity = table.Column<double>(type: "double precision", nullable: false),
                    Attributes_Perception = table.Column<double>(type: "double precision", nullable: false),
                    Personality_Curiosity = table.Column<double>(type: "double precision", nullable: false),
                    Personality_Patience = table.Column<double>(type: "double precision", nullable: false),
                    Personality_Aggression = table.Column<double>(type: "double precision", nullable: false),
                    Personality_Compassion = table.Column<double>(type: "double precision", nullable: false),
                    Personality_Diligence = table.Column<double>(type: "double precision", nullable: false),
                    Personality_Introversion = table.Column<double>(type: "double precision", nullable: false),
                    Needs_Hunger = table.Column<double>(type: "double precision", nullable: false),
                    Needs_Thirst = table.Column<double>(type: "double precision", nullable: false),
                    Needs_Energy = table.Column<double>(type: "double precision", nullable: false),
                    Needs_Warmth = table.Column<double>(type: "double precision", nullable: false),
                    Needs_Health = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Citizens", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Settlements",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Population = table.Column<int>(type: "integer", nullable: false),
                    TileX = table.Column<int>(type: "integer", nullable: false),
                    TileY = table.Column<int>(type: "integer", nullable: false),
                    TerritoryRadius = table.Column<int>(type: "integer", nullable: false),
                    Storage_MaxCapacity = table.Column<double>(type: "double precision", nullable: false),
                    MemberIds = table.Column<List<Guid>>(type: "uuid[]", nullable: false),
                    FoundedTick = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settlements", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CitizenMemory",
                columns: table => new
                {
                    CitizenId = table.Column<Guid>(type: "uuid", nullable: false),
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Tick = table.Column<long>(type: "bigint", nullable: false),
                    EventType = table.Column<string>(type: "text", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CitizenMemory", x => new { x.CitizenId, x.Id });
                    table.ForeignKey(
                        name: "FK_CitizenMemory_Citizens_CitizenId",
                        column: x => x.CitizenId,
                        principalTable: "Citizens",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Building",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BuildingType = table.Column<string>(type: "text", nullable: false),
                    TileX = table.Column<int>(type: "integer", nullable: false),
                    TileY = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    BuildProgress = table.Column<int>(type: "integer", nullable: false),
                    BuildTimeRequired = table.Column<int>(type: "integer", nullable: false),
                    AssignedWorkerId = table.Column<Guid>(type: "uuid", nullable: true),
                    Storage_MaxCapacity = table.Column<double>(type: "double precision", nullable: false),
                    SettlementId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Building", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Building_Settlements_SettlementId",
                        column: x => x.SettlementId,
                        principalTable: "Settlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Settlements_Items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InventorySettlementId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemType = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<double>(type: "double precision", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false),
                    StackLimit = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settlements_Items", x => new { x.InventorySettlementId, x.Id });
                    table.ForeignKey(
                        name: "FK_Settlements_Items_Settlements_InventorySettlementId",
                        column: x => x.InventorySettlementId,
                        principalTable: "Settlements",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Building_Items",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InventoryBuildingId = table.Column<Guid>(type: "uuid", nullable: false),
                    ItemType = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<double>(type: "double precision", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false),
                    StackLimit = table.Column<double>(type: "double precision", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Building_Items", x => new { x.InventoryBuildingId, x.Id });
                    table.ForeignKey(
                        name: "FK_Building_Items_Building_InventoryBuildingId",
                        column: x => x.InventoryBuildingId,
                        principalTable: "Building",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Building_SettlementId",
                table: "Building",
                column: "SettlementId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Building_Items");

            migrationBuilder.DropTable(
                name: "CitizenMemory");

            migrationBuilder.DropTable(
                name: "Settlements_Items");

            migrationBuilder.DropTable(
                name: "Building");

            migrationBuilder.DropTable(
                name: "Citizens");

            migrationBuilder.DropTable(
                name: "Settlements");
        }
    }
}
