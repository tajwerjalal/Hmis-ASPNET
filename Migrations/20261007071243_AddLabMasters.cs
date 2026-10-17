using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HmisWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddLabMasters : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "GROWTHDESCRIPTION",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GROWTHDESCRIPTION", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ORGANISM",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ORGANISM", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TESTDATA",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Unit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Range = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CategoryId = table.Column<long>(type: "bigint", nullable: true),
                    OrderNo = table.Column<int>(type: "int", nullable: false),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TESTDATA", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TESTDATA_CATEGORY_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "CATEGORY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TESTREMARK",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemId = table.Column<long>(type: "bigint", nullable: true),
                    TestCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TESTREMARK", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TESTREMARK_ITEM_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_GROWTHDESCRIPTION_Code",
                table: "GROWTHDESCRIPTION",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ORGANISM_Code",
                table: "ORGANISM",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TESTDATA_CategoryId",
                table: "TESTDATA",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_TESTDATA_Code",
                table: "TESTDATA",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TESTREMARK_ItemId",
                table: "TESTREMARK",
                column: "ItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GROWTHDESCRIPTION");

            migrationBuilder.DropTable(
                name: "ORGANISM");

            migrationBuilder.DropTable(
                name: "TESTDATA");

            migrationBuilder.DropTable(
                name: "TESTREMARK");
        }
    }
}
