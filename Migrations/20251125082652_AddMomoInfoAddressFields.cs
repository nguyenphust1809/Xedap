using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Xedap.Migrations
{
    /// <inheritdoc />
    public partial class AddMomoInfoAddressFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "MomoInfos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "District",
                table: "MomoInfos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Note",
                table: "MomoInfos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Province",
                table: "MomoInfos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReceiverName",
                table: "MomoInfos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ReceiverPhone",
                table: "MomoInfos",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Ward",
                table: "MomoInfos",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "MomoInfos");

            migrationBuilder.DropColumn(
                name: "District",
                table: "MomoInfos");

            migrationBuilder.DropColumn(
                name: "Note",
                table: "MomoInfos");

            migrationBuilder.DropColumn(
                name: "Province",
                table: "MomoInfos");

            migrationBuilder.DropColumn(
                name: "ReceiverName",
                table: "MomoInfos");

            migrationBuilder.DropColumn(
                name: "ReceiverPhone",
                table: "MomoInfos");

            migrationBuilder.DropColumn(
                name: "Ward",
                table: "MomoInfos");
        }
    }
}
