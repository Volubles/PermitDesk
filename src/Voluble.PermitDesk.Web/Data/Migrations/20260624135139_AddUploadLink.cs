using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voluble.PermitDesk.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUploadLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UploadLink",
                table: "Cases",
                type: "character varying(1024)",
                maxLength: 1024,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UploadLinkReceivedAt",
                table: "Cases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "UploadLinkUsedAt",
                table: "Cases",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Cases_LantmaterietReferenceNumber",
                table: "Cases",
                column: "LantmaterietReferenceNumber");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Cases_LantmaterietReferenceNumber",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "UploadLink",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "UploadLinkReceivedAt",
                table: "Cases");

            migrationBuilder.DropColumn(
                name: "UploadLinkUsedAt",
                table: "Cases");
        }
    }
}
