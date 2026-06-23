using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voluble.PermitDesk.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddLocalFolderPath : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "LocalFolderPath",
                table: "Cases",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LocalFolderPath",
                table: "Cases");
        }
    }
}
