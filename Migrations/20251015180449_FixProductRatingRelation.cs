using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xedap.Migrations
{
    /// <inheritdoc />
    public partial class FixProductRatingRelation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Xóa chỉ mục cũ (nếu có unique constraint)
            migrationBuilder.DropIndex(
                name: "IX_Ratings_ProductId",
                table: "Ratings");

            // Xóa cột Star cũ kiểu string
            migrationBuilder.DropColumn(
                name: "Star",
                table: "Ratings");

            // Tạo lại cột Star kiểu int
            migrationBuilder.AddColumn<int>(
                name: "Star",
                table: "Ratings",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            // Tạo lại index cho ProductId (không unique)
            migrationBuilder.CreateIndex(
                name: "IX_Ratings_ProductId",
                table: "Ratings",
                column: "ProductId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Xóa index mới
            migrationBuilder.DropIndex(
                name: "IX_Ratings_ProductId",
                table: "Ratings");

            // Xóa cột Star kiểu int
            migrationBuilder.DropColumn(
                name: "Star",
                table: "Ratings");

            // Tạo lại cột Star kiểu string
            migrationBuilder.AddColumn<string>(
                name: "Star",
                table: "Ratings",
                type: "text",
                nullable: false,
                defaultValue: "");

            // Tạo lại index cũ (unique)
            migrationBuilder.CreateIndex(
                name: "IX_Ratings_ProductId",
                table: "Ratings",
                column: "ProductId",
                unique: true);
        }
    }
}
