using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourBookingWeb.Migrations
{
    /// <inheritdoc />
    public partial class DeleteForgeinKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_Categories_CategoryId",
                table: "Bookings");

            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_TourSchedules_TourScheduleId",
                table: "Bookings");

            migrationBuilder.DropIndex(
                name: "IX_Bookings_CategoryId",
                table: "Bookings");

            migrationBuilder.DropColumn(
                name: "Departures",
                table: "Tours");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Bookings");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_TourSchedules_TourScheduleId",
                table: "Bookings",
                column: "TourScheduleId",
                principalTable: "TourSchedules",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Bookings_TourSchedules_TourScheduleId",
                table: "Bookings");

            migrationBuilder.AddColumn<string>(
                name: "Departures",
                table: "Tours",
                type: "nvarchar(max)",
                maxLength: 5000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CategoryId",
                table: "Bookings",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_CategoryId",
                table: "Bookings",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_Categories_CategoryId",
                table: "Bookings",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Bookings_TourSchedules_TourScheduleId",
                table: "Bookings",
                column: "TourScheduleId",
                principalTable: "TourSchedules",
                principalColumn: "Id");
        }
    }
}
