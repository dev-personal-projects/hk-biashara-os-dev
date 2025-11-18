using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiWorker.Migrations
{
    /// <inheritdoc />
    public partial class DocumentSignaturesAndThemes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreviewBlobUrl",
                table: "DocumentTemplates",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ThemeJson",
                table: "DocumentTemplates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "PreviewBlobUrl",
                table: "Documents",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AppliedThemeJson",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureBlobUrl",
                table: "Documents",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignatureNotes",
                table: "Documents",
                type: "nvarchar(512)",
                maxLength: 512,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "SignedAt",
                table: "Documents",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SignedBy",
                table: "Documents",
                type: "nvarchar(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "TemplateId",
                table: "Documents",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Documents_TemplateId",
                table: "Documents",
                column: "TemplateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Documents_DocumentTemplates_TemplateId",
                table: "Documents",
                column: "TemplateId",
                principalTable: "DocumentTemplates",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Documents_DocumentTemplates_TemplateId",
                table: "Documents");

            migrationBuilder.DropIndex(
                name: "IX_Documents_TemplateId",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "PreviewBlobUrl",
                table: "DocumentTemplates");

            migrationBuilder.DropColumn(
                name: "ThemeJson",
                table: "DocumentTemplates");

            migrationBuilder.DropColumn(
                name: "AppliedThemeJson",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SignatureBlobUrl",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SignatureNotes",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SignedAt",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "SignedBy",
                table: "Documents");

            migrationBuilder.DropColumn(
                name: "TemplateId",
                table: "Documents");

            migrationBuilder.AlterColumn<string>(
                name: "PreviewBlobUrl",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(512)",
                oldMaxLength: 512,
                oldNullable: true);
        }
    }
}
