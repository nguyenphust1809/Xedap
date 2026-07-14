using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

namespace Xedap.Migrations
{
    public partial class AddLocationToStores : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Point>(
                name: "Location",
                table: "Stores",
                type: "geometry (Point,4326)",
                nullable: true);

            // ✅ Cập nhật dữ liệu Location từ Latitude và Longitude
            migrationBuilder.Sql(@"
                UPDATE ""Stores""
                SET ""Location"" = ST_SetSRID(ST_MakePoint(""Longitude"", ""Latitude""), 4326)
                WHERE ""Latitude"" IS NOT NULL AND ""Longitude"" IS NOT NULL;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Location",
                table: "Stores");
        }
    }
}
