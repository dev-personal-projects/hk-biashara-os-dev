using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiWorker.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPreviewBlobUrl : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PreviewBlobUrl",
                table: "Documents",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreviewBlobUrl",
                table: "Documents");
        }
    }
}
