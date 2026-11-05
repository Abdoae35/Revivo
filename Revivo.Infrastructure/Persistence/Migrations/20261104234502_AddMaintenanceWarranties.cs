using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Revivo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMaintenanceWarranties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MaintenanceWarranties",
                columns: table => new
                {
                    MaintenanceRecordId = table.Column<int>(type: "int", nullable: false),
                    DurationMonths = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    WarningDays = table.Column<int>(type: "int", nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Brand = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MaintenanceWarranties", x => x.MaintenanceRecordId);
                    table.CheckConstraint("CK_MaintenanceWarranties_DurationMonths", "[DurationMonths] BETWEEN 1 AND 1200");
                    table.CheckConstraint("CK_MaintenanceWarranties_StartDate", "[StartDate] >= '1886-01-01' AND [StartDate] <= CONVERT(date, GETUTCDATE())");
                    table.CheckConstraint("CK_MaintenanceWarranties_WarningDays", "[WarningDays] >= 0");
                    table.ForeignKey(
                        name: "FK_MaintenanceWarranties_MaintenanceRecords_MaintenanceRecordId",
                        column: x => x.MaintenanceRecordId,
                        principalTable: "MaintenanceRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MaintenanceWarranties");
        }
    }
}
