using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voluble.PermitDesk.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSubmissionFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "MaterialTypes",
                table: "Cases",
                type: "character varying(128)",
                maxLength: 128,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "PurposeStatement",
                table: "Cases",
                type: "character varying(2048)",
                maxLength: 2048,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaterialTypes",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "PurposeStatement",
                table: "Cases");
        }
    }
}
