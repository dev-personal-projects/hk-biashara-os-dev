using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApiWorker.Migrations
{
    /// <inheritdoc />
    public partial class AddLogoUrlColumnToBusiness : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[businesses]') AND name = 'LogoUrl')
                BEGIN
                    ALTER TABLE [businesses] ADD [LogoUrl] nvarchar(max) NULL
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID(N'[businesses]') AND name = 'LogoUrl')
                BEGIN
                    ALTER TABLE [businesses] DROP COLUMN [LogoUrl]
                END
            ");
        }
    }
}
