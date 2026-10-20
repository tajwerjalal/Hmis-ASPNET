using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HmisWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddCashierModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ACCCHEAD",
                columns: table => new
                {
                    ACCNO = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ACCDESC = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ACCCHEAD", x => x.ACCNO);
                });

            migrationBuilder.CreateTable(
                name: "CASHCNT",
                columns: table => new
                {
                    CCODE = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LCODE = table.Column<int>(type: "int", nullable: false),
                    DESC = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ACTIVE_DT = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ACTIVE_TM = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ACTIVE_BY = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ACTIVE_KEY = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CLOSE_DT = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CLOSE_TM = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CLOSE_BY = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VNO = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NET_CASH = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    STATUS = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CASHCNT", x => new { x.CCODE, x.LCODE });
                });

            migrationBuilder.CreateTable(
                name: "CCONTROL",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RECNO = table.Column<int>(type: "int", nullable: false),
                    PAYNO = table.Column<int>(type: "int", nullable: false),
                    VOUNO = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CCONTROL", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CCOUNTER",
                columns: table => new
                {
                    CCODE = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DESC = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NEXT_NO = table.Column<int>(type: "int", nullable: false),
                    STATUS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    USER_ID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UDATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UTIME = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CCOUNTER", x => x.CCODE);
                });

            migrationBuilder.CreateTable(
                name: "CHIST",
                columns: table => new
                {
                    VOUNO = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    VOUDATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VOUTIME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TRTYPE = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    TRNO = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    TRDATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TRTIME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TYPE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    REGNO = table.Column<int>(type: "int", nullable: true),
                    ECODE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SETTLE = table.Column<bool>(type: "bit", nullable: false),
                    REFERENCE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MODE = table.Column<int>(type: "int", nullable: false),
                    CARDTYPE = table.Column<int>(type: "int", nullable: false),
                    CARDNAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CARDNO = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CARDVALID = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CARDAPPRNO = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CDISCOUNT = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DISCBY = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TOTALAMT = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    USERID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UDATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UTIME = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHIST", x => x.VOUNO);
                });

            migrationBuilder.CreateTable(
                name: "CPYMT",
                columns: table => new
                {
                    TRNO = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CCODE = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    LCODE = table.Column<int>(type: "int", nullable: false),
                    TRDATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TRTIME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TYPE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    REGNO = table.Column<int>(type: "int", nullable: true),
                    ECODE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SETTLE = table.Column<bool>(type: "bit", nullable: false),
                    REFERENCE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CDISCOUNT = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DISCBY = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TOTALAMT = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    USERID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UDATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UTIME = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CPYMT", x => x.TRNO);
                });

            migrationBuilder.CreateTable(
                name: "CRCVD",
                columns: table => new
                {
                    TRNO = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CCODE = table.Column<string>(type: "nvarchar(450)", nullable: true),
                    LCODE = table.Column<int>(type: "int", nullable: false),
                    TRDATE = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TRTIME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TYPE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    REGNO = table.Column<int>(type: "int", nullable: true),
                    SETTLE = table.Column<bool>(type: "bit", nullable: false),
                    REFERENCE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MODE = table.Column<int>(type: "int", nullable: false),
                    CARDTYPE = table.Column<int>(type: "int", nullable: false),
                    CARDNAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CARDNO = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CARDVALID = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CARDAPPRNO = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CDISCOUNT = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DISCBY = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TOTALAMT = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    USERID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UDATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UTIME = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CRCVD", x => x.TRNO);
                });

            migrationBuilder.CreateTable(
                name: "OTTHEATRE",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    TheatreType = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Active = table.Column<bool>(type: "bit", nullable: false),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OTTHEATRE", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CHISTDET",
                columns: table => new
                {
                    TRTYPE = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    TRNO = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ACCNO = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DEPT = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    INDENTNO = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    REMARKS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AMOUNT = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    USERID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UDATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UTIME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CashHistoryVouNo = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CHISTDET", x => new { x.TRTYPE, x.TRNO, x.ACCNO, x.DEPT });
                    table.ForeignKey(
                        name: "FK_CHISTDET_CHIST_CashHistoryVouNo",
                        column: x => x.CashHistoryVouNo,
                        principalTable: "CHIST",
                        principalColumn: "VOUNO");
                });

            migrationBuilder.CreateTable(
                name: "CPYMtdet",
                columns: table => new
                {
                    TRNO = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ACCNO = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DEPT = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    INDENTNO = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    REMARKS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AMOUNT = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    USERID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UDATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UTIME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CashPaymentTrNo = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CPYMtdet", x => new { x.TRNO, x.ACCNO, x.DEPT });
                    table.ForeignKey(
                        name: "FK_CPYMtdet_CPYMT_CashPaymentTrNo",
                        column: x => x.CashPaymentTrNo,
                        principalTable: "CPYMT",
                        principalColumn: "TRNO");
                });

            migrationBuilder.CreateTable(
                name: "CRCVDDET",
                columns: table => new
                {
                    TRNO = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ACCNO = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    DEPT = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    INDENTNO = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    REMARKS = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AMOUNT = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    USERID = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UDATE = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UTIME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CashReceiptTrNo = table.Column<string>(type: "nvarchar(450)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CRCVDDET", x => new { x.TRNO, x.ACCNO, x.DEPT });
                    table.ForeignKey(
                        name: "FK_CRCVDDET_CRCVD_CashReceiptTrNo",
                        column: x => x.CashReceiptTrNo,
                        principalTable: "CRCVD",
                        principalColumn: "TRNO");
                });

            migrationBuilder.CreateTable(
                name: "OTCASE",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseNo = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    PatientId = table.Column<long>(type: "bigint", nullable: false),
                    TheatreId = table.Column<long>(type: "bigint", nullable: true),
                    SurgeonId = table.Column<long>(type: "bigint", nullable: true),
                    AnesthetistId = table.Column<long>(type: "bigint", nullable: true),
                    ProcedureItemId = table.Column<long>(type: "bigint", nullable: true),
                    ProcedurePlanned = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CaseType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    SlotTime = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    DurationMins = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AnesthesiaPlanned = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AnesthesiaGiven = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    AsaGrade = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: true),
                    Fitness = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    ActualStart = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActualEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcedurePerformed = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    Findings = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Outcome = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                    Complications = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    PostOpInstructions = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CancelReason = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    BillNo = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    Remarks = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OTCASE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OTCASE_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OTCASE_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_OTCASE_ITEM_ProcedureItemId",
                        column: x => x.ProcedureItemId,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OTCASE_OTTHEATRE_TheatreId",
                        column: x => x.TheatreId,
                        principalTable: "OTTHEATRE",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OTCASE_PATIENT_PatientId",
                        column: x => x.PatientId,
                        principalTable: "PATIENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OTCASE_STAFF_AnesthetistId",
                        column: x => x.AnesthetistId,
                        principalTable: "STAFF",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_OTCASE_STAFF_SurgeonId",
                        column: x => x.SurgeonId,
                        principalTable: "STAFF",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OTCASEITEM",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CaseId = table.Column<long>(type: "bigint", nullable: false),
                    ItemId = table.Column<int>(type: "int", nullable: true),
                    ItemName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Qty = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Rate = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Chargeable = table.Column<bool>(type: "bit", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OTCASEITEM", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OTCASEITEM_OTCASE_CaseId",
                        column: x => x.CaseId,
                        principalTable: "OTCASE",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_OTCASEITEM_PHITEM_ItemId",
                        column: x => x.ItemId,
                        principalTable: "PHITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // ------------------------------------------------------------------
            // SAFETY FIX - Developer: Tajwer Jalal | Date: 20-Oct-2026 | Time: 19:40
            // The scaffolded Down() below is as generated except for the table
            // list. As scaffolded it ALSO called DropTable for the legacy VFP
            // mirrors and pharmacy tables (BCONTROL, REGISTER, IBIL_*, OBIL_*,
            // IPDBILL, OPDBILL, OPDBILL_AUDIT, IpdBillAudit, LegacyDepartment,
            // PH*), which this migration's Up() never created - they come from
            // the raw-SQL migration streams. `dotnet ef database update
            // 2026100908...` executed as generated would have destroyed live
            // data. Down now drops exactly what Up() creates, dependency order.
            // ------------------------------------------------------------------
            migrationBuilder.DropTable(
                name: "CHISTDET");

            migrationBuilder.DropTable(
                name: "CPYMtdet");

            migrationBuilder.DropTable(
                name: "CRCVDDET");

            migrationBuilder.DropTable(
                name: "OTCASEITEM");

            migrationBuilder.DropTable(
                name: "CHIST");

            migrationBuilder.DropTable(
                name: "CPYMT");

            migrationBuilder.DropTable(
                name: "CRCVD");

            migrationBuilder.DropTable(
                name: "OTCASE");

            migrationBuilder.DropTable(
                name: "OTTHEATRE");

            migrationBuilder.DropTable(
                name: "CASHCNT");

            migrationBuilder.DropTable(
                name: "CCOUNTER");

            migrationBuilder.DropTable(
                name: "CCONTROL");

            migrationBuilder.DropTable(
                name: "ACCCHEAD");
        } // Down
    } // class
} // namespace
