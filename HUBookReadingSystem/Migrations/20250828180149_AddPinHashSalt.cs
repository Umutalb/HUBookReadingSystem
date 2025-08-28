using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HUBookReadingSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddPinHashSalt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Pin",
                table: "Readers");

            migrationBuilder.AddColumn<byte[]>(
                name: "PinHash",
                table: "Readers",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "PinSalt",
                table: "Readers",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PinHash",
                table: "Readers");

            migrationBuilder.DropColumn(
                name: "PinSalt",
                table: "Readers");

            migrationBuilder.AddColumn<string>(
                name: "Pin",
                table: "Readers",
                type: "character varying(6)",
                maxLength: 6,
                nullable: false,
                defaultValue: "");
        }
    }
}
