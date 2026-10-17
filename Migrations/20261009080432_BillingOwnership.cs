using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HmisWeb.Migrations
{
    /// <inheritdoc />
    public partial class BillingOwnership : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Concode",
                table: "BILLITEM",
                type: "nvarchar(8)",
                maxLength: 8,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LegacyFlag",
                table: "BILLITEM",
                type: "nvarchar(2)",
                maxLength: 2,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CONCOMM",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    CompanyId = table.Column<long>(type: "bigint", nullable: true),
                    ConCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    StaffId = table.Column<long>(type: "bigint", nullable: true),
                    SCode = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    HmcPer = table.Column<double>(type: "float", nullable: false),
                    Dept = table.Column<string>(type: "nvarchar(4)", maxLength: 4, nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CONCOMM", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "POWNER",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Type = table.Column<string>(type: "nvarchar(1)", maxLength: 1, nullable: true),
                    Regno = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    EncounterId = table.Column<long>(type: "bigint", nullable: true),
                    OwnerCode = table.Column<string>(type: "nvarchar(8)", maxLength: 8, nullable: true),
                    OwnerId = table.Column<long>(type: "bigint", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POWNER", x => x.Id);
                    table.ForeignKey(
                        name: "FK_POWNER_PATIENTENCOUNTER_EncounterId",
                        column: x => x.EncounterId,
                        principalTable: "PATIENTENCOUNTER",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_POWNER_STAFF_OwnerId",
                        column: x => x.OwnerId,
                        principalTable: "STAFF",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "UNSETTLE",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReqNo = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    ReqDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Regno = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    EncounterId = table.Column<long>(type: "bigint", nullable: true),
                    ChgType = table.Column<int>(type: "int", nullable: true),
                    OldBillNo = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    OldBillAmt = table.Column<double>(type: "float", nullable: true),
                    ChgDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ChgBy = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    Reset = table.Column<bool>(type: "bit", nullable: false),
                    NewBillNo = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    NewBillAmt = table.Column<double>(type: "float", nullable: true),
                    NchgDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NchgBy = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    OldDiscAmt = table.Column<double>(type: "float", nullable: true),
                    OldDiscBy = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    NewDiscAmt = table.Column<double>(type: "float", nullable: true),
                    NewDiscBy = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UNSETTLE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UNSETTLE_PATIENTENCOUNTER_EncounterId",
                        column: x => x.EncounterId,
                        principalTable: "PATIENTENCOUNTER",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_CONCOMM_CompanyCode_ConCode_SCode",
                table: "CONCOMM",
                columns: new[] { "CompanyCode", "ConCode", "SCode" });

            migrationBuilder.CreateIndex(
                name: "IX_POWNER_EncounterId",
                table: "POWNER",
                column: "EncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_POWNER_OwnerId",
                table: "POWNER",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_POWNER_Type_Regno",
                table: "POWNER",
                columns: new[] { "Type", "Regno" });

            migrationBuilder.CreateIndex(
                name: "IX_UNSETTLE_EncounterId",
                table: "UNSETTLE",
                column: "EncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_UNSETTLE_ReqNo",
                table: "UNSETTLE",
                column: "ReqNo");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CONCOMM");

            migrationBuilder.DropTable(
                name: "POWNER");

            migrationBuilder.DropTable(
                name: "UNSETTLE");

            migrationBuilder.DropColumn(
                name: "Concode",
                table: "BILLITEM");

            migrationBuilder.DropColumn(
                name: "LegacyFlag",
                table: "BILLITEM");
        }
    }
}
