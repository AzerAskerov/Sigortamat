using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Sigortamat.Migrations
{
    /// <inheritdoc />
    public partial class AddRenewalWindowToUsers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "RenewalWindowEnd",
                table: "Users",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "RenewalWindowStart",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RenewalWindowEnd",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RenewalWindowStart",
                table: "Users");
        }
    }
}
