using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sigortaYoxla.Migrations
{
    /// <inheritdoc />
    public partial class AddCheckDateAndRenewalFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CheckDate",
                table: "InsuranceJobs",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "InsuranceRenewalTrackingId",
                table: "InsuranceJobs",
                type: "int",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckDate",
                table: "InsuranceJobs");

            migrationBuilder.DropColumn(
                name: "InsuranceRenewalTrackingId",
                table: "InsuranceJobs");
        }
    }
}
