using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sigortaYoxla.Migrations
{
    /// <inheritdoc />
    public partial class AddProcessAfterToQueue : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessAfter",
                table: "Queues",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProcessAfter",
                table: "Queues");
        }
    }
}
