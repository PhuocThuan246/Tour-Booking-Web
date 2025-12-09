using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TourBookingWeb.Migrations
{
    /// <inheritdoc />
    public partial class DeleteCagtegoryNavigation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tours_Categories_CategoryId",
                table: "Tours");

            migrationBuilder.AddForeignKey(
                name: "FK_Tours_Categories_CategoryId",
                table: "Tours",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tours_Categories_CategoryId",
                table: "Tours");

            migrationBuilder.AddForeignKey(
                name: "FK_Tours_Categories_CategoryId",
                table: "Tours",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "CategoryId",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
