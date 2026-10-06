using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HmisWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddPanelServiceCharges : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PANELDEPARTMENT",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillOrder = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PANELDEPARTMENT", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PANELSERVICECHARGE",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyId = table.Column<long>(type: "bigint", nullable: false),
                    DepartmentCode = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    DepartmentName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceCode = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    ServiceName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceGroup = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rate = table.Column<double>(type: "float", nullable: false),
                    ForeignerRate = table.Column<double>(type: "float", nullable: true),
                    StandardRate = table.Column<double>(type: "float", nullable: true),
                    StandardForeignerRate = table.Column<double>(type: "float", nullable: true),
                    ItemId = table.Column<long>(type: "bigint", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PANELSERVICECHARGE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PANELSERVICECHARGE_INSTITUTION_CompanyId",
                        column: x => x.CompanyId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PANELSERVICECHARGE_ITEM_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PANELDEPARTMENT_Code",
                table: "PANELDEPARTMENT",
                column: "Code",
                unique: true,
                filter: "[Code] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PANELSERVICECHARGE_CompanyId_DepartmentCode_ServiceCode",
                table: "PANELSERVICECHARGE",
                columns: new[] { "CompanyId", "DepartmentCode", "ServiceCode" },
                unique: true,
                filter: "[DepartmentCode] IS NOT NULL AND [ServiceCode] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PANELSERVICECHARGE_ItemId",
                table: "PANELSERVICECHARGE",
                column: "ItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PANELDEPARTMENT");

            migrationBuilder.DropTable(
                name: "PANELSERVICECHARGE");
        }
    }
}
