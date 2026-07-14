using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xedap.Migrations
{
    /// <inheritdoc />
    public partial class Newinf4model : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "MomoInfos",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "MomoInfos",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "MomoInfos");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "MomoInfos");
        }
    }
}
