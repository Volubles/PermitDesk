using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Voluble.PermitDesk.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddMailEvent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MailEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CaseId = table.Column<Guid>(type: "uuid", nullable: true),
                    ExternalMessageId = table.Column<string>(type: "character varying(998)", maxLength: 998, nullable: false),
                    From = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    Subject = table.Column<string>(type: "character varying(998)", maxLength: 998, nullable: false),
                    BodyPreview = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Classification = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    MatchConfidence = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    PdfFileId = table.Column<Guid>(type: "uuid", nullable: true),
                    ProcessedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    ProcessingStatus = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    ProcessingError = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MailEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MailEvents_CaseFiles_PdfFileId",
                        column: x => x.PdfFileId,
                        principalTable: "CaseFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_MailEvents_Cases_CaseId",
                        column: x => x.CaseId,
                        principalTable: "Cases",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MailEvents_CaseId",
                table: "MailEvents",
                column: "CaseId");

            migrationBuilder.CreateIndex(
                name: "IX_MailEvents_ExternalMessageId",
                table: "MailEvents",
                column: "ExternalMessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MailEvents_PdfFileId",
                table: "MailEvents",
                column: "PdfFileId");

            migrationBuilder.CreateIndex(
                name: "IX_MailEvents_ProcessingStatus",
                table: "MailEvents",
                column: "ProcessingStatus");

            migrationBuilder.CreateIndex(
                name: "IX_MailEvents_ReceivedAt",
                table: "MailEvents",
                column: "ReceivedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MailEvents");
        }
    }
}
