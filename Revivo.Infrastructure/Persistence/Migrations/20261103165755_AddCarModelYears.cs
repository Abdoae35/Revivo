using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Revivo.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCarModelYears : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "YearEnd",
                table: "CarModels",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "YearStart",
                table: "CarModels",
                type: "int",
                nullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_CarModels_Years",
                table: "CarModels",
                sql: "([YearStart] IS NULL OR [YearStart] >= 1886) AND ([YearEnd] IS NULL OR [YearEnd] >= [YearStart])");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_CarModels_Years",
                table: "CarModels");

            migrationBuilder.DropColumn(
                name: "YearEnd",
                table: "CarModels");

            migrationBuilder.DropColumn(
                name: "YearStart",
                table: "CarModels");
        }
    }
}
