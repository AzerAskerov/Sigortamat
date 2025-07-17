using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace sigortaYoxla.Migrations
{
    /// <inheritdoc />
    public partial class UpdateInsuranceJobForRealData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename columns to match real ISB.az data structure
            migrationBuilder.RenameColumn(
                name: "PlateNumber",
                table: "InsuranceJobs",
                newName: "CarNumber");

            migrationBuilder.RenameColumn(
                name: "InsuranceStatus",
                table: "InsuranceJobs", 
                newName: "Status");

            migrationBuilder.RenameColumn(
                name: "InsuranceCompany",
                table: "InsuranceJobs",
                newName: "Company");

            migrationBuilder.RenameColumn(
                name: "LastCheckedAt",
                table: "InsuranceJobs",
                newName: "ProcessedAt");

            // Drop unused columns
            migrationBuilder.DropColumn(
                name: "VehicleType",
                table: "InsuranceJobs");

            migrationBuilder.DropColumn(
                name: "OwnerName", 
                table: "InsuranceJobs");

            // Add new columns for real ISB.az data
            migrationBuilder.AddColumn<string>(
                name: "VehicleBrand",
                table: "InsuranceJobs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VehicleModel", 
                table: "InsuranceJobs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultText",
                table: "InsuranceJobs",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "InsuranceJobs",
                type: "datetime2",
                nullable: false,
                defaultValueSql: "GETDATE()");

            // Update Company column length for long insurance company names
            migrationBuilder.AlterColumn<string>(
                name: "Company",
                table: "InsuranceJobs",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldNullable: true);

            // Update Status column to be required
            migrationBuilder.AlterColumn<string>(
                name: "Status",
                table: "InsuranceJobs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse the changes
            migrationBuilder.RenameColumn(
                name: "CarNumber",
                table: "InsuranceJobs",
                newName: "PlateNumber");

            migrationBuilder.RenameColumn(
                name: "Status",
                table: "InsuranceJobs",
                newName: "InsuranceStatus");

            migrationBuilder.RenameColumn(
                name: "Company",
                table: "InsuranceJobs",
                newName: "InsuranceCompany");

            migrationBuilder.RenameColumn(
                name: "ProcessedAt",
                table: "InsuranceJobs",
                newName: "LastCheckedAt");

            migrationBuilder.DropColumn(
                name: "VehicleBrand",
                table: "InsuranceJobs");

            migrationBuilder.DropColumn(
                name: "VehicleModel",
                table: "InsuranceJobs");

            migrationBuilder.DropColumn(
                name: "ResultText",
                table: "InsuranceJobs");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "InsuranceJobs");

            migrationBuilder.AddColumn<string>(
                name: "VehicleType",
                table: "InsuranceJobs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OwnerName",
                table: "InsuranceJobs",
                type: "nvarchar(100)", 
                maxLength: 100,
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InsuranceCompany",
                table: "InsuranceJobs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(200)",
                oldMaxLength: 200,
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "InsuranceStatus",
                table: "InsuranceJobs",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldNullable: false,
                oldDefaultValue: "");
        }
    }
}
