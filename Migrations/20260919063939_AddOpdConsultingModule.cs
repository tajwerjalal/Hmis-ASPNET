using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HmisWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddOpdConsultingModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "SessionId",
                table: "TOKEN",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "TOKEN",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ConsultantSettlementId",
                table: "BILL",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CONSULTANTSETTLEMENT",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsultantId = table.Column<long>(type: "bigint", nullable: true),
                    SettlementDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TokenCount = table.Column<int>(type: "int", nullable: false),
                    BillCount = table.Column<int>(type: "int", nullable: false),
                    AmountHospital = table.Column<double>(type: "float", nullable: false),
                    AmountHandedOver = table.Column<double>(type: "float", nullable: false),
                    AmountTotal = table.Column<double>(type: "float", nullable: false),
                    PaymentMethod = table.Column<int>(type: "int", nullable: true),
                    ReceivedById = table.Column<long>(type: "bigint", nullable: true),
                    ReceivedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CONSULTANTSETTLEMENT", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CONSULTANTSETTLEMENT_STAFF_ConsultantId",
                        column: x => x.ConsultantId,
                        principalTable: "STAFF",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CONSULTANTSETTLEMENT_STAFF_ReceivedById",
                        column: x => x.ReceivedById,
                        principalTable: "STAFF",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CONSULTANTSETTLEMENT_WEBUSER_CreaterId",
                        column: x => x.CreaterId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OPDSESSION",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ConsultantId = table.Column<long>(type: "bigint", nullable: true),
                    SpecialityId = table.Column<long>(type: "bigint", nullable: true),
                    SessionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FromTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RoomNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Fee = table.Column<double>(type: "float", nullable: false),
                    HospitalFee = table.Column<double>(type: "float", nullable: false),
                    TokenStartingNo = table.Column<int>(type: "int", nullable: false),
                    TokenMax = table.Column<int>(type: "int", nullable: false),
                    TokenIncrement = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Note = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OPDSESSION", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OPDSESSION_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPDSESSION_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPDSESSION_SPECIALITY_SpecialityId",
                        column: x => x.SpecialityId,
                        principalTable: "SPECIALITY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPDSESSION_STAFF_ConsultantId",
                        column: x => x.ConsultantId,
                        principalTable: "STAFF",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OPDSESSION_WEBUSER_CreaterId",
                        column: x => x.CreaterId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TOKEN_SessionId",
                table: "TOKEN",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_ConsultantSettlementId",
                table: "BILL",
                column: "ConsultantSettlementId");

            migrationBuilder.CreateIndex(
                name: "IX_CONSULTANTSETTLEMENT_ConsultantId",
                table: "CONSULTANTSETTLEMENT",
                column: "ConsultantId");

            migrationBuilder.CreateIndex(
                name: "IX_CONSULTANTSETTLEMENT_CreaterId",
                table: "CONSULTANTSETTLEMENT",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_CONSULTANTSETTLEMENT_ReceivedById",
                table: "CONSULTANTSETTLEMENT",
                column: "ReceivedById");

            migrationBuilder.CreateIndex(
                name: "IX_OPDSESSION_ConsultantId",
                table: "OPDSESSION",
                column: "ConsultantId");

            migrationBuilder.CreateIndex(
                name: "IX_OPDSESSION_CreaterId",
                table: "OPDSESSION",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_OPDSESSION_DepartmentId",
                table: "OPDSESSION",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_OPDSESSION_InstitutionId",
                table: "OPDSESSION",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_OPDSESSION_SpecialityId",
                table: "OPDSESSION",
                column: "SpecialityId");

            migrationBuilder.AddForeignKey(
                name: "FK_BILL_CONSULTANTSETTLEMENT_ConsultantSettlementId",
                table: "BILL",
                column: "ConsultantSettlementId",
                principalTable: "CONSULTANTSETTLEMENT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TOKEN_OPDSESSION_SessionId",
                table: "TOKEN",
                column: "SessionId",
                principalTable: "OPDSESSION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BILL_CONSULTANTSETTLEMENT_ConsultantSettlementId",
                table: "BILL");

            migrationBuilder.DropForeignKey(
                name: "FK_TOKEN_OPDSESSION_SessionId",
                table: "TOKEN");

            migrationBuilder.DropTable(
                name: "CONSULTANTSETTLEMENT");

            migrationBuilder.DropTable(
                name: "OPDSESSION");

            migrationBuilder.DropIndex(
                name: "IX_TOKEN_SessionId",
                table: "TOKEN");

            migrationBuilder.DropIndex(
                name: "IX_BILL_ConsultantSettlementId",
                table: "BILL");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "TOKEN");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "TOKEN");

            migrationBuilder.DropColumn(
                name: "ConsultantSettlementId",
                table: "BILL");
        }
    }
}
