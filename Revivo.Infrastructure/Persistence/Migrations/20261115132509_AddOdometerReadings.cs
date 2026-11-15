using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Revivo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOdometerReadings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OdometerReadings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    MileageKm = table.Column<int>(type: "int", nullable: false),
                    RecordedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RecordedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OdometerReadings", x => x.Id);
                    table.CheckConstraint("CK_OdometerReadings_MileageKm", "[MileageKm] >= 0");
                    table.ForeignKey(
                        name: "FK_OdometerReadings_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OdometerReadings_VehicleId_RecordedAt_Id",
                table: "OdometerReadings",
                columns: new[] { "VehicleId", "RecordedAt", "Id" },
                descending: new[] { false, true, true });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OdometerReadings");
        }
    }
}
