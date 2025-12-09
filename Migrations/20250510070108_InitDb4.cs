using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourBookingWeb.Migrations
{
    /// <inheritdoc />
    public partial class InitDb4 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Id",
                table: "TourSchedules",
                newName: "TourScheduleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "TourScheduleId",
                table: "TourSchedules",
                newName: "Id");
        }
    }
}
