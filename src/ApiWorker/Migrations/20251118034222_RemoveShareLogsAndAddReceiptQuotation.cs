using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiWorker.Migrations
{
    /// <inheritdoc />
    public partial class RemoveShareLogsAndAddReceiptQuotation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShareLogs");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ShareLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DocumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Channel = table.Column<int>(type: "int", nullable: false),
                    Error = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    MessageId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    SentAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Success = table.Column<bool>(type: "bit", nullable: false),
                    Target = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShareLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShareLogs_Documents_DocumentId",
                        column: x => x.DocumentId,
                        principalTable: "Documents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShareLogs_DocumentId_SentAt",
                table: "ShareLogs",
                columns: new[] { "DocumentId", "SentAt" });
        }
    }
}
