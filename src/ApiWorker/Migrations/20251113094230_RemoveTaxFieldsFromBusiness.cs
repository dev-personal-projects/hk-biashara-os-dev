using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiWorker.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTaxFieldsFromBusiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Currency",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "UsesVat",
                table: "businesses");

            migrationBuilder.DropColumn(
                name: "DefaultTaxRate",
                table: "businesses");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Currency",
                table: "businesses",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: false,
                defaultValue: "KES");

            migrationBuilder.AddColumn<bool>(
                name: "UsesVat",
                table: "businesses",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultTaxRate",
                table: "businesses",
                type: "decimal(5,2)",
                nullable: true,
                defaultValue: 16.0m);
        }
    }
}
