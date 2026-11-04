using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Revivo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPeriodicMaintenanceSchedules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MaintenanceSchedules",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    VehicleId = table.Column<int>(type: "int", nullable: false),
                    MaintenanceItemId = table.Column<int>(type: "int", nullable: false),
                    IntervalKm = table.Column<int>(type: "int", nullable: true),
                    IntervalMonths = table.Column<int>(type: "int", nullable: true),
                    WarningKm = table.Column<int>(type: "int", nullable: false),
                    WarningDays = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceSchedules", x => x.Id);
                    table.CheckConstraint("CK_MaintenanceSchedules_IntervalKm", "[IntervalKm] IS NULL OR [IntervalKm] > 0");
                    table.CheckConstraint("CK_MaintenanceSchedules_IntervalMonths", "[IntervalMonths] IS NULL OR [IntervalMonths] BETWEEN 1 AND 1200");
                    table.CheckConstraint("CK_MaintenanceSchedules_WarningDays", "[WarningDays] >= 0");
                    table.CheckConstraint("CK_MaintenanceSchedules_WarningKm", "[WarningKm] >= 0");
                    table.ForeignKey(
                        name: "FK_MaintenanceSchedules_MaintenanceItems_MaintenanceItemId",
                        column: x => x.MaintenanceItemId,
                        principalTable: "MaintenanceItems",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MaintenanceSchedules_Vehicles_VehicleId",
                        column: x => x.VehicleId,
                        principalTable: "Vehicles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSchedules_MaintenanceItemId",
                table: "MaintenanceSchedules",
                column: "MaintenanceItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MaintenanceSchedules_VehicleId_MaintenanceItemId",
                table: "MaintenanceSchedules",
                columns: new[] { "VehicleId", "MaintenanceItemId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaintenanceSchedules");
        }
    }
}
