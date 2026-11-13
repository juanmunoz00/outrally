using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Outrally.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMedicalProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MedicalProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EncryptedData = table.Column<byte[]>(type: "varbinary(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MedicalProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PotluckItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActivityId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Unit = table.Column<int>(type: "int", nullable: false),
                    QuantityNeeded = table.Column<int>(type: "int", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedByUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PotluckItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PotluckItems_Activities_ActivityId",
                        column: x => x.ActivityId,
                        principalTable: "Activities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PotluckClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PotluckItemId = table.Column<int>(type: "int", nullable: false),
                    ClaimantUserId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClaimantName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    QuantityClaimed = table.Column<int>(type: "int", nullable: false),
                    ClaimedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PotluckClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PotluckClaims_PotluckItems_PotluckItemId",
                        column: x => x.PotluckItemId,
                        principalTable: "PotluckItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PotluckClaims_PotluckItemId",
                table: "PotluckClaims",
                column: "PotluckItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PotluckItems_ActivityId",
                table: "PotluckItems",
                column: "ActivityId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MedicalProfiles");

            migrationBuilder.DropTable(
                name: "PotluckClaims");

            migrationBuilder.DropTable(
                name: "PotluckItems");
        }
    }
}
