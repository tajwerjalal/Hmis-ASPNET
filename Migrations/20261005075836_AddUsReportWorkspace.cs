using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HmisWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddUsReportWorkspace : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "CharCount",
                table: "USORDER",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinalizedAt",
                table: "USORDER",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FinalizedById",
                table: "USORDER",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSavedAt",
                table: "USORDER",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "ReportStatus",
                table: "USORDER",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<long>(
                name: "ReportTemplateId",
                table: "USORDER",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ResultHtml",
                table: "USORDER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "WordCount",
                table: "USORDER",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "MEDICALTERM",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Term = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Definition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Category = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Synonyms = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MEDICALTERM", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "USREPORTTEMPLATE",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsServiceId = table.Column<long>(type: "bigint", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TemplateHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SectionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDefault = table.Column<bool>(type: "bit", nullable: false),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USREPORTTEMPLATE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_USREPORTTEMPLATE_USSERVICE_UsServiceId",
                        column: x => x.UsServiceId,
                        principalTable: "USSERVICE",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_USREPORTTEMPLATE_WEBUSER_CreaterId",
                        column: x => x.CreaterId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_USREPORTTEMPLATE_WEBUSER_EditerId",
                        column: x => x.EditerId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_USORDER_FinalizedById",
                table: "USORDER",
                column: "FinalizedById");

            migrationBuilder.CreateIndex(
                name: "IX_USORDER_ReportTemplateId",
                table: "USORDER",
                column: "ReportTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_USREPORTTEMPLATE_CreaterId",
                table: "USREPORTTEMPLATE",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_USREPORTTEMPLATE_EditerId",
                table: "USREPORTTEMPLATE",
                column: "EditerId");

            migrationBuilder.CreateIndex(
                name: "IX_USREPORTTEMPLATE_UsServiceId",
                table: "USREPORTTEMPLATE",
                column: "UsServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_USORDER_USREPORTTEMPLATE_ReportTemplateId",
                table: "USORDER",
                column: "ReportTemplateId",
                principalTable: "USREPORTTEMPLATE",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_USORDER_WEBUSER_FinalizedById",
                table: "USORDER",
                column: "FinalizedById",
                principalTable: "WEBUSER",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_USORDER_USREPORTTEMPLATE_ReportTemplateId",
                table: "USORDER");

            migrationBuilder.DropForeignKey(
                name: "FK_USORDER_WEBUSER_FinalizedById",
                table: "USORDER");

            migrationBuilder.DropTable(
                name: "MEDICALTERM");

            migrationBuilder.DropTable(
                name: "USREPORTTEMPLATE");

            migrationBuilder.DropIndex(
                name: "IX_USORDER_FinalizedById",
                table: "USORDER");

            migrationBuilder.DropIndex(
                name: "IX_USORDER_ReportTemplateId",
                table: "USORDER");

            migrationBuilder.DropColumn(
                name: "CharCount",
                table: "USORDER");

            migrationBuilder.DropColumn(
                name: "FinalizedAt",
                table: "USORDER");

            migrationBuilder.DropColumn(
                name: "FinalizedById",
                table: "USORDER");

            migrationBuilder.DropColumn(
                name: "LastSavedAt",
                table: "USORDER");

            migrationBuilder.DropColumn(
                name: "ReportStatus",
                table: "USORDER");

            migrationBuilder.DropColumn(
                name: "ReportTemplateId",
                table: "USORDER");

            migrationBuilder.DropColumn(
                name: "ResultHtml",
                table: "USORDER");

            migrationBuilder.DropColumn(
                name: "WordCount",
                table: "USORDER");
        }
    }
}
