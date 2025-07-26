using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigortamat.Migrations
{
    /// <inheritdoc />
    public partial class AddQueueNotificationFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CarNumber",
                table: "Queues",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Message",
                table: "Queues",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "Queues",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RefId",
                table: "Queues",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CarNumber",
                table: "Queues");

            migrationBuilder.DropColumn(
                name: "Message",
                table: "Queues");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "Queues");

            migrationBuilder.DropColumn(
                name: "RefId",
                table: "Queues");
        }
    }
}
