using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HmisWeb.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreateFull : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AUDITEVENT",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EventDataTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EventEndTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WebUserId = table.Column<long>(type: "bigint", nullable: true),
                    Uuid = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventTrigger = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    BeforeEdit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterEdit = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EventDuration = table.Column<long>(type: "bigint", nullable: true),
                    EventStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AUDITEVENT", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BILLENTRY",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TotDiscount = table.Column<double>(type: "float", nullable: true),
                    TotCash = table.Column<double>(type: "float", nullable: true),
                    TotCredit = table.Column<double>(type: "float", nullable: true),
                    TotCreditCard = table.Column<double>(type: "float", nullable: true),
                    TotVat = table.Column<double>(type: "float", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BILLENTRY", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ITEMPACKAGE",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ITEMPACKAGE", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WEBLANGUAGE",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    OrderNo = table.Column<double>(type: "float", nullable: false),
                    DefaultLanguage = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WEBLANGUAGE", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WEBCONTENT",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    Sinhala = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tamil = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    English = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrderNo = table.Column<double>(type: "float", nullable: false),
                    WebLanguageId = table.Column<long>(type: "bigint", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: true),
                    ShortContext = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LongContext = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ParentId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WEBCONTENT", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WEBCONTENT_WEBCONTENT_ParentId",
                        column: x => x.ParentId,
                        principalTable: "WEBCONTENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WEBCONTENT_WEBLANGUAGE_WebLanguageId",
                        column: x => x.WebLanguageId,
                        principalTable: "WEBLANGUAGE",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ADMISSION",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AdmissionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DischargeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PatientEncounterId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ADMISSION", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AGENTHISTORY",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BalanceBeforeTransaction = table.Column<double>(type: "float", nullable: false),
                    BalanceAfterTransaction = table.Column<double>(type: "float", nullable: false),
                    TransactionValue = table.Column<double>(type: "float", nullable: false),
                    AdjustmentToAgencyBalance = table.Column<double>(type: "float", nullable: true),
                    PaidAmountToAgency = table.Column<double>(type: "float", nullable: true),
                    PaidAmountByAgency = table.Column<double>(type: "float", nullable: true),
                    CompanyTransactionValue = table.Column<double>(type: "float", nullable: false),
                    CompanyBalanceBefore = table.Column<double>(type: "float", nullable: false),
                    CompanyBalanceAfter = table.Column<double>(type: "float", nullable: false),
                    AgentTransactionValue = table.Column<double>(type: "float", nullable: false),
                    AgentBalanceBefore = table.Column<double>(type: "float", nullable: false),
                    AgentBalanceAfter = table.Column<double>(type: "float", nullable: false),
                    StaffTrasnactionValue = table.Column<double>(type: "float", nullable: false),
                    CollectingCentertransactionValue = table.Column<double>(type: "float", nullable: false),
                    HistoryType = table.Column<int>(type: "int", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillItemId = table.Column<long>(type: "bigint", nullable: true),
                    BillId = table.Column<long>(type: "bigint", nullable: true),
                    BillSessionId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    AgencyId = table.Column<long>(type: "bigint", nullable: true),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AGENTHISTORY", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AMP",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AMP", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "APIKEY",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Key = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_APIKEY", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "APPEMAIL",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SmsType = table.Column<int>(type: "int", nullable: true),
                    ReceipientEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MessageSubject = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MessageBody = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SenderUsername = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SenderPassword = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SenderEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Attachment1 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Attachment2 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Attachment3 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Attachment4 = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SentSuccessfully = table.Column<bool>(type: "bit", nullable: true),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PatientReportId = table.Column<long>(type: "bigint", nullable: true),
                    PatientInvestigationId = table.Column<long>(type: "bigint", nullable: true),
                    BillId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_APPEMAIL", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AREA",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AREA", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BILL",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BillClassType = table.Column<int>(type: "int", nullable: true),
                    BillType = table.Column<int>(type: "int", nullable: true),
                    BillTypeAtomic = table.Column<int>(type: "int", nullable: true),
                    PaymentMethod = table.Column<int>(type: "int", nullable: true),
                    SurgeryBillType = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreditCardRefNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChequeRefNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreditDuration = table.Column<int>(type: "int", nullable: false),
                    ChequeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InvoiceDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    QutationNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Margin = table.Column<double>(type: "float", nullable: false),
                    Total = table.Column<double>(type: "float", nullable: false),
                    NetTotal = table.Column<double>(type: "float", nullable: false),
                    Discount = table.Column<double>(type: "float", nullable: false),
                    Vat = table.Column<double>(type: "float", nullable: false),
                    VatPlusNetTotal = table.Column<double>(type: "float", nullable: false),
                    DiscountPercent = table.Column<double>(type: "float", nullable: false),
                    BillTotal = table.Column<double>(type: "float", nullable: false),
                    PaidAmount = table.Column<double>(type: "float", nullable: false),
                    RefundAmount = table.Column<double>(type: "float", nullable: false),
                    Balance = table.Column<double>(type: "float", nullable: false),
                    ServiceCharge = table.Column<double>(type: "float", nullable: false),
                    Tax = table.Column<double>(type: "float", nullable: true),
                    CashPaid = table.Column<double>(type: "float", nullable: true),
                    CashBalance = table.Column<double>(type: "float", nullable: true),
                    SaleValue = table.Column<double>(type: "float", nullable: false),
                    FreeValue = table.Column<double>(type: "float", nullable: false),
                    PerformInstitutionFee = table.Column<double>(type: "float", nullable: false),
                    StaffFee = table.Column<double>(type: "float", nullable: false),
                    BillerFee = table.Column<double>(type: "float", nullable: false),
                    GrantTotal = table.Column<double>(type: "float", nullable: false),
                    ExpenseTotal = table.Column<double>(type: "float", nullable: false),
                    GrnNetTotal = table.Column<double>(type: "float", nullable: false),
                    HospitalFee = table.Column<double>(type: "float", nullable: false),
                    ProfessionalFee = table.Column<double>(type: "float", nullable: false),
                    Qty = table.Column<double>(type: "float", nullable: false),
                    TenderedAmount = table.Column<double>(type: "float", nullable: false),
                    TotalHospitalFee = table.Column<double>(type: "float", nullable: false),
                    TotalCenterFee = table.Column<double>(type: "float", nullable: false),
                    TotalStaffFee = table.Column<double>(type: "float", nullable: false),
                    ClaimableTotal = table.Column<double>(type: "float", nullable: false),
                    DeptId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InsId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CatId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BookingId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InvoiceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgentRefNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpOpOrCc = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LocalNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Paid = table.Column<bool>(type: "bit", nullable: false),
                    Cancelled = table.Column<bool>(type: "bit", nullable: false),
                    Refunded = table.Column<bool>(type: "bit", nullable: false),
                    Reactivated = table.Column<bool>(type: "bit", nullable: false),
                    Printed = table.Column<bool>(type: "bit", nullable: false),
                    DuplicatedPrinted = table.Column<bool>(type: "bit", nullable: false),
                    BillClosed = table.Column<bool>(type: "bit", nullable: false),
                    BillPaymentCompletelySettled = table.Column<bool>(type: "bit", nullable: false),
                    Smsed = table.Column<bool>(type: "bit", nullable: true),
                    BillDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BillTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AppointmentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApproveAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PrintedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DuplicatePrintedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SmsedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ItemId = table.Column<long>(type: "bigint", nullable: true),
                    MembershipSchemeId = table.Column<long>(type: "bigint", nullable: true),
                    CategoryId = table.Column<long>(type: "bigint", nullable: true),
                    CashTransactionId = table.Column<long>(type: "bigint", nullable: true),
                    BankId = table.Column<long>(type: "bigint", nullable: true),
                    ApproveUserId = table.Column<long>(type: "bigint", nullable: true),
                    PaymentSchemeInstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    CollectingCentreId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    FromInstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    ToInstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    CreditCompanyId = table.Column<long>(type: "bigint", nullable: true),
                    ReferenceInstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    ReferringDepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    FromDepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    ToDepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    BilledBillId = table.Column<long>(type: "bigint", nullable: true),
                    CancelledBillId = table.Column<long>(type: "bigint", nullable: true),
                    RefundedBillId = table.Column<long>(type: "bigint", nullable: true),
                    ReactivatedBillId = table.Column<long>(type: "bigint", nullable: true),
                    ReferenceBillId = table.Column<long>(type: "bigint", nullable: true),
                    ForwardReferenceBillId = table.Column<long>(type: "bigint", nullable: true),
                    BackwardReferenceBillId = table.Column<long>(type: "bigint", nullable: true),
                    StaffId = table.Column<long>(type: "bigint", nullable: true),
                    FromStaffId = table.Column<long>(type: "bigint", nullable: true),
                    ToStaffId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    EditorId = table.Column<long>(type: "bigint", nullable: true),
                    CheckedById = table.Column<long>(type: "bigint", nullable: true),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    PaymentSchemeId = table.Column<long>(type: "bigint", nullable: true),
                    PersonId = table.Column<long>(type: "bigint", nullable: true),
                    PatientId = table.Column<long>(type: "bigint", nullable: true),
                    ReferredById = table.Column<long>(type: "bigint", nullable: true),
                    PatientEncounterId = table.Column<long>(type: "bigint", nullable: true),
                    ProcedureId = table.Column<long>(type: "bigint", nullable: true),
                    BillPackegeId = table.Column<long>(type: "bigint", nullable: true),
                    SingleBillItemId = table.Column<long>(type: "bigint", nullable: true),
                    SingleBillSessionId = table.Column<long>(type: "bigint", nullable: true),
                    ReferredByInstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    PaidBillId = table.Column<long>(type: "bigint", nullable: true),
                    ToWebUserId = table.Column<long>(type: "bigint", nullable: true),
                    FromWebUserId = table.Column<long>(type: "bigint", nullable: true),
                    SmsedUserId = table.Column<long>(type: "bigint", nullable: true),
                    PrintedUserId = table.Column<long>(type: "bigint", nullable: true),
                    DuplicatePrintedUserId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckeAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DTYPE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DTYPE1 = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BILL", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BILL_BILL_BackwardReferenceBillId",
                        column: x => x.BackwardReferenceBillId,
                        principalTable: "BILL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BILL_BILL_BilledBillId",
                        column: x => x.BilledBillId,
                        principalTable: "BILL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BILL_BILL_CancelledBillId",
                        column: x => x.CancelledBillId,
                        principalTable: "BILL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BILL_BILL_ForwardReferenceBillId",
                        column: x => x.ForwardReferenceBillId,
                        principalTable: "BILL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BILL_BILL_PaidBillId",
                        column: x => x.PaidBillId,
                        principalTable: "BILL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BILL_BILL_ReferenceBillId",
                        column: x => x.ReferenceBillId,
                        principalTable: "BILL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BILL_BILL_RefundedBillId",
                        column: x => x.RefundedBillId,
                        principalTable: "BILL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BILLCOMPONENT",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeptId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InsId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CatId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ComponentValue = table.Column<double>(type: "float", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PackegeId = table.Column<long>(type: "bigint", nullable: true),
                    ItemId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    BillId = table.Column<long>(type: "bigint", nullable: true),
                    BillItemId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    StaffId = table.Column<long>(type: "bigint", nullable: true),
                    SpecialityId = table.Column<long>(type: "bigint", nullable: true),
                    PaymentMethodValueId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BILLCOMPONENT", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BILLCOMPONENT_BILL_BillId",
                        column: x => x.BillId,
                        principalTable: "BILL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BILLFEE",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeeValue = table.Column<double>(type: "float", nullable: false),
                    FeeGrossValue = table.Column<double>(type: "float", nullable: true),
                    FeeDiscount = table.Column<double>(type: "float", nullable: false),
                    FeeVat = table.Column<double>(type: "float", nullable: false),
                    FeeVatPlusValue = table.Column<double>(type: "float", nullable: false),
                    FeeMargin = table.Column<double>(type: "float", nullable: false),
                    FeeAdjusted = table.Column<double>(type: "float", nullable: false),
                    PaidValue = table.Column<double>(type: "float", nullable: false),
                    SettleValue = table.Column<double>(type: "float", nullable: false),
                    OrderNo = table.Column<int>(type: "int", nullable: false),
                    FeeAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FeeId = table.Column<long>(type: "bigint", nullable: true),
                    PatientId = table.Column<long>(type: "bigint", nullable: true),
                    PatienEncounterId = table.Column<long>(type: "bigint", nullable: true),
                    ChildEncounterId = table.Column<long>(type: "bigint", nullable: true),
                    StaffId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    SpecialityId = table.Column<long>(type: "bigint", nullable: true),
                    ReferenceBillFeeId = table.Column<long>(type: "bigint", nullable: true),
                    PatientItemId = table.Column<long>(type: "bigint", nullable: true),
                    PriceMatrixId = table.Column<long>(type: "bigint", nullable: true),
                    BillItemId = table.Column<long>(type: "bigint", nullable: true),
                    BillId = table.Column<long>(type: "bigint", nullable: true),
                    ReferenceBillItemId = table.Column<long>(type: "bigint", nullable: true),
                    ReferencePatientRoomId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    EditorId = table.Column<long>(type: "bigint", nullable: true),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BILLFEE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BILLFEE_BILLFEE_ReferenceBillFeeId",
                        column: x => x.ReferenceBillFeeId,
                        principalTable: "BILLFEE",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BILLFEE_BILL_BillId",
                        column: x => x.BillId,
                        principalTable: "BILL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BILLFEEPAYMENT",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Amount = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    BillFeeId = table.Column<long>(type: "bigint", nullable: true),
                    PaymentId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BILLFEEPAYMENT", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BILLFEEPAYMENT_BILLFEE_BillFeeId",
                        column: x => x.BillFeeId,
                        principalTable: "BILLFEE",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BILLITEM",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Qty = table.Column<double>(type: "float", nullable: true),
                    Descreption = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RemainingQty = table.Column<double>(type: "float", nullable: false),
                    Rate = table.Column<double>(type: "float", nullable: false),
                    DiscountRate = table.Column<double>(type: "float", nullable: false),
                    MarginRate = table.Column<double>(type: "float", nullable: false),
                    NetRate = table.Column<double>(type: "float", nullable: false),
                    GrossValue = table.Column<double>(type: "float", nullable: false),
                    Discount = table.Column<double>(type: "float", nullable: false),
                    Vat = table.Column<double>(type: "float", nullable: false),
                    NetValue = table.Column<double>(type: "float", nullable: false),
                    VatPlusNetValue = table.Column<double>(type: "float", nullable: false),
                    MarginValue = table.Column<double>(type: "float", nullable: false),
                    AdjustedValue = table.Column<double>(type: "float", nullable: false),
                    HospitalFee = table.Column<double>(type: "float", nullable: false),
                    CollectingCentreFee = table.Column<double>(type: "float", nullable: false),
                    StaffFee = table.Column<double>(type: "float", nullable: false),
                    OtherFee = table.Column<double>(type: "float", nullable: false),
                    FeeValue = table.Column<double>(type: "float", nullable: false),
                    SearialNo = table.Column<int>(type: "int", nullable: false),
                    Refunded = table.Column<bool>(type: "bit", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    InsId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DeptId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CatId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ItemId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgentRefNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FromTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ToTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SessionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BillTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InwardChargeType = table.Column<int>(type: "int", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: true),
                    BillItemStatus = table.Column<int>(type: "int", nullable: true),
                    BillId = table.Column<long>(type: "bigint", nullable: true),
                    ExpenseBillId = table.Column<long>(type: "bigint", nullable: true),
                    ItemId1 = table.Column<long>(type: "bigint", nullable: true),
                    ParentBillItemId = table.Column<long>(type: "bigint", nullable: true),
                    ReferanceBillItemId = table.Column<long>(type: "bigint", nullable: true),
                    PaidForBillFeeId = table.Column<long>(type: "bigint", nullable: true),
                    PatientEncounterId = table.Column<long>(type: "bigint", nullable: true),
                    ReferenceBillId = table.Column<long>(type: "bigint", nullable: true),
                    PriceMatrixId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    EditorId = table.Column<long>(type: "bigint", nullable: true),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BILLITEM", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BILLITEM_BILLFEE_PaidForBillFeeId",
                        column: x => x.PaidForBillFeeId,
                        principalTable: "BILLFEE",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BILLITEM_BILLITEM_ParentBillItemId",
                        column: x => x.ParentBillItemId,
                        principalTable: "BILLITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BILLITEM_BILLITEM_ReferanceBillItemId",
                        column: x => x.ReferanceBillItemId,
                        principalTable: "BILLITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BILLITEM_BILL_BillId",
                        column: x => x.BillId,
                        principalTable: "BILL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BILLITEM_BILL_ExpenseBillId",
                        column: x => x.ExpenseBillId,
                        principalTable: "BILL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BILLITEM_BILL_ReferenceBillId",
                        column: x => x.ReferenceBillId,
                        principalTable: "BILL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BILLNUMBER",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LastBillNumber = table.Column<long>(type: "bigint", nullable: true),
                    BillType = table.Column<int>(type: "int", nullable: true),
                    BillClassType = table.Column<int>(type: "int", nullable: true),
                    BillYear = table.Column<int>(type: "int", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    ToDepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BILLNUMBER", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BILLSESSION",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SessionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SessionNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillId = table.Column<long>(type: "bigint", nullable: true),
                    BillItemId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BILLSESSION", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BILLSESSION_BILLITEM_BillItemId",
                        column: x => x.BillItemId,
                        principalTable: "BILLITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BILLSESSION_BILL_BillId",
                        column: x => x.BillId,
                        principalTable: "BILL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CASHBOOK",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CashBalance = table.Column<double>(type: "float", nullable: false),
                    TotalCashAdded = table.Column<double>(type: "float", nullable: false),
                    TotalCashRemoved = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CASHBOOK", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CASHBOOKENTRY",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CashIn = table.Column<double>(type: "float", nullable: false),
                    CashOut = table.Column<double>(type: "float", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CashBalance = table.Column<double>(type: "float", nullable: false),
                    CashBookId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CASHBOOKENTRY", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CASHBOOKENTRY_CASHBOOK_CashBookId",
                        column: x => x.CashBookId,
                        principalTable: "CASHBOOK",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CASHTRANSACTION",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayingAmount = table.Column<double>(type: "float", nullable: true),
                    ChangeAmount = table.Column<double>(type: "float", nullable: true),
                    TotalPaidAmount = table.Column<double>(type: "float", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DrawerId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CASHTRANSACTION", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CATEGORY",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrintingName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SymanticType = table.Column<int>(type: "int", nullable: true),
                    ParentCategoryId = table.Column<long>(type: "bigint", nullable: true),
                    PaymentSchemeId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DiscountAllowed = table.Column<bool>(type: "bit", nullable: false),
                    DepartmentLimitToDepartment = table.Column<bool>(type: "bit", nullable: false),
                    DTYPE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DTYPE1 = table.Column<string>(type: "nvarchar(34)", maxLength: 34, nullable: false),
                    SpecialityId = table.Column<long>(type: "bigint", nullable: true),
                    StaffId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CATEGORY", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CATEGORY_CATEGORY_ParentCategoryId",
                        column: x => x.ParentCategoryId,
                        principalTable: "CATEGORY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CATEGORYITEM",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ItemId = table.Column<long>(type: "bigint", nullable: true),
                    CategoryId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CATEGORYITEM", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CATEGORYITEM_CATEGORY_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "CATEGORY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "CONFIGOPTION",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OptionValue = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OptionScope = table.Column<int>(type: "int", nullable: true),
                    OptionType = table.Column<int>(type: "int", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CONFIGOPTION", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DEPARTMENT",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrintingName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentPhone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentFax = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentEmail = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DepartmentType = table.Column<int>(type: "int", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    SiteId = table.Column<long>(type: "bigint", nullable: true),
                    LabInstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    SuperDepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    SampleDepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    LabDepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DTYPE = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DEPARTMENT", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DEPARTMENT_DEPARTMENT_LabDepartmentId",
                        column: x => x.LabDepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DEPARTMENT_DEPARTMENT_SampleDepartmentId",
                        column: x => x.SampleDepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DEPARTMENT_DEPARTMENT_SuperDepartmentId",
                        column: x => x.SuperDepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DESIGNATION",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DESIGNATION", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DRAWER",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartingCash = table.Column<double>(type: "float", nullable: false),
                    CurrentCash = table.Column<double>(type: "float", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Closed = table.Column<bool>(type: "bit", nullable: false),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CashierId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DRAWER", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DRAWER_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ENCOUNTERCREDITCOMPANY",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientEncounterId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreditLimit = table.Column<double>(type: "float", nullable: false),
                    Descreption = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PolicyNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferanceNo = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ENCOUNTERCREDITCOMPANY", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FAMILY",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MembershipCardNo = table.Column<long>(type: "bigint", nullable: true),
                    PhoneNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MembershipSchemeId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedInstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedDepartmentId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_FAMILY", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FAMILY_DEPARTMENT_CreatedDepartmentId",
                        column: x => x.CreatedDepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FAMILYMEMBER",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FamilyId = table.Column<long>(type: "bigint", nullable: true),
                    PatientId = table.Column<long>(type: "bigint", nullable: true),
                    RelationToChhId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_FAMILYMEMBER", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FAMILYMEMBER_CATEGORY_RelationToChhId",
                        column: x => x.RelationToChhId,
                        principalTable: "CATEGORY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FAMILYMEMBER_FAMILY_FamilyId",
                        column: x => x.FamilyId,
                        principalTable: "FAMILY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FEE",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EndorserAddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FeeType = table.Column<int>(type: "int", nullable: true),
                    FeeValue = table.Column<double>(type: "float", nullable: false),
                    FeeGrossValue = table.Column<double>(type: "float", nullable: true),
                    FeeDiscount = table.Column<double>(type: "float", nullable: false),
                    FeeVat = table.Column<double>(type: "float", nullable: false),
                    FeeMargin = table.Column<double>(type: "float", nullable: false),
                    FeeAdjusted = table.Column<double>(type: "float", nullable: false),
                    VatPercentage = table.Column<double>(type: "float", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    SpecialityId = table.Column<long>(type: "bigint", nullable: true),
                    StaffId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DTYPE = table.Column<string>(type: "nvarchar(21)", maxLength: 21, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FEE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FEE_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FEECHANGE",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ValidFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FeeId = table.Column<long>(type: "bigint", nullable: true),
                    Done = table.Column<bool>(type: "bit", nullable: false),
                    DoneAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FeeChangeType = table.Column<int>(type: "int", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FEECHANGE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FEECHANGE_FEE_FeeId",
                        column: x => x.FeeId,
                        principalTable: "FEE",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FEEVALUE",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    CategoryId = table.Column<long>(type: "bigint", nullable: true),
                    TotalValueForLocals = table.Column<double>(type: "float", nullable: true),
                    TotalValueForForeigners = table.Column<double>(type: "float", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FEEVALUE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FEEVALUE_CATEGORY_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "CATEGORY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FEEVALUE_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "FORM",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EditorId = table.Column<long>(type: "bigint", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FORM", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "GRADE",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GRADE", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "INSTITUTION",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Fax = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Website = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrintingName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RegNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PharmacyCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SiName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstitutionType = table.Column<int>(type: "int", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    BankBranchId = table.Column<long>(type: "bigint", nullable: true),
                    ParentInstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    LabInstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    ContactPersonId = table.Column<long>(type: "bigint", nullable: true),
                    LabDepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    FeeListTypeId = table.Column<long>(type: "bigint", nullable: true),
                    RouteId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_INSTITUTION", x => x.Id);
                    table.ForeignKey(
                        name: "FK_INSTITUTION_CATEGORY_FeeListTypeId",
                        column: x => x.FeeListTypeId,
                        principalTable: "CATEGORY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_INSTITUTION_DEPARTMENT_LabDepartmentId",
                        column: x => x.LabDepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_INSTITUTION_INSTITUTION_BankBranchId",
                        column: x => x.BankBranchId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_INSTITUTION_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_INSTITUTION_INSTITUTION_LabInstitutionId",
                        column: x => x.LabInstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_INSTITUTION_INSTITUTION_ParentInstitutionId",
                        column: x => x.ParentInstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ISSUERATEMARGINS",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    FromDepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    ToDepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    FromInstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    ToInstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    RateForPharmaceuticals = table.Column<double>(type: "float", nullable: false),
                    RateForConsumables = table.Column<double>(type: "float", nullable: false),
                    RateForInventory = table.Column<double>(type: "float", nullable: false),
                    ShowRates = table.Column<bool>(type: "bit", nullable: false),
                    AtPurchaseRate = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ISSUERATEMARGINS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ISSUERATEMARGINS_DEPARTMENT_FromDepartmentId",
                        column: x => x.FromDepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ISSUERATEMARGINS_DEPARTMENT_ToDepartmentId",
                        column: x => x.ToDepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ISSUERATEMARGINS_INSTITUTION_FromInstitutionId",
                        column: x => x.FromInstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ISSUERATEMARGINS_INSTITUTION_ToInstitutionId",
                        column: x => x.ToInstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ITEM",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeSystem = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastBarcode = table.Column<long>(type: "bigint", nullable: true),
                    PrintName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShortName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Descreption = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrderNo = table.Column<int>(type: "int", nullable: false),
                    CanSechduleForOtherDays = table.Column<bool>(type: "bit", nullable: false),
                    ItemId = table.Column<long>(type: "bigint", nullable: true),
                    IsMasterItem = table.Column<bool>(type: "bit", nullable: false),
                    HasReportFormat = table.Column<bool>(type: "bit", nullable: false),
                    NumberOfDaysToMarkAsShortExpiary = table.Column<int>(type: "int", nullable: false),
                    Total = table.Column<double>(type: "float", nullable: true),
                    TotalForForeigner = table.Column<double>(type: "float", nullable: true),
                    DiscountAllowed = table.Column<bool>(type: "bit", nullable: true),
                    CategoryId = table.Column<long>(type: "bigint", nullable: true),
                    FinancialCategoryId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    SpecialityId = table.Column<long>(type: "bigint", nullable: true),
                    StaffId = table.Column<long>(type: "bigint", nullable: true),
                    ForInstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    ForDepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    BilledAsId = table.Column<long>(type: "bigint", nullable: true),
                    ReportedAsId = table.Column<long>(type: "bigint", nullable: true),
                    MasterItemReferenceId = table.Column<long>(type: "bigint", nullable: true),
                    MeasurementUnitId = table.Column<long>(type: "bigint", nullable: true),
                    WorksheetId = table.Column<long>(type: "bigint", nullable: true),
                    ReportFormatId = table.Column<long>(type: "bigint", nullable: true),
                    ManufacturerId = table.Column<long>(type: "bigint", nullable: true),
                    ImporterId = table.Column<long>(type: "bigint", nullable: true),
                    ParentItemId = table.Column<long>(type: "bigint", nullable: true),
                    VmpId = table.Column<long>(type: "bigint", nullable: true),
                    MachineId = table.Column<long>(type: "bigint", nullable: true),
                    StrengthUnitId = table.Column<long>(type: "bigint", nullable: true),
                    BaseUnitId = table.Column<long>(type: "bigint", nullable: true),
                    IssueUnitId = table.Column<long>(type: "bigint", nullable: true),
                    ForBillType = table.Column<int>(type: "int", nullable: true),
                    ItemType = table.Column<int>(type: "int", nullable: true),
                    InwardChargeType = table.Column<int>(type: "int", nullable: true),
                    SymanticType = table.Column<int>(type: "int", nullable: true),
                    DepartmentType = table.Column<int>(type: "int", nullable: true),
                    SessionNumberType = table.Column<int>(type: "int", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: true),
                    ItemBarcodeGenerationStrategy = table.Column<int>(type: "int", nullable: true),
                    UserChangable = table.Column<bool>(type: "bit", nullable: false),
                    DblValue = table.Column<double>(type: "float", nullable: false),
                    PriceByBatch = table.Column<bool>(type: "bit", nullable: false),
                    Billable = table.Column<bool>(type: "bit", nullable: false),
                    Vatable = table.Column<bool>(type: "bit", nullable: false),
                    Formatable = table.Column<bool>(type: "bit", nullable: false),
                    PatientNotRequired = table.Column<bool>(type: "bit", nullable: false),
                    ChargesVisibleForInward = table.Column<bool>(type: "bit", nullable: false),
                    RequestForQuentity = table.Column<bool>(type: "bit", nullable: false),
                    MarginNotAllowed = table.Column<bool>(type: "bit", nullable: false),
                    PrintSessionNumber = table.Column<bool>(type: "bit", nullable: false),
                    Inactive = table.Column<bool>(type: "bit", nullable: false),
                    VatPercentage = table.Column<double>(type: "float", nullable: false),
                    EffectiveFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EffectiveTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ScanFee = table.Column<bool>(type: "bit", nullable: false),
                    ProfitMargin = table.Column<double>(type: "float", nullable: false),
                    CreditNumbers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CashNumbers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgencyNumbers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReserveName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReserveNumbers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxTableRows = table.Column<int>(type: "int", nullable: false),
                    HasMoreThanOneComponant = table.Column<bool>(type: "bit", nullable: false),
                    IssueUnitsPerPackUnit = table.Column<double>(type: "float", nullable: true),
                    BaseUnitsPerIssueUnit = table.Column<double>(type: "float", nullable: true),
                    PrintFeesForBills = table.Column<bool>(type: "bit", nullable: false),
                    CanRemoveItemfromPackage = table.Column<bool>(type: "bit", nullable: false),
                    ForGender = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DTYPE = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DTYPE1 = table.Column<string>(type: "nvarchar(21)", maxLength: 21, nullable: false),
                    SessionWeekday = table.Column<int>(type: "int", nullable: true),
                    SessionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SessionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SessionAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartingNo = table.Column<int>(type: "int", nullable: true),
                    NumberIncrement = table.Column<int>(type: "int", nullable: true),
                    MaxNo = table.Column<int>(type: "int", nullable: true),
                    ContinueNumbers = table.Column<bool>(type: "bit", nullable: true),
                    AfterSessionId = table.Column<long>(type: "bigint", nullable: true),
                    SessionNumberGeneratorId = table.Column<long>(type: "bigint", nullable: true),
                    BeforeSessionId = table.Column<long>(type: "bigint", nullable: true),
                    StartingTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndingTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    NumberOfDaysForAutomaticInstanceCreation = table.Column<int>(type: "int", nullable: true),
                    Refundable = table.Column<bool>(type: "bit", nullable: true),
                    DisplayCount = table.Column<int>(type: "int", nullable: true),
                    DisplayPercent = table.Column<double>(type: "float", nullable: true),
                    Duration = table.Column<double>(type: "float", nullable: true),
                    RoomNo = table.Column<int>(type: "int", nullable: true),
                    Deactivated = table.Column<bool>(type: "bit", nullable: true),
                    DeactivateComment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OriginatingSessionId = table.Column<long>(type: "bigint", nullable: true),
                    RecervedNumbers = table.Column<int>(type: "int", nullable: true),
                    PaidAppointmentsOnly = table.Column<bool>(type: "bit", nullable: true),
                    ExcludeFromPatientPortal = table.Column<bool>(type: "bit", nullable: true),
                    CanChangePatient = table.Column<bool>(type: "bit", nullable: true),
                    SessionStartingNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Activities = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Actions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NotificationRoles = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DataEntryForms = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ITEM", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ITEM_CATEGORY_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "CATEGORY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ITEM_CATEGORY_FinancialCategoryId",
                        column: x => x.FinancialCategoryId,
                        principalTable: "CATEGORY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ITEM_CATEGORY_ReportFormatId",
                        column: x => x.ReportFormatId,
                        principalTable: "CATEGORY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ITEM_CATEGORY_SessionNumberGeneratorId",
                        column: x => x.SessionNumberGeneratorId,
                        principalTable: "CATEGORY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ITEM_CATEGORY_WorksheetId",
                        column: x => x.WorksheetId,
                        principalTable: "CATEGORY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ITEM_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ITEM_DEPARTMENT_ForDepartmentId",
                        column: x => x.ForDepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ITEM_INSTITUTION_ForInstitutionId",
                        column: x => x.ForInstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ITEM_INSTITUTION_ImporterId",
                        column: x => x.ImporterId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ITEM_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ITEM_INSTITUTION_ManufacturerId",
                        column: x => x.ManufacturerId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ITEM_ITEM_AfterSessionId",
                        column: x => x.AfterSessionId,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ITEM_ITEM_BeforeSessionId",
                        column: x => x.BeforeSessionId,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ITEM_ITEM_BilledAsId",
                        column: x => x.BilledAsId,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ITEM_ITEM_MasterItemReferenceId",
                        column: x => x.MasterItemReferenceId,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ITEM_ITEM_OriginatingSessionId",
                        column: x => x.OriginatingSessionId,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ITEM_ITEM_ParentItemId",
                        column: x => x.ParentItemId,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ITEM_ITEM_ReportedAsId",
                        column: x => x.ReportedAsId,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MAPPING",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Item1Id = table.Column<long>(type: "bigint", nullable: true),
                    Item2Id = table.Column<long>(type: "bigint", nullable: true),
                    Descreption = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MAPPING", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MAPPING_ITEM_Item1Id",
                        column: x => x.Item1Id,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MAPPING_ITEM_Item2Id",
                        column: x => x.Item2Id,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ITEMBATCH",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BatchNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateOfManufacture = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateOfExpiry = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ItemId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_ITEMBATCH", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ITEMBATCH_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ITEMBATCH_ITEM_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ITEMMAPPING",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ItemId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ITEMMAPPING", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ITEMMAPPING_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ITEMMAPPING_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ITEMMAPPING_ITEM_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ITEMSCATEGORIES",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ItemId = table.Column<long>(type: "bigint", nullable: true),
                    CategoryId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ITEMSCATEGORIES", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ITEMSCATEGORIES_CATEGORY_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "CATEGORY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ITEMSCATEGORIES_ITEM_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LOGINS",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Ipaddress = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LoginTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LogoutTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LoggedOut = table.Column<bool>(type: "bit", nullable: false),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    WebUserId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LOGINS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LOGINS_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LOGINS_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MACHINE",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MACHINE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MACHINE_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MACHINE_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MEASUREMENTUNIT",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
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
                    table.PrimaryKey("PK_MEASUREMENTUNIT", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MEDICALPACKAGEITEM",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackegeId = table.Column<long>(type: "bigint", nullable: true),
                    ItemId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MEDICALPACKAGEITEM", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MEDICALPACKAGEITEM_ITEM_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MEDICALPACKAGEITEM_ITEM_PackegeId",
                        column: x => x.PackegeId,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MEMBERSHIPSCHEME",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MEMBERSHIPSCHEME", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MEMBERSHIPSCHEME_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "NOTIFICATION",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BillId = table.Column<long>(type: "bigint", nullable: true),
                    Completed = table.Column<bool>(type: "bit", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TriggerType = table.Column<int>(type: "int", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NOTIFICATION", x => x.Id);
                    table.ForeignKey(
                        name: "FK_NOTIFICATION_BILL_BillId",
                        column: x => x.BillId,
                        principalTable: "BILL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PACKAGEITEM",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PackegeId = table.Column<long>(type: "bigint", nullable: true),
                    ItemId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PACKAGEITEM", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PACKAGEITEM_ITEM_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PACKAGEITEM_ITEM_PackegeId",
                        column: x => x.PackegeId,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PATIENT",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientPhoneNumber = table.Column<long>(type: "bigint", nullable: true),
                    PatientMobileNumber = table.Column<long>(type: "bigint", nullable: true),
                    PersonId = table.Column<long>(type: "bigint", nullable: true),
                    PinNo = table.Column<int>(type: "int", nullable: true),
                    HinNo = table.Column<int>(type: "int", nullable: true),
                    CreatedInstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BaImage = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Phn = table.Column<string>(type: "nvarchar(15)", maxLength: 15, nullable: true),
                    HasAnAccount = table.Column<bool>(type: "bit", nullable: true),
                    RunningBalance = table.Column<double>(type: "float", nullable: true),
                    CreditLimit = table.Column<double>(type: "float", nullable: true),
                    PatientId = table.Column<long>(type: "bigint", nullable: true),
                    CardIssues = table.Column<bool>(type: "bit", nullable: true),
                    CardIssuedDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PATIENT", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PATIENT_INSTITUTION_CreatedInstitutionId",
                        column: x => x.CreatedInstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PATIENTDEPOSIT",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    Balance = table.Column<double>(type: "float", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PATIENTDEPOSIT", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PATIENTDEPOSIT_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PATIENTDEPOSIT_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PATIENTDEPOSIT_PATIENT_PatientId",
                        column: x => x.PatientId,
                        principalTable: "PATIENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PATIENTDEPOSITHISTORY",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BillItemId = table.Column<long>(type: "bigint", nullable: true),
                    BillId = table.Column<long>(type: "bigint", nullable: true),
                    BillSessionId = table.Column<long>(type: "bigint", nullable: true),
                    PatientDepositId = table.Column<long>(type: "bigint", nullable: true),
                    BalanceBeforeTransaction = table.Column<double>(type: "float", nullable: false),
                    BalanceAfterTransaction = table.Column<double>(type: "float", nullable: false),
                    TransactionValue = table.Column<double>(type: "float", nullable: false),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    HistoryType = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferenceNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Comment = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PATIENTDEPOSITHISTORY", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PATIENTDEPOSITHISTORY_BILLITEM_BillItemId",
                        column: x => x.BillItemId,
                        principalTable: "BILLITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PATIENTDEPOSITHISTORY_BILLSESSION_BillSessionId",
                        column: x => x.BillSessionId,
                        principalTable: "BILLSESSION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PATIENTDEPOSITHISTORY_BILL_BillId",
                        column: x => x.BillId,
                        principalTable: "BILL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PATIENTDEPOSITHISTORY_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PATIENTDEPOSITHISTORY_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PATIENTDEPOSITHISTORY_PATIENTDEPOSIT_PatientDepositId",
                        column: x => x.PatientDepositId,
                        principalTable: "PATIENTDEPOSIT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PATIENTENCOUNTER",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EncounterType = table.Column<int>(type: "int", nullable: true),
                    EncounterComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EncounterDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EncounterTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateOfAdmission = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DateOfDischarge = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PatientId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    ReferringDepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    ReferringInstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    ReferringStaffId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DTYPE = table.Column<string>(type: "nvarchar(21)", maxLength: 21, nullable: false),
                    AppointmentDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AppointmentTimeFrom = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AppointmentTimeTo = table.Column<DateTime>(type: "datetime2", nullable: true),
                    BillId = table.Column<long>(type: "bigint", nullable: true),
                    PatientEncounterId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PATIENTENCOUNTER", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PATIENTENCOUNTER_BILL_BillId",
                        column: x => x.BillId,
                        principalTable: "BILL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PATIENTENCOUNTER_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PATIENTENCOUNTER_DEPARTMENT_ReferringDepartmentId",
                        column: x => x.ReferringDepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PATIENTENCOUNTER_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PATIENTENCOUNTER_INSTITUTION_ReferringInstitutionId",
                        column: x => x.ReferringInstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PATIENTENCOUNTER_PATIENTENCOUNTER_PatientEncounterId",
                        column: x => x.PatientEncounterId,
                        principalTable: "PATIENTENCOUNTER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PATIENTENCOUNTER_PATIENT_PatientId",
                        column: x => x.PatientId,
                        principalTable: "PATIENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PATIENTFLAG",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientId = table.Column<long>(type: "bigint", nullable: true),
                    PatientEncounterId = table.Column<long>(type: "bigint", nullable: true),
                    InvestigationFlagId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PATIENTFLAG", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PATIENTFLAG_PATIENTENCOUNTER_PatientEncounterId",
                        column: x => x.PatientEncounterId,
                        principalTable: "PATIENTENCOUNTER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PATIENTFLAG_PATIENT_PatientId",
                        column: x => x.PatientId,
                        principalTable: "PATIENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PATIENTITEM",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PatientId = table.Column<long>(type: "bigint", nullable: true),
                    PatientEncounterId = table.Column<long>(type: "bigint", nullable: true),
                    BillId = table.Column<long>(type: "bigint", nullable: true),
                    BillItemId = table.Column<long>(type: "bigint", nullable: true),
                    PackegeId = table.Column<long>(type: "bigint", nullable: true),
                    ItemId = table.Column<long>(type: "bigint", nullable: true),
                    FromTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ToTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ServiceValue = table.Column<double>(type: "float", nullable: true),
                    Discount = table.Column<double>(type: "float", nullable: false),
                    AdjustedValue = table.Column<double>(type: "float", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PATIENTITEM", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PATIENTITEM_BILLITEM_BillItemId",
                        column: x => x.BillItemId,
                        principalTable: "BILLITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PATIENTITEM_BILL_BillId",
                        column: x => x.BillId,
                        principalTable: "BILL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PATIENTITEM_ITEM_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PATIENTITEM_ITEM_PackegeId",
                        column: x => x.PackegeId,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PATIENTITEM_PATIENTENCOUNTER_PatientEncounterId",
                        column: x => x.PatientEncounterId,
                        principalTable: "PATIENTENCOUNTER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PATIENTITEM_PATIENT_PatientId",
                        column: x => x.PatientId,
                        principalTable: "PATIENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PATIENTROOM",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FromTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ToTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Discharged = table.Column<bool>(type: "bit", nullable: false),
                    DischargedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RoomId = table.Column<long>(type: "bigint", nullable: true),
                    PatientEncounterId = table.Column<long>(type: "bigint", nullable: true),
                    AdmissionId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PATIENTROOM", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PATIENTROOM_ADMISSION_AdmissionId",
                        column: x => x.AdmissionId,
                        principalTable: "ADMISSION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PATIENTROOM_PATIENTENCOUNTER_PatientEncounterId",
                        column: x => x.PatientEncounterId,
                        principalTable: "PATIENTENCOUNTER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PAYMENT",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WrittenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ToRealizeAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PaymentMethod = table.Column<int>(type: "int", nullable: true),
                    Realized = table.Column<bool>(type: "bit", nullable: false),
                    RealizedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RealizeComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChequeRefNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChequeDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreditCardRefNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PaidValue = table.Column<double>(type: "float", nullable: false),
                    CreditDurationInDays = table.Column<int>(type: "int", nullable: false),
                    CurrencyDenominationsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReferenceNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CashbookEntryStated = table.Column<bool>(type: "bit", nullable: false),
                    CashbookEntryCompleted = table.Column<bool>(type: "bit", nullable: false),
                    BillId = table.Column<long>(type: "bigint", nullable: true),
                    BankId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    RealizerId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    HandoverCreatedBillId = table.Column<long>(type: "bigint", nullable: true),
                    HandoverAcceptBillId = table.Column<long>(type: "bigint", nullable: true),
                    CashbookEntryId = table.Column<long>(type: "bigint", nullable: true),
                    CashbookId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PAYMENT", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PAYMENT_BILL_BillId",
                        column: x => x.BillId,
                        principalTable: "BILL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PAYMENT_BILL_HandoverAcceptBillId",
                        column: x => x.HandoverAcceptBillId,
                        principalTable: "BILL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PAYMENT_BILL_HandoverCreatedBillId",
                        column: x => x.HandoverCreatedBillId,
                        principalTable: "BILL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PAYMENT_CASHBOOKENTRY_CashbookEntryId",
                        column: x => x.CashbookEntryId,
                        principalTable: "CASHBOOKENTRY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PAYMENT_CASHBOOK_CashbookId",
                        column: x => x.CashbookId,
                        principalTable: "CASHBOOK",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PAYMENT_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PAYMENT_INSTITUTION_BankId",
                        column: x => x.BankId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PAYMENT_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PAYMENTGATEWAYTRANSACTION",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    OrderId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrderAmount = table.Column<double>(type: "float", nullable: true),
                    SessionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrderStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TransactionId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SessionInstanceId = table.Column<long>(type: "bigint", nullable: true),
                    BillId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PAYMENTGATEWAYTRANSACTION", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PAYMENTGATEWAYTRANSACTION_BILL_BillId",
                        column: x => x.BillId,
                        principalTable: "BILL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PAYMENTGATEWAYTRANSACTION_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PAYMENTGATEWAYTRANSACTION_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PAYMENTSCHEME",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PAYMENTSCHEME", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PAYMENTSCHEME_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PAYMENTSCHEME_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PERSON",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Nic = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Fax = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Website = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Mobile = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TNAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SNAME = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Initials = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Surname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ZoneCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Dob = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AreaId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    Title = table.Column<int>(type: "int", nullable: true),
                    Sex = table.Column<int>(type: "int", nullable: true),
                    Foreigner = table.Column<bool>(type: "bit", nullable: false),
                    CivilStatusId = table.Column<long>(type: "bigint", nullable: true),
                    RaceId = table.Column<long>(type: "bigint", nullable: true),
                    BloodGroupId = table.Column<long>(type: "bigint", nullable: true),
                    OccupationId = table.Column<long>(type: "bigint", nullable: true),
                    ReligionId = table.Column<long>(type: "bigint", nullable: true),
                    MembershipSchemeId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PERSON", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PERSON_AREA_AreaId",
                        column: x => x.AreaId,
                        principalTable: "AREA",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PERSON_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PERSON_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PERSON_ITEM_BloodGroupId",
                        column: x => x.BloodGroupId,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PERSON_ITEM_CivilStatusId",
                        column: x => x.CivilStatusId,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PERSON_ITEM_OccupationId",
                        column: x => x.OccupationId,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PERSON_ITEM_RaceId",
                        column: x => x.RaceId,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PERSON_ITEM_ReligionId",
                        column: x => x.ReligionId,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PERSON_MEMBERSHIPSCHEME_MembershipSchemeId",
                        column: x => x.MembershipSchemeId,
                        principalTable: "MEMBERSHIPSCHEME",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PERSONINSTITUTION",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    PersonId = table.Column<long>(type: "bigint", nullable: true),
                    StaffId = table.Column<long>(type: "bigint", nullable: true),
                    Type = table.Column<int>(type: "int", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PERSONINSTITUTION", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PERSONINSTITUTION_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PERSONINSTITUTION_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PERSONINSTITUTION_PERSON_PersonId",
                        column: x => x.PersonId,
                        principalTable: "PERSON",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PHARMACEUTICALBILLITEM",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Qty = table.Column<double>(type: "float", nullable: true),
                    FreeQty = table.Column<double>(type: "float", nullable: true),
                    SellingPrice = table.Column<double>(type: "float", nullable: true),
                    PurchasePrice = table.Column<double>(type: "float", nullable: true),
                    BillItemId = table.Column<long>(type: "bigint", nullable: true),
                    ItemBatchId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PHARMACEUTICALBILLITEM", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PHARMACEUTICALBILLITEM_BILLITEM_BillItemId",
                        column: x => x.BillItemId,
                        principalTable: "BILLITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PHARMACEUTICALBILLITEM_ITEMBATCH_ItemBatchId",
                        column: x => x.ItemBatchId,
                        principalTable: "ITEMBATCH",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PRICEMATRIX",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Fee = table.Column<double>(type: "float", nullable: true),
                    FromDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ToDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ItemId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PRICEMATRIX", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PRICEMATRIX_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PRICEMATRIX_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PRICEMATRIX_ITEM_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "REPORTTEMPLATE",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReportTemplateType = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OrderNo = table.Column<int>(type: "int", nullable: false),
                    Keywords = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CategoryId = table.Column<long>(type: "bigint", nullable: true),
                    Filters = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BillTypes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Columns = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Totals = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_REPORTTEMPLATE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_REPORTTEMPLATE_CATEGORY_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "CATEGORY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ROOM",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RoomCategoryId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    BuildingId = table.Column<long>(type: "bigint", nullable: true),
                    FloorId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROOM", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ROOM_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ROOM_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ROOMCATEGORY",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROOMCATEGORY", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ROOMCATEGORY_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ROSTER",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROSTER", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ROSTER_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ROUTES",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ROUTES", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ROUTES_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SERVICESESESSIONINSTANCE",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Total = table.Column<double>(type: "float", nullable: true),
                    TotalForForeigner = table.Column<double>(type: "float", nullable: true),
                    DiscountAllowed = table.Column<bool>(type: "bit", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    SpecialityId = table.Column<long>(type: "bigint", nullable: true),
                    StaffId = table.Column<long>(type: "bigint", nullable: true),
                    ForInstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    ForDepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    ForBillType = table.Column<int>(type: "int", nullable: true),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrintName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShortName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FullName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InwardChargeType = table.Column<int>(type: "int", nullable: true),
                    DblValue = table.Column<double>(type: "float", nullable: false),
                    SessionNumberType = table.Column<int>(type: "int", nullable: true),
                    PriceByBatch = table.Column<bool>(type: "bit", nullable: false),
                    Billable = table.Column<bool>(type: "bit", nullable: false),
                    Vatable = table.Column<bool>(type: "bit", nullable: false),
                    Formatable = table.Column<bool>(type: "bit", nullable: false),
                    PatientNotRequired = table.Column<bool>(type: "bit", nullable: false),
                    ChargesVisibleForInward = table.Column<bool>(type: "bit", nullable: false),
                    RequestForQuentity = table.Column<bool>(type: "bit", nullable: false),
                    MarginNotAllowed = table.Column<bool>(type: "bit", nullable: false),
                    VatPercentage = table.Column<double>(type: "float", nullable: false),
                    ScanFee = table.Column<bool>(type: "bit", nullable: false),
                    ProfitMargin = table.Column<double>(type: "float", nullable: false),
                    CreditNumbers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CashNumbers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AgencyNumbers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReserveName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReserveNumbers = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaxTableRows = table.Column<int>(type: "int", nullable: false),
                    SessionWeekday = table.Column<int>(type: "int", nullable: true),
                    SessionDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SessionTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SessionAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StartingNo = table.Column<int>(type: "int", nullable: false),
                    NumberIncrement = table.Column<int>(type: "int", nullable: false),
                    MaxNo = table.Column<int>(type: "int", nullable: false),
                    ContinueNumbers = table.Column<bool>(type: "bit", nullable: false),
                    SessionNumberGeneratorId = table.Column<long>(type: "bigint", nullable: true),
                    StartingTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EndingTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Refundable = table.Column<bool>(type: "bit", nullable: false),
                    DisplayCount = table.Column<int>(type: "int", nullable: false),
                    DisplayPercent = table.Column<double>(type: "float", nullable: false),
                    Duration = table.Column<double>(type: "float", nullable: false),
                    RoomNo = table.Column<int>(type: "int", nullable: false),
                    Deactivated = table.Column<bool>(type: "bit", nullable: false),
                    DeactivateComment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OriginatingSessionId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SERVICESESESSIONINSTANCE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SERVICESESESSIONINSTANCE_CATEGORY_SessionNumberGeneratorId",
                        column: x => x.SessionNumberGeneratorId,
                        principalTable: "CATEGORY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SERVICESESESSIONINSTANCE_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SERVICESESESSIONINSTANCE_DEPARTMENT_ForDepartmentId",
                        column: x => x.ForDepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SERVICESESESSIONINSTANCE_INSTITUTION_ForInstitutionId",
                        column: x => x.ForInstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SERVICESESESSIONINSTANCE_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SERVICESESESSIONINSTANCE_ITEM_OriginatingSessionId",
                        column: x => x.OriginatingSessionId,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SHIFT",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StartTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EndTime = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SHIFT", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SHIFT_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SMS",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientInvestigationId = table.Column<long>(type: "bigint", nullable: true),
                    BillId = table.Column<long>(type: "bigint", nullable: true),
                    PatientReportId = table.Column<long>(type: "bigint", nullable: true),
                    SendingMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceivedMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SendingUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Otp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReceipientNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmsType = table.Column<int>(type: "int", nullable: true),
                    SentSuccessfully = table.Column<bool>(type: "bit", nullable: true),
                    Pending = table.Column<bool>(type: "bit", nullable: false),
                    SentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SMS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SMS_BILL_BillId",
                        column: x => x.BillId,
                        principalTable: "BILL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SMS_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SMS_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SPECIALITY",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DTYPE = table.Column<string>(type: "nvarchar(21)", maxLength: 21, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SPECIALITY", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SPECIALITY_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "STAFF",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StaffNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CodeNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SpecialityCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CpmdcCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PersonId = table.Column<long>(type: "bigint", nullable: true),
                    DesignationId = table.Column<long>(type: "bigint", nullable: true),
                    GradeId = table.Column<long>(type: "bigint", nullable: true),
                    StaffCategoryId = table.Column<long>(type: "bigint", nullable: true),
                    RosterId = table.Column<long>(type: "bigint", nullable: true),
                    SpecialityId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    WorkingDepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DTYPE = table.Column<string>(type: "nvarchar(13)", maxLength: 13, nullable: false),
                    SltRegNo = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PmcNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CarryDefaultSMS = table.Column<bool>(type: "bit", nullable: true),
                    CarryOwnStaff = table.Column<bool>(type: "bit", nullable: true),
                    ChannelingDestinationInstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    ChannelingDestinationDepartmentId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STAFF", x => x.Id);
                    table.ForeignKey(
                        name: "FK_STAFF_CATEGORY_DesignationId",
                        column: x => x.DesignationId,
                        principalTable: "CATEGORY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_STAFF_CATEGORY_GradeId",
                        column: x => x.GradeId,
                        principalTable: "CATEGORY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_STAFF_CATEGORY_StaffCategoryId",
                        column: x => x.StaffCategoryId,
                        principalTable: "CATEGORY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_STAFF_DEPARTMENT_ChannelingDestinationDepartmentId",
                        column: x => x.ChannelingDestinationDepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_STAFF_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_STAFF_DEPARTMENT_WorkingDepartmentId",
                        column: x => x.WorkingDepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_STAFF_INSTITUTION_ChannelingDestinationInstitutionId",
                        column: x => x.ChannelingDestinationInstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_STAFF_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_STAFF_PERSON_PersonId",
                        column: x => x.PersonId,
                        principalTable: "PERSON",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_STAFF_ROSTER_RosterId",
                        column: x => x.RosterId,
                        principalTable: "ROSTER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_STAFF_SPECIALITY_SpecialityId",
                        column: x => x.SpecialityId,
                        principalTable: "SPECIALITY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "STAFFCATEGORY",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STAFFCATEGORY", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "STAFFSALARY",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GrossSalary = table.Column<double>(type: "float", nullable: true),
                    NetSalary = table.Column<double>(type: "float", nullable: true),
                    EpfEmployee = table.Column<double>(type: "float", nullable: true),
                    EpfEmployer = table.Column<double>(type: "float", nullable: true),
                    Etf = table.Column<double>(type: "float", nullable: true),
                    Payee = table.Column<double>(type: "float", nullable: true),
                    BasicSalary = table.Column<double>(type: "float", nullable: true),
                    Allowance = table.Column<double>(type: "float", nullable: true),
                    SalaryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    StaffId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STAFFSALARY", x => x.Id);
                    table.ForeignKey(
                        name: "FK_STAFFSALARY_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_STAFFSALARY_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_STAFFSALARY_STAFF_StaffId",
                        column: x => x.StaffId,
                        principalTable: "STAFF",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "STOCK",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StockQty = table.Column<double>(type: "float", nullable: true),
                    StockFreeQty = table.Column<double>(type: "float", nullable: true),
                    QuentityInUnit = table.Column<double>(type: "float", nullable: true),
                    QuentityInPack = table.Column<double>(type: "float", nullable: true),
                    LastPurchaseUnitPrice = table.Column<double>(type: "float", nullable: true),
                    LastPurchaseFreeQty = table.Column<double>(type: "float", nullable: true),
                    LastPurchaseDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ItemBatchId = table.Column<long>(type: "bigint", nullable: true),
                    ItemId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_STOCK", x => x.Id);
                    table.ForeignKey(
                        name: "FK_STOCK_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_STOCK_ITEMBATCH_ItemBatchId",
                        column: x => x.ItemBatchId,
                        principalTable: "ITEMBATCH",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_STOCK_ITEM_ItemId",
                        column: x => x.ItemId,
                        principalTable: "ITEM",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TOKEN",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TokenId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TokenNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TokenDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TokenAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    TokenType = table.Column<int>(type: "int", nullable: true),
                    CateroryId = table.Column<long>(type: "bigint", nullable: true),
                    BillId = table.Column<long>(type: "bigint", nullable: true),
                    IssuedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Called = table.Column<bool>(type: "bit", nullable: false),
                    CalledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    InProgress = table.Column<bool>(type: "bit", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Completed = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PatientId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    FromDepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    ToDepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    FromInstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    ToInstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    ServiceCounterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedById = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetiredById = table.Column<long>(type: "bigint", nullable: true),
                    StaffId = table.Column<long>(type: "bigint", nullable: true),
                    FromStaffId = table.Column<long>(type: "bigint", nullable: true),
                    ToStaffId = table.Column<long>(type: "bigint", nullable: true),
                    PatientEncounterId = table.Column<long>(type: "bigint", nullable: true),
                    CounterId = table.Column<long>(type: "bigint", nullable: true),
                    DoctorId = table.Column<long>(type: "bigint", nullable: true),
                    ReferaTokenId = table.Column<long>(type: "bigint", nullable: true),
                    RestartTokenServices = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TOKEN", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TOKEN_BILL_BillId",
                        column: x => x.BillId,
                        principalTable: "BILL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TOKEN_CATEGORY_CateroryId",
                        column: x => x.CateroryId,
                        principalTable: "CATEGORY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TOKEN_DEPARTMENT_CounterId",
                        column: x => x.CounterId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TOKEN_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TOKEN_DEPARTMENT_FromDepartmentId",
                        column: x => x.FromDepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TOKEN_DEPARTMENT_ServiceCounterId",
                        column: x => x.ServiceCounterId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TOKEN_DEPARTMENT_ToDepartmentId",
                        column: x => x.ToDepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TOKEN_INSTITUTION_FromInstitutionId",
                        column: x => x.FromInstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TOKEN_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TOKEN_INSTITUTION_ToInstitutionId",
                        column: x => x.ToInstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TOKEN_PATIENTENCOUNTER_PatientEncounterId",
                        column: x => x.PatientEncounterId,
                        principalTable: "PATIENTENCOUNTER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TOKEN_PATIENT_PatientId",
                        column: x => x.PatientId,
                        principalTable: "PATIENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TOKEN_STAFF_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "STAFF",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TOKEN_STAFF_FromStaffId",
                        column: x => x.FromStaffId,
                        principalTable: "STAFF",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TOKEN_STAFF_StaffId",
                        column: x => x.StaffId,
                        principalTable: "STAFF",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TOKEN_STAFF_ToStaffId",
                        column: x => x.ToStaffId,
                        principalTable: "STAFF",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TOKEN_TOKEN_ReferaTokenId",
                        column: x => x.ReferaTokenId,
                        principalTable: "TOKEN",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TRIGGERSUBSCRIPTION",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TriggerType = table.Column<int>(type: "int", nullable: true),
                    WebUserId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    WebUserRoleId = table.Column<long>(type: "bigint", nullable: true),
                    OrderNumber = table.Column<double>(type: "float", nullable: false),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRIGGERSUBSCRIPTION", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TRIGGERSUBSCRIPTION_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UPLOAD",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BaImage = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FileType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CategoryId = table.Column<long>(type: "bigint", nullable: true),
                    WebContentId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Comments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UploadType = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UPLOAD", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UPLOAD_CATEGORY_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "CATEGORY",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UPLOAD_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UPLOAD_WEBCONTENT_WebContentId",
                        column: x => x.WebContentId,
                        principalTable: "WEBCONTENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "USERICON",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WebUserId = table.Column<long>(type: "bigint", nullable: true),
                    WebUserRoleId = table.Column<long>(type: "bigint", nullable: true),
                    Icon = table.Column<int>(type: "int", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    OrderNumber = table.Column<double>(type: "float", nullable: false),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USERICON", x => x.Id);
                    table.ForeignKey(
                        name: "FK_USERICON_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "USERNOTIFICATION",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NotificationId = table.Column<long>(type: "bigint", nullable: true),
                    WebUserId = table.Column<long>(type: "bigint", nullable: true),
                    Seen = table.Column<bool>(type: "bit", nullable: false),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ToDepartmentId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USERNOTIFICATION", x => x.Id);
                    table.ForeignKey(
                        name: "FK_USERNOTIFICATION_DEPARTMENT_ToDepartmentId",
                        column: x => x.ToDepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_USERNOTIFICATION_NOTIFICATION_NotificationId",
                        column: x => x.NotificationId,
                        principalTable: "NOTIFICATION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "USERPREFERENCE",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WebUserId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    AbbreviationForHistory = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AbbreviationForExamination = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AbbreviationForInvestigations = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AbbreviationForTreatments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AbbreviationForManagement = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PharmacyRetailBillTemplate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PharmacyWholesaleBillTemplate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InwardDepositBillTemplate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InwardDepositCancelBillTemplate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChannellingBillTemplate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChannellingCancellationBillTemplate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChannelingDoctorPaymentBillTemplate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OpdBillForCashierTemplate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmsTemplateForOpdBillSetting = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstitutionSpecificItems = table.Column<bool>(type: "bit", nullable: false),
                    OpdBillTemplate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstitutionRestrictedBilling = table.Column<bool>(type: "bit", nullable: false),
                    OpdSettleWithoutReferralDetails = table.Column<bool>(type: "bit", nullable: false),
                    PartialPaymentOfOpdBillsAllowed = table.Column<bool>(type: "bit", nullable: false),
                    PartialPaymentOfOpdPreBillsAllowed = table.Column<bool>(type: "bit", nullable: false),
                    PaymentMethodAllowedInInwardMatrix = table.Column<bool>(type: "bit", nullable: false),
                    PharmacyBillPrabodha = table.Column<bool>(type: "bit", nullable: false),
                    CheckPaymentSchemeValidation = table.Column<bool>(type: "bit", nullable: false),
                    GrnBillDetailed = table.Column<bool>(type: "bit", nullable: false),
                    BhtNumberWithYear = table.Column<bool>(type: "bit", nullable: false),
                    BhtNumberWithOutAdmissionType = table.Column<bool>(type: "bit", nullable: false),
                    DepNumGenFromToDepartment = table.Column<bool>(type: "bit", nullable: false),
                    TranferNetTotalbyRetailRate = table.Column<bool>(type: "bit", nullable: false),
                    AllowtoChangePaymentMethodDuringPayment = table.Column<bool>(type: "bit", nullable: false),
                    OpdSettleWithoutCashTendered = table.Column<bool>(type: "bit", nullable: false),
                    ChannelWithOutReferenceNumber = table.Column<bool>(type: "bit", nullable: false),
                    PharmayPurchaseWithLastRate = table.Column<bool>(type: "bit", nullable: false),
                    InwardAddServiceBillTimeCheck = table.Column<bool>(type: "bit", nullable: false),
                    InwardMoChargeCalculateInitialTime = table.Column<bool>(type: "bit", nullable: false),
                    InwardChangeAdmissionFee = table.Column<bool>(type: "bit", nullable: false),
                    PharmacyBillWithOutItem = table.Column<bool>(type: "bit", nullable: false),
                    FiveFivePaperWithHeadings = table.Column<bool>(type: "bit", nullable: false),
                    ShowOnlyMarkedDoctors = table.Column<bool>(type: "bit", nullable: false),
                    ChannelSettleWithoutPatientPhoneNumber = table.Column<bool>(type: "bit", nullable: false),
                    OpdSettleWithoutPatientPhoneNumber = table.Column<bool>(type: "bit", nullable: false),
                    ChannelBillDouble = table.Column<bool>(type: "bit", nullable: false),
                    HasAwebsiteAsFrontEnd = table.Column<bool>(type: "bit", nullable: false),
                    ThemeName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogoUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LoggingHeader = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LoggingText = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChannelDoctorArivalMsgSend = table.Column<bool>(type: "bit", nullable: false),
                    MicroBiologyFont = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LogoName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OpdBillPaperType = table.Column<int>(type: "int", nullable: true),
                    InwardServiceBillPaperType = table.Column<int>(type: "int", nullable: true),
                    PharmacyBillPaperType = table.Column<int>(type: "int", nullable: true),
                    ChannelBillPaperType = table.Column<int>(type: "int", nullable: true),
                    InwardDepositPaymentBillPaper = table.Column<int>(type: "int", nullable: true),
                    PartialPaymentOfPharmacyBillsAllowed = table.Column<bool>(type: "bit", nullable: false),
                    ApplicationInstitution = table.Column<int>(type: "int", nullable: true),
                    ChannellingPaymentMethod = table.Column<int>(type: "int", nullable: true),
                    CanSettleOpdBillWithInvestigationsWithoutReferringDoctor = table.Column<bool>(type: "bit", nullable: true),
                    PrintBarcodeInOpdBill = table.Column<bool>(type: "bit", nullable: true),
                    SentEmailWithInvestigationReportApproval = table.Column<bool>(type: "bit", nullable: true),
                    SentSmsWithInvestigationRequestApproval = table.Column<bool>(type: "bit", nullable: true),
                    SentDailySmsSummeryForReferringDoctors = table.Column<bool>(type: "bit", nullable: true),
                    SmsUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmsUsername = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmsPassword = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmsUserAlias = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmsUsernameParameterName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmsPasswordParameterName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmsUserAliasParameterName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmsPhoneNumberParameterName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmsMessageParameterName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmsAuthenticationType = table.Column<int>(type: "int", nullable: true),
                    FamilyMembership = table.Column<bool>(type: "bit", nullable: false),
                    MembershipExpires = table.Column<bool>(type: "bit", nullable: false),
                    NeedAreaForPatientRegistration = table.Column<bool>(type: "bit", nullable: false),
                    NeedNicForPatientRegistration = table.Column<bool>(type: "bit", nullable: false),
                    NeedPhoneNumberForPatientRegistration = table.Column<bool>(type: "bit", nullable: false),
                    SendBulkSms = table.Column<bool>(type: "bit", nullable: false),
                    PharmacyBillFooter = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PharmacyBillHeader = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LongDateFormat = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShortDateFormat = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LongDateTimeFormat = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShortDateTimeFormat = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LongTimeFormat = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShortTimeFormat = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EncrptionKey = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NameRegex = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MobileRegex = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EmailRegex = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NicRegex = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LengthOfOTPIndexes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OpdItemListingStrategy = table.Column<int>(type: "int", nullable: true),
                    CcItemListingStrategy = table.Column<int>(type: "int", nullable: true),
                    OpdBillingStrategy = table.Column<int>(type: "int", nullable: true),
                    InwardItemListingStrategy = table.Column<int>(type: "int", nullable: true),
                    OpdTokenNumberGenerationStrategy = table.Column<int>(type: "int", nullable: true),
                    PrintOpdTokenNumber = table.Column<bool>(type: "bit", nullable: false),
                    AutodisplayMenu = table.Column<bool>(type: "bit", nullable: false),
                    ShowBillWiseDetails = table.Column<bool>(type: "bit", nullable: false),
                    InpatientFinalBillPrintHeader = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChangeTextCasesPatientName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SendSmsOnChannelBooking = table.Column<bool>(type: "bit", nullable: false),
                    SendSmsOnChannelBookingCancellation = table.Column<bool>(type: "bit", nullable: false),
                    SendSmsOnChannelDoctorArrival = table.Column<bool>(type: "bit", nullable: false),
                    SendSmsOnChannelBookingCompletion = table.Column<bool>(type: "bit", nullable: false),
                    SendSmsOnChannelBookingNoShow = table.Column<bool>(type: "bit", nullable: false),
                    SendSmsOnChannelBookingDocterPayment = table.Column<bool>(type: "bit", nullable: false),
                    SmsTemplateForChannelBooking = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmsTemplateForChannelBookingCancellation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmsTemplateForChannelDoctorArrival = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmsTemplateForChannelBookingCompletion = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmsTemplateForChannelBookingNoShow = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SmsTemplateForChannelBookingDoctorPayment = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OpdSettleWithoutPatientArea = table.Column<bool>(type: "bit", nullable: false),
                    OpdBillingAftershiftStart = table.Column<bool>(type: "bit", nullable: false),
                    NumberOfOPDBillCopies = table.Column<int>(type: "int", nullable: true),
                    NumberOfCCBillCopies = table.Column<int>(type: "int", nullable: true),
                    NumberOfChannellingBillCopies = table.Column<int>(type: "int", nullable: true),
                    InwardAddmissionStatemenetEnglish = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InwardAddmissionStatemenetSinhala = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ChannelingBillHeaderTemplate = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ShowBarCodeInChannelBookingBill = table.Column<bool>(type: "bit", nullable: false),
                    GenarateOpdTokenNumbersToCounterWise = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_USERPREFERENCE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_USERPREFERENCE_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_USERPREFERENCE_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VMP",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Strength = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VtmId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VMP", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "VMPP",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    VmpId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VMPP", x => x.Id);
                    table.ForeignKey(
                        name: "FK_VMPP_VMP_VmpId",
                        column: x => x.VmpId,
                        principalTable: "VMP",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "VTM",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VTM", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WEBTHEME",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WEBTHEME", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WEBUSER",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Password = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    RealUserName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WebUserPass = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WebUserPersonId = table.Column<long>(type: "bigint", nullable: true),
                    WebThemeId = table.Column<long>(type: "bigint", nullable: true),
                    WebUserRoleUserId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    StaffId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    ActivatorId = table.Column<long>(type: "bigint", nullable: true),
                    SelfRoleUserId = table.Column<long>(type: "bigint", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    Activated = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ActivatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WEBUSER", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WEBUSER_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WEBUSER_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WEBUSER_PERSON_WebUserPersonId",
                        column: x => x.WebUserPersonId,
                        principalTable: "PERSON",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WEBUSER_STAFF_StaffId",
                        column: x => x.StaffId,
                        principalTable: "STAFF",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WEBUSER_WEBUSER_ActivatorId",
                        column: x => x.ActivatorId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WEBUSER_WEBUSER_CreaterId",
                        column: x => x.CreaterId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WEBUSER_WEBUSER_RetirerId",
                        column: x => x.RetirerId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WEBUSERDASHBOARD",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WebUserId = table.Column<long>(type: "bigint", nullable: true),
                    Dashboard = table.Column<int>(type: "int", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WEBUSERDASHBOARD", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WEBUSERDASHBOARD_WEBUSER_CreaterId",
                        column: x => x.CreaterId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WEBUSERDASHBOARD_WEBUSER_RetirerId",
                        column: x => x.RetirerId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WEBUSERDASHBOARD_WEBUSER_WebUserId",
                        column: x => x.WebUserId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WEBUSERDEPARTMENT",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WebUserId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WEBUSERDEPARTMENT", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WEBUSERDEPARTMENT_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WEBUSERDEPARTMENT_WEBUSER_WebUserId",
                        column: x => x.WebUserId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WEBUSERPAYMENTSCHEME",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WebUserId = table.Column<long>(type: "bigint", nullable: true),
                    PaymentSchemeId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WEBUSERPAYMENTSCHEME", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WEBUSERPAYMENTSCHEME_PAYMENTSCHEME_PaymentSchemeId",
                        column: x => x.PaymentSchemeId,
                        principalTable: "PAYMENTSCHEME",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WEBUSERPAYMENTSCHEME_WEBUSER_CreaterId",
                        column: x => x.CreaterId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WEBUSERPAYMENTSCHEME_WEBUSER_RetirerId",
                        column: x => x.RetirerId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WEBUSERPAYMENTSCHEME_WEBUSER_WebUserId",
                        column: x => x.WebUserId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WEBUSERROLE",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Privileges = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WEBUSERROLE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WEBUSERROLE_WEBUSER_CreaterId",
                        column: x => x.CreaterId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WEBUSERROLE_WEBUSER_RetirerId",
                        column: x => x.RetirerId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WEBUSERROUTE",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WebUserId = table.Column<long>(type: "bigint", nullable: true),
                    RouteId = table.Column<long>(type: "bigint", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WEBUSERROUTE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WEBUSERROUTE_ROUTES_RouteId",
                        column: x => x.RouteId,
                        principalTable: "ROUTES",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WEBUSERROUTE_WEBUSER_CreaterId",
                        column: x => x.CreaterId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WEBUSERROUTE_WEBUSER_RetirerId",
                        column: x => x.RetirerId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WEBUSERROUTE_WEBUSER_WebUserId",
                        column: x => x.WebUserId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WEBUSERPRIVILEGE",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Privilege = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WebUserRoleId = table.Column<long>(type: "bigint", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WEBUSERPRIVILEGE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WEBUSERPRIVILEGE_WEBUSERROLE_WebUserRoleId",
                        column: x => x.WebUserRoleId,
                        principalTable: "WEBUSERROLE",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WEBUSERROLEPRIVILEGE",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WebUserRoleId = table.Column<long>(type: "bigint", nullable: true),
                    Privilege = table.Column<int>(type: "int", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetirerId = table.Column<long>(type: "bigint", nullable: true),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tname = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WEBUSERROLEPRIVILEGE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WEBUSERROLEPRIVILEGE_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WEBUSERROLEPRIVILEGE_WEBUSERROLE_WebUserRoleId",
                        column: x => x.WebUserRoleId,
                        principalTable: "WEBUSERROLE",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WEBUSERROLEPRIVILEGE_WEBUSER_CreaterId",
                        column: x => x.CreaterId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WEBUSERROLEPRIVILEGE_WEBUSER_RetirerId",
                        column: x => x.RetirerId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WEBUSERROLEUSER",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WebUserRoleId = table.Column<long>(type: "bigint", nullable: true),
                    WebUserId = table.Column<long>(type: "bigint", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WEBUSERROLEUSER", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WEBUSERROLEUSER_WEBUSERROLE_WebUserRoleId",
                        column: x => x.WebUserRoleId,
                        principalTable: "WEBUSERROLE",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WEBUSERROLEUSER_WEBUSER_WebUserId",
                        column: x => x.WebUserId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ADMISSION_CreaterId",
                table: "ADMISSION",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_ADMISSION_PatientEncounterId",
                table: "ADMISSION",
                column: "PatientEncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_AGENTHISTORY_AgencyId",
                table: "AGENTHISTORY",
                column: "AgencyId");

            migrationBuilder.CreateIndex(
                name: "IX_AGENTHISTORY_BillId",
                table: "AGENTHISTORY",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_AGENTHISTORY_BillItemId",
                table: "AGENTHISTORY",
                column: "BillItemId");

            migrationBuilder.CreateIndex(
                name: "IX_AGENTHISTORY_BillSessionId",
                table: "AGENTHISTORY",
                column: "BillSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_AGENTHISTORY_CreaterId",
                table: "AGENTHISTORY",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_AGENTHISTORY_DepartmentId",
                table: "AGENTHISTORY",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_AGENTHISTORY_InstitutionId",
                table: "AGENTHISTORY",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_AGENTHISTORY_RetirerId",
                table: "AGENTHISTORY",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_AMP_CreaterId",
                table: "AMP",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_AMP_EditerId",
                table: "AMP",
                column: "EditerId");

            migrationBuilder.CreateIndex(
                name: "IX_APIKEY_CreaterId",
                table: "APIKEY",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_APIKEY_InstitutionId",
                table: "APIKEY",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_APPEMAIL_BillId",
                table: "APPEMAIL",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_APPEMAIL_CreaterId",
                table: "APPEMAIL",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_APPEMAIL_DepartmentId",
                table: "APPEMAIL",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_APPEMAIL_InstitutionId",
                table: "APPEMAIL",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_APPEMAIL_RetirerId",
                table: "APPEMAIL",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_AREA_CreaterId",
                table: "AREA",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_AREA_InstitutionId",
                table: "AREA",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_AREA_RetirerId",
                table: "AREA",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_BackwardReferenceBillId",
                table: "BILL",
                column: "BackwardReferenceBillId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_BankId",
                table: "BILL",
                column: "BankId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_BilledBillId",
                table: "BILL",
                column: "BilledBillId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_CancelledBillId",
                table: "BILL",
                column: "CancelledBillId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_CashTransactionId",
                table: "BILL",
                column: "CashTransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_CategoryId",
                table: "BILL",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_CheckedById",
                table: "BILL",
                column: "CheckedById");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_CollectingCentreId",
                table: "BILL",
                column: "CollectingCentreId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_CreaterId",
                table: "BILL",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_CreditCompanyId",
                table: "BILL",
                column: "CreditCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_DepartmentId",
                table: "BILL",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_EditorId",
                table: "BILL",
                column: "EditorId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_ForwardReferenceBillId",
                table: "BILL",
                column: "ForwardReferenceBillId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_FromDepartmentId",
                table: "BILL",
                column: "FromDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_FromInstitutionId",
                table: "BILL",
                column: "FromInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_InstitutionId",
                table: "BILL",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_ItemId",
                table: "BILL",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_MembershipSchemeId",
                table: "BILL",
                column: "MembershipSchemeId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_PaidBillId",
                table: "BILL",
                column: "PaidBillId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_PatientEncounterId",
                table: "BILL",
                column: "PatientEncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_PatientId",
                table: "BILL",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_PaymentSchemeId",
                table: "BILL",
                column: "PaymentSchemeId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_PaymentSchemeInstitutionId",
                table: "BILL",
                column: "PaymentSchemeInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_PersonId",
                table: "BILL",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_ReferenceBillId",
                table: "BILL",
                column: "ReferenceBillId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_ReferenceInstitutionId",
                table: "BILL",
                column: "ReferenceInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_ReferredById",
                table: "BILL",
                column: "ReferredById");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_ReferredByInstitutionId",
                table: "BILL",
                column: "ReferredByInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_RefundedBillId",
                table: "BILL",
                column: "RefundedBillId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_RetirerId",
                table: "BILL",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_StaffId",
                table: "BILL",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_ToDepartmentId",
                table: "BILL",
                column: "ToDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_BILL_ToInstitutionId",
                table: "BILL",
                column: "ToInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLCOMPONENT_BillId",
                table: "BILLCOMPONENT",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLCOMPONENT_BillItemId",
                table: "BILLCOMPONENT",
                column: "BillItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLCOMPONENT_CreaterId",
                table: "BILLCOMPONENT",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLCOMPONENT_DepartmentId",
                table: "BILLCOMPONENT",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLCOMPONENT_InstitutionId",
                table: "BILLCOMPONENT",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLCOMPONENT_ItemId",
                table: "BILLCOMPONENT",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLCOMPONENT_PackegeId",
                table: "BILLCOMPONENT",
                column: "PackegeId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLCOMPONENT_RetirerId",
                table: "BILLCOMPONENT",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLCOMPONENT_SpecialityId",
                table: "BILLCOMPONENT",
                column: "SpecialityId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLCOMPONENT_StaffId",
                table: "BILLCOMPONENT",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLFEE_BillId",
                table: "BILLFEE",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLFEE_BillItemId",
                table: "BILLFEE",
                column: "BillItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLFEE_ChildEncounterId",
                table: "BILLFEE",
                column: "ChildEncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLFEE_CreaterId",
                table: "BILLFEE",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLFEE_DepartmentId",
                table: "BILLFEE",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLFEE_EditorId",
                table: "BILLFEE",
                column: "EditorId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLFEE_FeeId",
                table: "BILLFEE",
                column: "FeeId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLFEE_InstitutionId",
                table: "BILLFEE",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLFEE_PatienEncounterId",
                table: "BILLFEE",
                column: "PatienEncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLFEE_PatientId",
                table: "BILLFEE",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLFEE_PriceMatrixId",
                table: "BILLFEE",
                column: "PriceMatrixId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLFEE_ReferenceBillFeeId",
                table: "BILLFEE",
                column: "ReferenceBillFeeId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLFEE_ReferenceBillItemId",
                table: "BILLFEE",
                column: "ReferenceBillItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLFEE_ReferencePatientRoomId",
                table: "BILLFEE",
                column: "ReferencePatientRoomId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLFEE_RetirerId",
                table: "BILLFEE",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLFEE_SpecialityId",
                table: "BILLFEE",
                column: "SpecialityId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLFEE_StaffId",
                table: "BILLFEE",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLFEEPAYMENT_BillFeeId",
                table: "BILLFEEPAYMENT",
                column: "BillFeeId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLFEEPAYMENT_CreaterId",
                table: "BILLFEEPAYMENT",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLFEEPAYMENT_DepartmentId",
                table: "BILLFEEPAYMENT",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLFEEPAYMENT_InstitutionId",
                table: "BILLFEEPAYMENT",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLFEEPAYMENT_PaymentId",
                table: "BILLFEEPAYMENT",
                column: "PaymentId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLFEEPAYMENT_RetirerId",
                table: "BILLFEEPAYMENT",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLITEM_BillId",
                table: "BILLITEM",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLITEM_CreaterId",
                table: "BILLITEM",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLITEM_EditorId",
                table: "BILLITEM",
                column: "EditorId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLITEM_ExpenseBillId",
                table: "BILLITEM",
                column: "ExpenseBillId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLITEM_ItemId1",
                table: "BILLITEM",
                column: "ItemId1");

            migrationBuilder.CreateIndex(
                name: "IX_BILLITEM_PaidForBillFeeId",
                table: "BILLITEM",
                column: "PaidForBillFeeId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLITEM_ParentBillItemId",
                table: "BILLITEM",
                column: "ParentBillItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLITEM_PatientEncounterId",
                table: "BILLITEM",
                column: "PatientEncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLITEM_PriceMatrixId",
                table: "BILLITEM",
                column: "PriceMatrixId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLITEM_ReferanceBillItemId",
                table: "BILLITEM",
                column: "ReferanceBillItemId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLITEM_ReferenceBillId",
                table: "BILLITEM",
                column: "ReferenceBillId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLITEM_RetirerId",
                table: "BILLITEM",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLNUMBER_DepartmentId",
                table: "BILLNUMBER",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLNUMBER_InstitutionId",
                table: "BILLNUMBER",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLNUMBER_RetirerId",
                table: "BILLNUMBER",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLNUMBER_ToDepartmentId",
                table: "BILLNUMBER",
                column: "ToDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLSESSION_BillId",
                table: "BILLSESSION",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_BILLSESSION_BillItemId",
                table: "BILLSESSION",
                column: "BillItemId",
                unique: true,
                filter: "[BillItemId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BILLSESSION_CreaterId",
                table: "BILLSESSION",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_CASHBOOK_CreaterId",
                table: "CASHBOOK",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_CASHBOOK_DepartmentId",
                table: "CASHBOOK",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CASHBOOK_InstitutionId",
                table: "CASHBOOK",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_CASHBOOK_RetirerId",
                table: "CASHBOOK",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_CASHBOOKENTRY_CashBookId",
                table: "CASHBOOKENTRY",
                column: "CashBookId");

            migrationBuilder.CreateIndex(
                name: "IX_CASHBOOKENTRY_CreaterId",
                table: "CASHBOOKENTRY",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_CASHBOOKENTRY_DepartmentId",
                table: "CASHBOOKENTRY",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CASHBOOKENTRY_InstitutionId",
                table: "CASHBOOKENTRY",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_CASHTRANSACTION_CreaterId",
                table: "CASHTRANSACTION",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_CASHTRANSACTION_DepartmentId",
                table: "CASHTRANSACTION",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CASHTRANSACTION_DrawerId",
                table: "CASHTRANSACTION",
                column: "DrawerId");

            migrationBuilder.CreateIndex(
                name: "IX_CASHTRANSACTION_InstitutionId",
                table: "CASHTRANSACTION",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_CATEGORY_CreaterId",
                table: "CATEGORY",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_CATEGORY_EditerId",
                table: "CATEGORY",
                column: "EditerId");

            migrationBuilder.CreateIndex(
                name: "IX_CATEGORY_InstitutionId",
                table: "CATEGORY",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_CATEGORY_ParentCategoryId",
                table: "CATEGORY",
                column: "ParentCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CATEGORY_PaymentSchemeId",
                table: "CATEGORY",
                column: "PaymentSchemeId");

            migrationBuilder.CreateIndex(
                name: "IX_CATEGORY_RetirerId",
                table: "CATEGORY",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_CATEGORY_SpecialityId",
                table: "CATEGORY",
                column: "SpecialityId");

            migrationBuilder.CreateIndex(
                name: "IX_CATEGORY_StaffId",
                table: "CATEGORY",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_CATEGORYITEM_CategoryId",
                table: "CATEGORYITEM",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_CATEGORYITEM_CreaterId",
                table: "CATEGORYITEM",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_CATEGORYITEM_ItemId",
                table: "CATEGORYITEM",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_CATEGORYITEM_RetirerId",
                table: "CATEGORYITEM",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_CONFIGOPTION_CreaterId",
                table: "CONFIGOPTION",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_CONFIGOPTION_DepartmentId",
                table: "CONFIGOPTION",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_CONFIGOPTION_InstitutionId",
                table: "CONFIGOPTION",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_DEPARTMENT_CreaterId",
                table: "DEPARTMENT",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_DEPARTMENT_EditerId",
                table: "DEPARTMENT",
                column: "EditerId");

            migrationBuilder.CreateIndex(
                name: "IX_DEPARTMENT_InstitutionId",
                table: "DEPARTMENT",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_DEPARTMENT_LabDepartmentId",
                table: "DEPARTMENT",
                column: "LabDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DEPARTMENT_LabInstitutionId",
                table: "DEPARTMENT",
                column: "LabInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_DEPARTMENT_RetirerId",
                table: "DEPARTMENT",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_DEPARTMENT_SampleDepartmentId",
                table: "DEPARTMENT",
                column: "SampleDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DEPARTMENT_SiteId",
                table: "DEPARTMENT",
                column: "SiteId");

            migrationBuilder.CreateIndex(
                name: "IX_DEPARTMENT_SuperDepartmentId",
                table: "DEPARTMENT",
                column: "SuperDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DESIGNATION_CreaterId",
                table: "DESIGNATION",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_DRAWER_CashierId",
                table: "DRAWER",
                column: "CashierId");

            migrationBuilder.CreateIndex(
                name: "IX_DRAWER_CreaterId",
                table: "DRAWER",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_DRAWER_DepartmentId",
                table: "DRAWER",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_DRAWER_InstitutionId",
                table: "DRAWER",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ENCOUNTERCREDITCOMPANY_CreaterId",
                table: "ENCOUNTERCREDITCOMPANY",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_ENCOUNTERCREDITCOMPANY_InstitutionId",
                table: "ENCOUNTERCREDITCOMPANY",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ENCOUNTERCREDITCOMPANY_PatientEncounterId",
                table: "ENCOUNTERCREDITCOMPANY",
                column: "PatientEncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_ENCOUNTERCREDITCOMPANY_RetirerId",
                table: "ENCOUNTERCREDITCOMPANY",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_FAMILY_CreatedDepartmentId",
                table: "FAMILY",
                column: "CreatedDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FAMILY_CreatedInstitutionId",
                table: "FAMILY",
                column: "CreatedInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_FAMILY_CreaterId",
                table: "FAMILY",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_FAMILY_EditerId",
                table: "FAMILY",
                column: "EditerId");

            migrationBuilder.CreateIndex(
                name: "IX_FAMILY_MembershipSchemeId",
                table: "FAMILY",
                column: "MembershipSchemeId");

            migrationBuilder.CreateIndex(
                name: "IX_FAMILY_RetirerId",
                table: "FAMILY",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_FAMILYMEMBER_CreaterId",
                table: "FAMILYMEMBER",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_FAMILYMEMBER_EditerId",
                table: "FAMILYMEMBER",
                column: "EditerId");

            migrationBuilder.CreateIndex(
                name: "IX_FAMILYMEMBER_FamilyId",
                table: "FAMILYMEMBER",
                column: "FamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_FAMILYMEMBER_PatientId",
                table: "FAMILYMEMBER",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_FAMILYMEMBER_RelationToChhId",
                table: "FAMILYMEMBER",
                column: "RelationToChhId");

            migrationBuilder.CreateIndex(
                name: "IX_FAMILYMEMBER_RetirerId",
                table: "FAMILYMEMBER",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_FEE_CreaterId",
                table: "FEE",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_FEE_DepartmentId",
                table: "FEE",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FEE_EditerId",
                table: "FEE",
                column: "EditerId");

            migrationBuilder.CreateIndex(
                name: "IX_FEE_InstitutionId",
                table: "FEE",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_FEE_ItemId",
                table: "FEE",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_FEE_RetirerId",
                table: "FEE",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_FEE_SpecialityId",
                table: "FEE",
                column: "SpecialityId");

            migrationBuilder.CreateIndex(
                name: "IX_FEE_StaffId",
                table: "FEE",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_FEECHANGE_CreaterId",
                table: "FEECHANGE",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_FEECHANGE_FeeId",
                table: "FEECHANGE",
                column: "FeeId");

            migrationBuilder.CreateIndex(
                name: "IX_FEECHANGE_RetirerId",
                table: "FEECHANGE",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_FEEVALUE_CategoryId",
                table: "FEEVALUE",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_FEEVALUE_CreaterId",
                table: "FEEVALUE",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_FEEVALUE_DepartmentId",
                table: "FEEVALUE",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_FEEVALUE_InstitutionId",
                table: "FEEVALUE",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_FEEVALUE_ItemId",
                table: "FEEVALUE",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_FEEVALUE_RetirerId",
                table: "FEEVALUE",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_FORM_CreaterId",
                table: "FORM",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_FORM_EditorId",
                table: "FORM",
                column: "EditorId");

            migrationBuilder.CreateIndex(
                name: "IX_FORM_RetirerId",
                table: "FORM",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_GRADE_CreaterId",
                table: "GRADE",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_INSTITUTION_BankBranchId",
                table: "INSTITUTION",
                column: "BankBranchId");

            migrationBuilder.CreateIndex(
                name: "IX_INSTITUTION_ContactPersonId",
                table: "INSTITUTION",
                column: "ContactPersonId");

            migrationBuilder.CreateIndex(
                name: "IX_INSTITUTION_CreaterId",
                table: "INSTITUTION",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_INSTITUTION_EditerId",
                table: "INSTITUTION",
                column: "EditerId");

            migrationBuilder.CreateIndex(
                name: "IX_INSTITUTION_FeeListTypeId",
                table: "INSTITUTION",
                column: "FeeListTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_INSTITUTION_InstitutionId",
                table: "INSTITUTION",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_INSTITUTION_LabDepartmentId",
                table: "INSTITUTION",
                column: "LabDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_INSTITUTION_LabInstitutionId",
                table: "INSTITUTION",
                column: "LabInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_INSTITUTION_ParentInstitutionId",
                table: "INSTITUTION",
                column: "ParentInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_INSTITUTION_RetirerId",
                table: "INSTITUTION",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_INSTITUTION_RouteId",
                table: "INSTITUTION",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_ISSUERATEMARGINS_CreaterId",
                table: "ISSUERATEMARGINS",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_ISSUERATEMARGINS_FromDepartmentId",
                table: "ISSUERATEMARGINS",
                column: "FromDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ISSUERATEMARGINS_FromInstitutionId",
                table: "ISSUERATEMARGINS",
                column: "FromInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ISSUERATEMARGINS_RetirerId",
                table: "ISSUERATEMARGINS",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_ISSUERATEMARGINS_ToDepartmentId",
                table: "ISSUERATEMARGINS",
                column: "ToDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ISSUERATEMARGINS_ToInstitutionId",
                table: "ISSUERATEMARGINS",
                column: "ToInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_AfterSessionId",
                table: "ITEM",
                column: "AfterSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_BaseUnitId",
                table: "ITEM",
                column: "BaseUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_BeforeSessionId",
                table: "ITEM",
                column: "BeforeSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_BilledAsId",
                table: "ITEM",
                column: "BilledAsId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_CategoryId",
                table: "ITEM",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_CreaterId",
                table: "ITEM",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_DepartmentId",
                table: "ITEM",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_EditerId",
                table: "ITEM",
                column: "EditerId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_FinancialCategoryId",
                table: "ITEM",
                column: "FinancialCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_ForDepartmentId",
                table: "ITEM",
                column: "ForDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_ForInstitutionId",
                table: "ITEM",
                column: "ForInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_ImporterId",
                table: "ITEM",
                column: "ImporterId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_InstitutionId",
                table: "ITEM",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_IssueUnitId",
                table: "ITEM",
                column: "IssueUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_MachineId",
                table: "ITEM",
                column: "MachineId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_ManufacturerId",
                table: "ITEM",
                column: "ManufacturerId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_MasterItemReferenceId",
                table: "ITEM",
                column: "MasterItemReferenceId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_MeasurementUnitId",
                table: "ITEM",
                column: "MeasurementUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_OriginatingSessionId",
                table: "ITEM",
                column: "OriginatingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_ParentItemId",
                table: "ITEM",
                column: "ParentItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_ReportedAsId",
                table: "ITEM",
                column: "ReportedAsId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_ReportFormatId",
                table: "ITEM",
                column: "ReportFormatId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_RetirerId",
                table: "ITEM",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_SessionNumberGeneratorId",
                table: "ITEM",
                column: "SessionNumberGeneratorId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_SpecialityId",
                table: "ITEM",
                column: "SpecialityId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_StaffId",
                table: "ITEM",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_StrengthUnitId",
                table: "ITEM",
                column: "StrengthUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_VmpId",
                table: "ITEM",
                column: "VmpId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEM_WorksheetId",
                table: "ITEM",
                column: "WorksheetId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEMBATCH_CreaterId",
                table: "ITEMBATCH",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEMBATCH_DepartmentId",
                table: "ITEMBATCH",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEMBATCH_EditerId",
                table: "ITEMBATCH",
                column: "EditerId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEMBATCH_ItemId",
                table: "ITEMBATCH",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEMBATCH_RetirerId",
                table: "ITEMBATCH",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEMMAPPING_CreaterId",
                table: "ITEMMAPPING",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEMMAPPING_DepartmentId",
                table: "ITEMMAPPING",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEMMAPPING_InstitutionId",
                table: "ITEMMAPPING",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEMMAPPING_ItemId",
                table: "ITEMMAPPING",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEMMAPPING_RetirerId",
                table: "ITEMMAPPING",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEMSCATEGORIES_CategoryId",
                table: "ITEMSCATEGORIES",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEMSCATEGORIES_CreaterId",
                table: "ITEMSCATEGORIES",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEMSCATEGORIES_ItemId",
                table: "ITEMSCATEGORIES",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_ITEMSCATEGORIES_RetirerId",
                table: "ITEMSCATEGORIES",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_LOGINS_DepartmentId",
                table: "LOGINS",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_LOGINS_InstitutionId",
                table: "LOGINS",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_LOGINS_WebUserId",
                table: "LOGINS",
                column: "WebUserId");

            migrationBuilder.CreateIndex(
                name: "IX_MACHINE_CreaterId",
                table: "MACHINE",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_MACHINE_DepartmentId",
                table: "MACHINE",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_MACHINE_InstitutionId",
                table: "MACHINE",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_MAPPING_Item1Id",
                table: "MAPPING",
                column: "Item1Id");

            migrationBuilder.CreateIndex(
                name: "IX_MAPPING_Item2Id",
                table: "MAPPING",
                column: "Item2Id");

            migrationBuilder.CreateIndex(
                name: "IX_MEASUREMENTUNIT_CreaterId",
                table: "MEASUREMENTUNIT",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_MEASUREMENTUNIT_EditerId",
                table: "MEASUREMENTUNIT",
                column: "EditerId");

            migrationBuilder.CreateIndex(
                name: "IX_MEASUREMENTUNIT_RetirerId",
                table: "MEASUREMENTUNIT",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_MEDICALPACKAGEITEM_CreaterId",
                table: "MEDICALPACKAGEITEM",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_MEDICALPACKAGEITEM_ItemId",
                table: "MEDICALPACKAGEITEM",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_MEDICALPACKAGEITEM_PackegeId",
                table: "MEDICALPACKAGEITEM",
                column: "PackegeId");

            migrationBuilder.CreateIndex(
                name: "IX_MEDICALPACKAGEITEM_RetirerId",
                table: "MEDICALPACKAGEITEM",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_MEMBERSHIPSCHEME_CreaterId",
                table: "MEMBERSHIPSCHEME",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_MEMBERSHIPSCHEME_InstitutionId",
                table: "MEMBERSHIPSCHEME",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_MEMBERSHIPSCHEME_RetirerId",
                table: "MEMBERSHIPSCHEME",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_BillId",
                table: "NOTIFICATION",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_CreaterId",
                table: "NOTIFICATION",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_NOTIFICATION_RetirerId",
                table: "NOTIFICATION",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_PACKAGEITEM_CreaterId",
                table: "PACKAGEITEM",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_PACKAGEITEM_ItemId",
                table: "PACKAGEITEM",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PACKAGEITEM_PackegeId",
                table: "PACKAGEITEM",
                column: "PackegeId");

            migrationBuilder.CreateIndex(
                name: "IX_PACKAGEITEM_RetirerId",
                table: "PACKAGEITEM",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENT_CreatedInstitutionId",
                table: "PATIENT",
                column: "CreatedInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENT_CreaterId",
                table: "PATIENT",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENT_EditerId",
                table: "PATIENT",
                column: "EditerId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENT_PersonId",
                table: "PATIENT",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENT_RetirerId",
                table: "PATIENT",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTDEPOSIT_CreaterId",
                table: "PATIENTDEPOSIT",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTDEPOSIT_DepartmentId",
                table: "PATIENTDEPOSIT",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTDEPOSIT_InstitutionId",
                table: "PATIENTDEPOSIT",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTDEPOSIT_PatientId",
                table: "PATIENTDEPOSIT",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTDEPOSIT_RetirerId",
                table: "PATIENTDEPOSIT",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTDEPOSITHISTORY_BillId",
                table: "PATIENTDEPOSITHISTORY",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTDEPOSITHISTORY_BillItemId",
                table: "PATIENTDEPOSITHISTORY",
                column: "BillItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTDEPOSITHISTORY_BillSessionId",
                table: "PATIENTDEPOSITHISTORY",
                column: "BillSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTDEPOSITHISTORY_CreaterId",
                table: "PATIENTDEPOSITHISTORY",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTDEPOSITHISTORY_DepartmentId",
                table: "PATIENTDEPOSITHISTORY",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTDEPOSITHISTORY_InstitutionId",
                table: "PATIENTDEPOSITHISTORY",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTDEPOSITHISTORY_PatientDepositId",
                table: "PATIENTDEPOSITHISTORY",
                column: "PatientDepositId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTDEPOSITHISTORY_RetirerId",
                table: "PATIENTDEPOSITHISTORY",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTENCOUNTER_BillId",
                table: "PATIENTENCOUNTER",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTENCOUNTER_CreaterId",
                table: "PATIENTENCOUNTER",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTENCOUNTER_DepartmentId",
                table: "PATIENTENCOUNTER",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTENCOUNTER_EditerId",
                table: "PATIENTENCOUNTER",
                column: "EditerId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTENCOUNTER_InstitutionId",
                table: "PATIENTENCOUNTER",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTENCOUNTER_PatientEncounterId",
                table: "PATIENTENCOUNTER",
                column: "PatientEncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTENCOUNTER_PatientId",
                table: "PATIENTENCOUNTER",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTENCOUNTER_ReferringDepartmentId",
                table: "PATIENTENCOUNTER",
                column: "ReferringDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTENCOUNTER_ReferringInstitutionId",
                table: "PATIENTENCOUNTER",
                column: "ReferringInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTENCOUNTER_ReferringStaffId",
                table: "PATIENTENCOUNTER",
                column: "ReferringStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTENCOUNTER_RetirerId",
                table: "PATIENTENCOUNTER",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTFLAG_CreaterId",
                table: "PATIENTFLAG",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTFLAG_PatientEncounterId",
                table: "PATIENTFLAG",
                column: "PatientEncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTFLAG_PatientId",
                table: "PATIENTFLAG",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTFLAG_RetirerId",
                table: "PATIENTFLAG",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTITEM_BillId",
                table: "PATIENTITEM",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTITEM_BillItemId",
                table: "PATIENTITEM",
                column: "BillItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTITEM_CreaterId",
                table: "PATIENTITEM",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTITEM_ItemId",
                table: "PATIENTITEM",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTITEM_PackegeId",
                table: "PATIENTITEM",
                column: "PackegeId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTITEM_PatientEncounterId",
                table: "PATIENTITEM",
                column: "PatientEncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTITEM_PatientId",
                table: "PATIENTITEM",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTITEM_RetirerId",
                table: "PATIENTITEM",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTROOM_AdmissionId",
                table: "PATIENTROOM",
                column: "AdmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTROOM_CreaterId",
                table: "PATIENTROOM",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTROOM_PatientEncounterId",
                table: "PATIENTROOM",
                column: "PatientEncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTROOM_RoomId",
                table: "PATIENTROOM",
                column: "RoomId");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENT_BankId",
                table: "PAYMENT",
                column: "BankId");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENT_BillId",
                table: "PAYMENT",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENT_CashbookEntryId",
                table: "PAYMENT",
                column: "CashbookEntryId");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENT_CashbookId",
                table: "PAYMENT",
                column: "CashbookId");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENT_CreaterId",
                table: "PAYMENT",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENT_DepartmentId",
                table: "PAYMENT",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENT_HandoverAcceptBillId",
                table: "PAYMENT",
                column: "HandoverAcceptBillId");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENT_HandoverCreatedBillId",
                table: "PAYMENT",
                column: "HandoverCreatedBillId");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENT_InstitutionId",
                table: "PAYMENT",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENT_RealizerId",
                table: "PAYMENT",
                column: "RealizerId");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENT_RetirerId",
                table: "PAYMENT",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENTGATEWAYTRANSACTION_BillId",
                table: "PAYMENTGATEWAYTRANSACTION",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENTGATEWAYTRANSACTION_CreaterId",
                table: "PAYMENTGATEWAYTRANSACTION",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENTGATEWAYTRANSACTION_DepartmentId",
                table: "PAYMENTGATEWAYTRANSACTION",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENTGATEWAYTRANSACTION_InstitutionId",
                table: "PAYMENTGATEWAYTRANSACTION",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENTGATEWAYTRANSACTION_RetirerId",
                table: "PAYMENTGATEWAYTRANSACTION",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENTSCHEME_CreaterId",
                table: "PAYMENTSCHEME",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENTSCHEME_DepartmentId",
                table: "PAYMENTSCHEME",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENTSCHEME_InstitutionId",
                table: "PAYMENTSCHEME",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_PAYMENTSCHEME_RetirerId",
                table: "PAYMENTSCHEME",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_PERSON_AreaId",
                table: "PERSON",
                column: "AreaId");

            migrationBuilder.CreateIndex(
                name: "IX_PERSON_BloodGroupId",
                table: "PERSON",
                column: "BloodGroupId");

            migrationBuilder.CreateIndex(
                name: "IX_PERSON_CivilStatusId",
                table: "PERSON",
                column: "CivilStatusId");

            migrationBuilder.CreateIndex(
                name: "IX_PERSON_CreaterId",
                table: "PERSON",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_PERSON_DepartmentId",
                table: "PERSON",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PERSON_EditerId",
                table: "PERSON",
                column: "EditerId");

            migrationBuilder.CreateIndex(
                name: "IX_PERSON_InstitutionId",
                table: "PERSON",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_PERSON_MembershipSchemeId",
                table: "PERSON",
                column: "MembershipSchemeId");

            migrationBuilder.CreateIndex(
                name: "IX_PERSON_OccupationId",
                table: "PERSON",
                column: "OccupationId");

            migrationBuilder.CreateIndex(
                name: "IX_PERSON_RaceId",
                table: "PERSON",
                column: "RaceId");

            migrationBuilder.CreateIndex(
                name: "IX_PERSON_ReligionId",
                table: "PERSON",
                column: "ReligionId");

            migrationBuilder.CreateIndex(
                name: "IX_PERSON_RetirerId",
                table: "PERSON",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_PERSONINSTITUTION_CreaterId",
                table: "PERSONINSTITUTION",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_PERSONINSTITUTION_DepartmentId",
                table: "PERSONINSTITUTION",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PERSONINSTITUTION_InstitutionId",
                table: "PERSONINSTITUTION",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_PERSONINSTITUTION_PersonId",
                table: "PERSONINSTITUTION",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_PERSONINSTITUTION_RetirerId",
                table: "PERSONINSTITUTION",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_PERSONINSTITUTION_StaffId",
                table: "PERSONINSTITUTION",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_PHARMACEUTICALBILLITEM_BillItemId",
                table: "PHARMACEUTICALBILLITEM",
                column: "BillItemId",
                unique: true,
                filter: "[BillItemId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PHARMACEUTICALBILLITEM_CreaterId",
                table: "PHARMACEUTICALBILLITEM",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_PHARMACEUTICALBILLITEM_ItemBatchId",
                table: "PHARMACEUTICALBILLITEM",
                column: "ItemBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_PRICEMATRIX_CreaterId",
                table: "PRICEMATRIX",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_PRICEMATRIX_DepartmentId",
                table: "PRICEMATRIX",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_PRICEMATRIX_EditerId",
                table: "PRICEMATRIX",
                column: "EditerId");

            migrationBuilder.CreateIndex(
                name: "IX_PRICEMATRIX_InstitutionId",
                table: "PRICEMATRIX",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_PRICEMATRIX_ItemId",
                table: "PRICEMATRIX",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_PRICEMATRIX_RetirerId",
                table: "PRICEMATRIX",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_REPORTTEMPLATE_CategoryId",
                table: "REPORTTEMPLATE",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_REPORTTEMPLATE_CreaterId",
                table: "REPORTTEMPLATE",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_REPORTTEMPLATE_RetirerId",
                table: "REPORTTEMPLATE",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_ROOM_CreaterId",
                table: "ROOM",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_ROOM_DepartmentId",
                table: "ROOM",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_ROOM_InstitutionId",
                table: "ROOM",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ROOM_RoomCategoryId",
                table: "ROOM",
                column: "RoomCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_ROOMCATEGORY_CreaterId",
                table: "ROOMCATEGORY",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_ROOMCATEGORY_InstitutionId",
                table: "ROOMCATEGORY",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ROSTER_CreaterId",
                table: "ROSTER",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_ROSTER_InstitutionId",
                table: "ROSTER",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ROUTES_CreaterId",
                table: "ROUTES",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_ROUTES_InstitutionId",
                table: "ROUTES",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_ROUTES_RetirerId",
                table: "ROUTES",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_SERVICESESESSIONINSTANCE_CreaterId",
                table: "SERVICESESESSIONINSTANCE",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_SERVICESESESSIONINSTANCE_DepartmentId",
                table: "SERVICESESESSIONINSTANCE",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SERVICESESESSIONINSTANCE_EditerId",
                table: "SERVICESESESSIONINSTANCE",
                column: "EditerId");

            migrationBuilder.CreateIndex(
                name: "IX_SERVICESESESSIONINSTANCE_ForDepartmentId",
                table: "SERVICESESESSIONINSTANCE",
                column: "ForDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SERVICESESESSIONINSTANCE_ForInstitutionId",
                table: "SERVICESESESSIONINSTANCE",
                column: "ForInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_SERVICESESESSIONINSTANCE_InstitutionId",
                table: "SERVICESESESSIONINSTANCE",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_SERVICESESESSIONINSTANCE_OriginatingSessionId",
                table: "SERVICESESESSIONINSTANCE",
                column: "OriginatingSessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SERVICESESESSIONINSTANCE_RetirerId",
                table: "SERVICESESESSIONINSTANCE",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_SERVICESESESSIONINSTANCE_SessionNumberGeneratorId",
                table: "SERVICESESESSIONINSTANCE",
                column: "SessionNumberGeneratorId");

            migrationBuilder.CreateIndex(
                name: "IX_SERVICESESESSIONINSTANCE_SpecialityId",
                table: "SERVICESESESSIONINSTANCE",
                column: "SpecialityId");

            migrationBuilder.CreateIndex(
                name: "IX_SERVICESESESSIONINSTANCE_StaffId",
                table: "SERVICESESESSIONINSTANCE",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_SHIFT_CreaterId",
                table: "SHIFT",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_SHIFT_InstitutionId",
                table: "SHIFT",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_SMS_BillId",
                table: "SMS",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_SMS_CreaterId",
                table: "SMS",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_SMS_DepartmentId",
                table: "SMS",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SMS_InstitutionId",
                table: "SMS",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_SMS_RetirerId",
                table: "SMS",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_SPECIALITY_CreaterId",
                table: "SPECIALITY",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_SPECIALITY_InstitutionId",
                table: "SPECIALITY",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_SPECIALITY_RetirerId",
                table: "SPECIALITY",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_STAFF_ChannelingDestinationDepartmentId",
                table: "STAFF",
                column: "ChannelingDestinationDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_STAFF_ChannelingDestinationInstitutionId",
                table: "STAFF",
                column: "ChannelingDestinationInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_STAFF_CreaterId",
                table: "STAFF",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_STAFF_DepartmentId",
                table: "STAFF",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_STAFF_DesignationId",
                table: "STAFF",
                column: "DesignationId");

            migrationBuilder.CreateIndex(
                name: "IX_STAFF_EditerId",
                table: "STAFF",
                column: "EditerId");

            migrationBuilder.CreateIndex(
                name: "IX_STAFF_GradeId",
                table: "STAFF",
                column: "GradeId");

            migrationBuilder.CreateIndex(
                name: "IX_STAFF_InstitutionId",
                table: "STAFF",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_STAFF_PersonId",
                table: "STAFF",
                column: "PersonId");

            migrationBuilder.CreateIndex(
                name: "IX_STAFF_RetirerId",
                table: "STAFF",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_STAFF_RosterId",
                table: "STAFF",
                column: "RosterId");

            migrationBuilder.CreateIndex(
                name: "IX_STAFF_SpecialityId",
                table: "STAFF",
                column: "SpecialityId");

            migrationBuilder.CreateIndex(
                name: "IX_STAFF_StaffCategoryId",
                table: "STAFF",
                column: "StaffCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_STAFF_WorkingDepartmentId",
                table: "STAFF",
                column: "WorkingDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_STAFFCATEGORY_CreaterId",
                table: "STAFFCATEGORY",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_STAFFSALARY_CreaterId",
                table: "STAFFSALARY",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_STAFFSALARY_DepartmentId",
                table: "STAFFSALARY",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_STAFFSALARY_EditerId",
                table: "STAFFSALARY",
                column: "EditerId");

            migrationBuilder.CreateIndex(
                name: "IX_STAFFSALARY_InstitutionId",
                table: "STAFFSALARY",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_STAFFSALARY_RetirerId",
                table: "STAFFSALARY",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_STAFFSALARY_StaffId",
                table: "STAFFSALARY",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_STOCK_CreaterId",
                table: "STOCK",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_STOCK_DepartmentId",
                table: "STOCK",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_STOCK_EditerId",
                table: "STOCK",
                column: "EditerId");

            migrationBuilder.CreateIndex(
                name: "IX_STOCK_ItemBatchId",
                table: "STOCK",
                column: "ItemBatchId");

            migrationBuilder.CreateIndex(
                name: "IX_STOCK_ItemId",
                table: "STOCK",
                column: "ItemId");

            migrationBuilder.CreateIndex(
                name: "IX_TOKEN_BillId",
                table: "TOKEN",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_TOKEN_CateroryId",
                table: "TOKEN",
                column: "CateroryId");

            migrationBuilder.CreateIndex(
                name: "IX_TOKEN_CounterId",
                table: "TOKEN",
                column: "CounterId");

            migrationBuilder.CreateIndex(
                name: "IX_TOKEN_CreatedById",
                table: "TOKEN",
                column: "CreatedById");

            migrationBuilder.CreateIndex(
                name: "IX_TOKEN_DepartmentId",
                table: "TOKEN",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TOKEN_DoctorId",
                table: "TOKEN",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_TOKEN_FromDepartmentId",
                table: "TOKEN",
                column: "FromDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TOKEN_FromInstitutionId",
                table: "TOKEN",
                column: "FromInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_TOKEN_FromStaffId",
                table: "TOKEN",
                column: "FromStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_TOKEN_InstitutionId",
                table: "TOKEN",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_TOKEN_PatientEncounterId",
                table: "TOKEN",
                column: "PatientEncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_TOKEN_PatientId",
                table: "TOKEN",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_TOKEN_ReferaTokenId",
                table: "TOKEN",
                column: "ReferaTokenId");

            migrationBuilder.CreateIndex(
                name: "IX_TOKEN_RetiredById",
                table: "TOKEN",
                column: "RetiredById");

            migrationBuilder.CreateIndex(
                name: "IX_TOKEN_ServiceCounterId",
                table: "TOKEN",
                column: "ServiceCounterId");

            migrationBuilder.CreateIndex(
                name: "IX_TOKEN_StaffId",
                table: "TOKEN",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_TOKEN_ToDepartmentId",
                table: "TOKEN",
                column: "ToDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TOKEN_ToInstitutionId",
                table: "TOKEN",
                column: "ToInstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_TOKEN_ToStaffId",
                table: "TOKEN",
                column: "ToStaffId");

            migrationBuilder.CreateIndex(
                name: "IX_TRIGGERSUBSCRIPTION_CreaterId",
                table: "TRIGGERSUBSCRIPTION",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_TRIGGERSUBSCRIPTION_DepartmentId",
                table: "TRIGGERSUBSCRIPTION",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_TRIGGERSUBSCRIPTION_EditerId",
                table: "TRIGGERSUBSCRIPTION",
                column: "EditerId");

            migrationBuilder.CreateIndex(
                name: "IX_TRIGGERSUBSCRIPTION_RetirerId",
                table: "TRIGGERSUBSCRIPTION",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_TRIGGERSUBSCRIPTION_WebUserId",
                table: "TRIGGERSUBSCRIPTION",
                column: "WebUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TRIGGERSUBSCRIPTION_WebUserRoleId",
                table: "TRIGGERSUBSCRIPTION",
                column: "WebUserRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_UPLOAD_CategoryId",
                table: "UPLOAD",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_UPLOAD_CreaterId",
                table: "UPLOAD",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_UPLOAD_InstitutionId",
                table: "UPLOAD",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_UPLOAD_RetirerId",
                table: "UPLOAD",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_UPLOAD_WebContentId",
                table: "UPLOAD",
                column: "WebContentId");

            migrationBuilder.CreateIndex(
                name: "IX_USERICON_DepartmentId",
                table: "USERICON",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_USERICON_WebUserId",
                table: "USERICON",
                column: "WebUserId");

            migrationBuilder.CreateIndex(
                name: "IX_USERICON_WebUserRoleId",
                table: "USERICON",
                column: "WebUserRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_USERNOTIFICATION_CreaterId",
                table: "USERNOTIFICATION",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_USERNOTIFICATION_NotificationId",
                table: "USERNOTIFICATION",
                column: "NotificationId");

            migrationBuilder.CreateIndex(
                name: "IX_USERNOTIFICATION_RetirerId",
                table: "USERNOTIFICATION",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_USERNOTIFICATION_ToDepartmentId",
                table: "USERNOTIFICATION",
                column: "ToDepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_USERNOTIFICATION_WebUserId",
                table: "USERNOTIFICATION",
                column: "WebUserId");

            migrationBuilder.CreateIndex(
                name: "IX_USERPREFERENCE_DepartmentId",
                table: "USERPREFERENCE",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_USERPREFERENCE_InstitutionId",
                table: "USERPREFERENCE",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_USERPREFERENCE_WebUserId",
                table: "USERPREFERENCE",
                column: "WebUserId");

            migrationBuilder.CreateIndex(
                name: "IX_VMP_CreaterId",
                table: "VMP",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_VMP_VtmId",
                table: "VMP",
                column: "VtmId");

            migrationBuilder.CreateIndex(
                name: "IX_VMPP_CreaterId",
                table: "VMPP",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_VMPP_VmpId",
                table: "VMPP",
                column: "VmpId");

            migrationBuilder.CreateIndex(
                name: "IX_VTM_CreaterId",
                table: "VTM",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBCONTENT_ParentId",
                table: "WEBCONTENT",
                column: "ParentId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBCONTENT_WebLanguageId",
                table: "WEBCONTENT",
                column: "WebLanguageId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBTHEME_CreaterId",
                table: "WEBTHEME",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBTHEME_RetirerId",
                table: "WEBTHEME",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSER_ActivatorId",
                table: "WEBUSER",
                column: "ActivatorId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSER_CreaterId",
                table: "WEBUSER",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSER_DepartmentId",
                table: "WEBUSER",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSER_InstitutionId",
                table: "WEBUSER",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSER_RetirerId",
                table: "WEBUSER",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSER_SelfRoleUserId",
                table: "WEBUSER",
                column: "SelfRoleUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSER_StaffId",
                table: "WEBUSER",
                column: "StaffId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSER_WebUserPersonId",
                table: "WEBUSER",
                column: "WebUserPersonId",
                unique: true,
                filter: "[WebUserPersonId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSER_WebUserRoleUserId",
                table: "WEBUSER",
                column: "WebUserRoleUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSERDASHBOARD_CreaterId",
                table: "WEBUSERDASHBOARD",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSERDASHBOARD_RetirerId",
                table: "WEBUSERDASHBOARD",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSERDASHBOARD_WebUserId",
                table: "WEBUSERDASHBOARD",
                column: "WebUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSERDEPARTMENT_DepartmentId",
                table: "WEBUSERDEPARTMENT",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSERDEPARTMENT_WebUserId",
                table: "WEBUSERDEPARTMENT",
                column: "WebUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSERPAYMENTSCHEME_CreaterId",
                table: "WEBUSERPAYMENTSCHEME",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSERPAYMENTSCHEME_PaymentSchemeId",
                table: "WEBUSERPAYMENTSCHEME",
                column: "PaymentSchemeId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSERPAYMENTSCHEME_RetirerId",
                table: "WEBUSERPAYMENTSCHEME",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSERPAYMENTSCHEME_WebUserId",
                table: "WEBUSERPAYMENTSCHEME",
                column: "WebUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSERPRIVILEGE_WebUserRoleId",
                table: "WEBUSERPRIVILEGE",
                column: "WebUserRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSERROLE_CreaterId",
                table: "WEBUSERROLE",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSERROLE_RetirerId",
                table: "WEBUSERROLE",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSERROLEPRIVILEGE_CreaterId",
                table: "WEBUSERROLEPRIVILEGE",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSERROLEPRIVILEGE_DepartmentId",
                table: "WEBUSERROLEPRIVILEGE",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSERROLEPRIVILEGE_RetirerId",
                table: "WEBUSERROLEPRIVILEGE",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSERROLEPRIVILEGE_WebUserRoleId",
                table: "WEBUSERROLEPRIVILEGE",
                column: "WebUserRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSERROLEUSER_WebUserId",
                table: "WEBUSERROLEUSER",
                column: "WebUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSERROLEUSER_WebUserRoleId",
                table: "WEBUSERROLEUSER",
                column: "WebUserRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSERROUTE_CreaterId",
                table: "WEBUSERROUTE",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSERROUTE_RetirerId",
                table: "WEBUSERROUTE",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSERROUTE_RouteId",
                table: "WEBUSERROUTE",
                column: "RouteId");

            migrationBuilder.CreateIndex(
                name: "IX_WEBUSERROUTE_WebUserId",
                table: "WEBUSERROUTE",
                column: "WebUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_ADMISSION_PATIENTENCOUNTER_PatientEncounterId",
                table: "ADMISSION",
                column: "PatientEncounterId",
                principalTable: "PATIENTENCOUNTER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ADMISSION_WEBUSER_CreaterId",
                table: "ADMISSION",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AGENTHISTORY_BILLITEM_BillItemId",
                table: "AGENTHISTORY",
                column: "BillItemId",
                principalTable: "BILLITEM",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AGENTHISTORY_BILLSESSION_BillSessionId",
                table: "AGENTHISTORY",
                column: "BillSessionId",
                principalTable: "BILLSESSION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AGENTHISTORY_BILL_BillId",
                table: "AGENTHISTORY",
                column: "BillId",
                principalTable: "BILL",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AGENTHISTORY_DEPARTMENT_DepartmentId",
                table: "AGENTHISTORY",
                column: "DepartmentId",
                principalTable: "DEPARTMENT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AGENTHISTORY_INSTITUTION_AgencyId",
                table: "AGENTHISTORY",
                column: "AgencyId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AGENTHISTORY_INSTITUTION_InstitutionId",
                table: "AGENTHISTORY",
                column: "InstitutionId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AGENTHISTORY_WEBUSER_CreaterId",
                table: "AGENTHISTORY",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AGENTHISTORY_WEBUSER_RetirerId",
                table: "AGENTHISTORY",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AMP_WEBUSER_CreaterId",
                table: "AMP",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AMP_WEBUSER_EditerId",
                table: "AMP",
                column: "EditerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_APIKEY_INSTITUTION_InstitutionId",
                table: "APIKEY",
                column: "InstitutionId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_APIKEY_WEBUSER_CreaterId",
                table: "APIKEY",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_APPEMAIL_BILL_BillId",
                table: "APPEMAIL",
                column: "BillId",
                principalTable: "BILL",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_APPEMAIL_DEPARTMENT_DepartmentId",
                table: "APPEMAIL",
                column: "DepartmentId",
                principalTable: "DEPARTMENT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_APPEMAIL_INSTITUTION_InstitutionId",
                table: "APPEMAIL",
                column: "InstitutionId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_APPEMAIL_WEBUSER_CreaterId",
                table: "APPEMAIL",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_APPEMAIL_WEBUSER_RetirerId",
                table: "APPEMAIL",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AREA_INSTITUTION_InstitutionId",
                table: "AREA",
                column: "InstitutionId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AREA_WEBUSER_CreaterId",
                table: "AREA",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_AREA_WEBUSER_RetirerId",
                table: "AREA",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILL_CASHTRANSACTION_CashTransactionId",
                table: "BILL",
                column: "CashTransactionId",
                principalTable: "CASHTRANSACTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILL_CATEGORY_CategoryId",
                table: "BILL",
                column: "CategoryId",
                principalTable: "CATEGORY",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILL_DEPARTMENT_DepartmentId",
                table: "BILL",
                column: "DepartmentId",
                principalTable: "DEPARTMENT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILL_DEPARTMENT_FromDepartmentId",
                table: "BILL",
                column: "FromDepartmentId",
                principalTable: "DEPARTMENT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILL_DEPARTMENT_ToDepartmentId",
                table: "BILL",
                column: "ToDepartmentId",
                principalTable: "DEPARTMENT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILL_INSTITUTION_BankId",
                table: "BILL",
                column: "BankId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILL_INSTITUTION_CollectingCentreId",
                table: "BILL",
                column: "CollectingCentreId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILL_INSTITUTION_CreditCompanyId",
                table: "BILL",
                column: "CreditCompanyId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILL_INSTITUTION_FromInstitutionId",
                table: "BILL",
                column: "FromInstitutionId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILL_INSTITUTION_InstitutionId",
                table: "BILL",
                column: "InstitutionId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILL_INSTITUTION_PaymentSchemeInstitutionId",
                table: "BILL",
                column: "PaymentSchemeInstitutionId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILL_INSTITUTION_ReferenceInstitutionId",
                table: "BILL",
                column: "ReferenceInstitutionId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILL_INSTITUTION_ReferredByInstitutionId",
                table: "BILL",
                column: "ReferredByInstitutionId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILL_INSTITUTION_ToInstitutionId",
                table: "BILL",
                column: "ToInstitutionId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILL_ITEM_ItemId",
                table: "BILL",
                column: "ItemId",
                principalTable: "ITEM",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILL_MEMBERSHIPSCHEME_MembershipSchemeId",
                table: "BILL",
                column: "MembershipSchemeId",
                principalTable: "MEMBERSHIPSCHEME",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILL_PATIENTENCOUNTER_PatientEncounterId",
                table: "BILL",
                column: "PatientEncounterId",
                principalTable: "PATIENTENCOUNTER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILL_PATIENT_PatientId",
                table: "BILL",
                column: "PatientId",
                principalTable: "PATIENT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILL_PAYMENTSCHEME_PaymentSchemeId",
                table: "BILL",
                column: "PaymentSchemeId",
                principalTable: "PAYMENTSCHEME",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILL_PERSON_PersonId",
                table: "BILL",
                column: "PersonId",
                principalTable: "PERSON",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILL_STAFF_ReferredById",
                table: "BILL",
                column: "ReferredById",
                principalTable: "STAFF",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILL_STAFF_StaffId",
                table: "BILL",
                column: "StaffId",
                principalTable: "STAFF",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILL_WEBUSER_CheckedById",
                table: "BILL",
                column: "CheckedById",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILL_WEBUSER_CreaterId",
                table: "BILL",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILL_WEBUSER_EditorId",
                table: "BILL",
                column: "EditorId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILL_WEBUSER_RetirerId",
                table: "BILL",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLCOMPONENT_BILLITEM_BillItemId",
                table: "BILLCOMPONENT",
                column: "BillItemId",
                principalTable: "BILLITEM",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLCOMPONENT_DEPARTMENT_DepartmentId",
                table: "BILLCOMPONENT",
                column: "DepartmentId",
                principalTable: "DEPARTMENT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLCOMPONENT_INSTITUTION_InstitutionId",
                table: "BILLCOMPONENT",
                column: "InstitutionId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLCOMPONENT_ITEM_ItemId",
                table: "BILLCOMPONENT",
                column: "ItemId",
                principalTable: "ITEM",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLCOMPONENT_ITEM_PackegeId",
                table: "BILLCOMPONENT",
                column: "PackegeId",
                principalTable: "ITEM",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLCOMPONENT_SPECIALITY_SpecialityId",
                table: "BILLCOMPONENT",
                column: "SpecialityId",
                principalTable: "SPECIALITY",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLCOMPONENT_STAFF_StaffId",
                table: "BILLCOMPONENT",
                column: "StaffId",
                principalTable: "STAFF",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLCOMPONENT_WEBUSER_CreaterId",
                table: "BILLCOMPONENT",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLCOMPONENT_WEBUSER_RetirerId",
                table: "BILLCOMPONENT",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLFEE_BILLITEM_BillItemId",
                table: "BILLFEE",
                column: "BillItemId",
                principalTable: "BILLITEM",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLFEE_BILLITEM_ReferenceBillItemId",
                table: "BILLFEE",
                column: "ReferenceBillItemId",
                principalTable: "BILLITEM",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLFEE_DEPARTMENT_DepartmentId",
                table: "BILLFEE",
                column: "DepartmentId",
                principalTable: "DEPARTMENT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLFEE_FEE_FeeId",
                table: "BILLFEE",
                column: "FeeId",
                principalTable: "FEE",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLFEE_INSTITUTION_InstitutionId",
                table: "BILLFEE",
                column: "InstitutionId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLFEE_PATIENTENCOUNTER_ChildEncounterId",
                table: "BILLFEE",
                column: "ChildEncounterId",
                principalTable: "PATIENTENCOUNTER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLFEE_PATIENTENCOUNTER_PatienEncounterId",
                table: "BILLFEE",
                column: "PatienEncounterId",
                principalTable: "PATIENTENCOUNTER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLFEE_PATIENTROOM_ReferencePatientRoomId",
                table: "BILLFEE",
                column: "ReferencePatientRoomId",
                principalTable: "PATIENTROOM",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLFEE_PATIENT_PatientId",
                table: "BILLFEE",
                column: "PatientId",
                principalTable: "PATIENT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLFEE_PRICEMATRIX_PriceMatrixId",
                table: "BILLFEE",
                column: "PriceMatrixId",
                principalTable: "PRICEMATRIX",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLFEE_SPECIALITY_SpecialityId",
                table: "BILLFEE",
                column: "SpecialityId",
                principalTable: "SPECIALITY",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLFEE_STAFF_StaffId",
                table: "BILLFEE",
                column: "StaffId",
                principalTable: "STAFF",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLFEE_WEBUSER_CreaterId",
                table: "BILLFEE",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLFEE_WEBUSER_EditorId",
                table: "BILLFEE",
                column: "EditorId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLFEE_WEBUSER_RetirerId",
                table: "BILLFEE",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLFEEPAYMENT_DEPARTMENT_DepartmentId",
                table: "BILLFEEPAYMENT",
                column: "DepartmentId",
                principalTable: "DEPARTMENT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLFEEPAYMENT_INSTITUTION_InstitutionId",
                table: "BILLFEEPAYMENT",
                column: "InstitutionId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLFEEPAYMENT_PAYMENT_PaymentId",
                table: "BILLFEEPAYMENT",
                column: "PaymentId",
                principalTable: "PAYMENT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLFEEPAYMENT_WEBUSER_CreaterId",
                table: "BILLFEEPAYMENT",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLFEEPAYMENT_WEBUSER_RetirerId",
                table: "BILLFEEPAYMENT",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLITEM_ITEM_ItemId1",
                table: "BILLITEM",
                column: "ItemId1",
                principalTable: "ITEM",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLITEM_PATIENTENCOUNTER_PatientEncounterId",
                table: "BILLITEM",
                column: "PatientEncounterId",
                principalTable: "PATIENTENCOUNTER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLITEM_PRICEMATRIX_PriceMatrixId",
                table: "BILLITEM",
                column: "PriceMatrixId",
                principalTable: "PRICEMATRIX",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLITEM_WEBUSER_CreaterId",
                table: "BILLITEM",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLITEM_WEBUSER_EditorId",
                table: "BILLITEM",
                column: "EditorId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLITEM_WEBUSER_RetirerId",
                table: "BILLITEM",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLNUMBER_DEPARTMENT_DepartmentId",
                table: "BILLNUMBER",
                column: "DepartmentId",
                principalTable: "DEPARTMENT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLNUMBER_DEPARTMENT_ToDepartmentId",
                table: "BILLNUMBER",
                column: "ToDepartmentId",
                principalTable: "DEPARTMENT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLNUMBER_INSTITUTION_InstitutionId",
                table: "BILLNUMBER",
                column: "InstitutionId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLNUMBER_WEBUSER_RetirerId",
                table: "BILLNUMBER",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_BILLSESSION_WEBUSER_CreaterId",
                table: "BILLSESSION",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CASHBOOK_DEPARTMENT_DepartmentId",
                table: "CASHBOOK",
                column: "DepartmentId",
                principalTable: "DEPARTMENT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CASHBOOK_INSTITUTION_InstitutionId",
                table: "CASHBOOK",
                column: "InstitutionId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CASHBOOK_WEBUSER_CreaterId",
                table: "CASHBOOK",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CASHBOOK_WEBUSER_RetirerId",
                table: "CASHBOOK",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CASHBOOKENTRY_DEPARTMENT_DepartmentId",
                table: "CASHBOOKENTRY",
                column: "DepartmentId",
                principalTable: "DEPARTMENT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CASHBOOKENTRY_INSTITUTION_InstitutionId",
                table: "CASHBOOKENTRY",
                column: "InstitutionId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CASHBOOKENTRY_WEBUSER_CreaterId",
                table: "CASHBOOKENTRY",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CASHTRANSACTION_DEPARTMENT_DepartmentId",
                table: "CASHTRANSACTION",
                column: "DepartmentId",
                principalTable: "DEPARTMENT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CASHTRANSACTION_DRAWER_DrawerId",
                table: "CASHTRANSACTION",
                column: "DrawerId",
                principalTable: "DRAWER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CASHTRANSACTION_INSTITUTION_InstitutionId",
                table: "CASHTRANSACTION",
                column: "InstitutionId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CASHTRANSACTION_WEBUSER_CreaterId",
                table: "CASHTRANSACTION",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CATEGORY_INSTITUTION_InstitutionId",
                table: "CATEGORY",
                column: "InstitutionId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CATEGORY_PAYMENTSCHEME_PaymentSchemeId",
                table: "CATEGORY",
                column: "PaymentSchemeId",
                principalTable: "PAYMENTSCHEME",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CATEGORY_SPECIALITY_SpecialityId",
                table: "CATEGORY",
                column: "SpecialityId",
                principalTable: "SPECIALITY",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CATEGORY_STAFF_StaffId",
                table: "CATEGORY",
                column: "StaffId",
                principalTable: "STAFF",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CATEGORY_WEBUSER_CreaterId",
                table: "CATEGORY",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CATEGORY_WEBUSER_EditerId",
                table: "CATEGORY",
                column: "EditerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CATEGORY_WEBUSER_RetirerId",
                table: "CATEGORY",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CATEGORYITEM_ITEM_ItemId",
                table: "CATEGORYITEM",
                column: "ItemId",
                principalTable: "ITEM",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CATEGORYITEM_WEBUSER_CreaterId",
                table: "CATEGORYITEM",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CATEGORYITEM_WEBUSER_RetirerId",
                table: "CATEGORYITEM",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CONFIGOPTION_DEPARTMENT_DepartmentId",
                table: "CONFIGOPTION",
                column: "DepartmentId",
                principalTable: "DEPARTMENT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CONFIGOPTION_INSTITUTION_InstitutionId",
                table: "CONFIGOPTION",
                column: "InstitutionId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_CONFIGOPTION_WEBUSER_CreaterId",
                table: "CONFIGOPTION",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DEPARTMENT_INSTITUTION_InstitutionId",
                table: "DEPARTMENT",
                column: "InstitutionId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DEPARTMENT_INSTITUTION_LabInstitutionId",
                table: "DEPARTMENT",
                column: "LabInstitutionId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DEPARTMENT_INSTITUTION_SiteId",
                table: "DEPARTMENT",
                column: "SiteId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DEPARTMENT_WEBUSER_CreaterId",
                table: "DEPARTMENT",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DEPARTMENT_WEBUSER_EditerId",
                table: "DEPARTMENT",
                column: "EditerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DEPARTMENT_WEBUSER_RetirerId",
                table: "DEPARTMENT",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DESIGNATION_WEBUSER_CreaterId",
                table: "DESIGNATION",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DRAWER_INSTITUTION_InstitutionId",
                table: "DRAWER",
                column: "InstitutionId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DRAWER_WEBUSER_CashierId",
                table: "DRAWER",
                column: "CashierId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DRAWER_WEBUSER_CreaterId",
                table: "DRAWER",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ENCOUNTERCREDITCOMPANY_INSTITUTION_InstitutionId",
                table: "ENCOUNTERCREDITCOMPANY",
                column: "InstitutionId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ENCOUNTERCREDITCOMPANY_PATIENTENCOUNTER_PatientEncounterId",
                table: "ENCOUNTERCREDITCOMPANY",
                column: "PatientEncounterId",
                principalTable: "PATIENTENCOUNTER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ENCOUNTERCREDITCOMPANY_WEBUSER_CreaterId",
                table: "ENCOUNTERCREDITCOMPANY",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ENCOUNTERCREDITCOMPANY_WEBUSER_RetirerId",
                table: "ENCOUNTERCREDITCOMPANY",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FAMILY_INSTITUTION_CreatedInstitutionId",
                table: "FAMILY",
                column: "CreatedInstitutionId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FAMILY_MEMBERSHIPSCHEME_MembershipSchemeId",
                table: "FAMILY",
                column: "MembershipSchemeId",
                principalTable: "MEMBERSHIPSCHEME",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FAMILY_WEBUSER_CreaterId",
                table: "FAMILY",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FAMILY_WEBUSER_EditerId",
                table: "FAMILY",
                column: "EditerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FAMILY_WEBUSER_RetirerId",
                table: "FAMILY",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FAMILYMEMBER_PATIENT_PatientId",
                table: "FAMILYMEMBER",
                column: "PatientId",
                principalTable: "PATIENT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FAMILYMEMBER_WEBUSER_CreaterId",
                table: "FAMILYMEMBER",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FAMILYMEMBER_WEBUSER_EditerId",
                table: "FAMILYMEMBER",
                column: "EditerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FAMILYMEMBER_WEBUSER_RetirerId",
                table: "FAMILYMEMBER",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FEE_INSTITUTION_InstitutionId",
                table: "FEE",
                column: "InstitutionId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FEE_ITEM_ItemId",
                table: "FEE",
                column: "ItemId",
                principalTable: "ITEM",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FEE_SPECIALITY_SpecialityId",
                table: "FEE",
                column: "SpecialityId",
                principalTable: "SPECIALITY",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FEE_STAFF_StaffId",
                table: "FEE",
                column: "StaffId",
                principalTable: "STAFF",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FEE_WEBUSER_CreaterId",
                table: "FEE",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FEE_WEBUSER_EditerId",
                table: "FEE",
                column: "EditerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FEE_WEBUSER_RetirerId",
                table: "FEE",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FEECHANGE_WEBUSER_CreaterId",
                table: "FEECHANGE",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FEECHANGE_WEBUSER_RetirerId",
                table: "FEECHANGE",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FEEVALUE_INSTITUTION_InstitutionId",
                table: "FEEVALUE",
                column: "InstitutionId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FEEVALUE_ITEM_ItemId",
                table: "FEEVALUE",
                column: "ItemId",
                principalTable: "ITEM",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FEEVALUE_WEBUSER_CreaterId",
                table: "FEEVALUE",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FEEVALUE_WEBUSER_RetirerId",
                table: "FEEVALUE",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FORM_WEBUSER_CreaterId",
                table: "FORM",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FORM_WEBUSER_EditorId",
                table: "FORM",
                column: "EditorId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FORM_WEBUSER_RetirerId",
                table: "FORM",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_GRADE_WEBUSER_CreaterId",
                table: "GRADE",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_INSTITUTION_PERSON_ContactPersonId",
                table: "INSTITUTION",
                column: "ContactPersonId",
                principalTable: "PERSON",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_INSTITUTION_ROUTES_RouteId",
                table: "INSTITUTION",
                column: "RouteId",
                principalTable: "ROUTES",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_INSTITUTION_WEBUSER_CreaterId",
                table: "INSTITUTION",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_INSTITUTION_WEBUSER_EditerId",
                table: "INSTITUTION",
                column: "EditerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_INSTITUTION_WEBUSER_RetirerId",
                table: "INSTITUTION",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ISSUERATEMARGINS_WEBUSER_CreaterId",
                table: "ISSUERATEMARGINS",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ISSUERATEMARGINS_WEBUSER_RetirerId",
                table: "ISSUERATEMARGINS",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ITEM_MACHINE_MachineId",
                table: "ITEM",
                column: "MachineId",
                principalTable: "MACHINE",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ITEM_MEASUREMENTUNIT_BaseUnitId",
                table: "ITEM",
                column: "BaseUnitId",
                principalTable: "MEASUREMENTUNIT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ITEM_MEASUREMENTUNIT_IssueUnitId",
                table: "ITEM",
                column: "IssueUnitId",
                principalTable: "MEASUREMENTUNIT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ITEM_MEASUREMENTUNIT_MeasurementUnitId",
                table: "ITEM",
                column: "MeasurementUnitId",
                principalTable: "MEASUREMENTUNIT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ITEM_MEASUREMENTUNIT_StrengthUnitId",
                table: "ITEM",
                column: "StrengthUnitId",
                principalTable: "MEASUREMENTUNIT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ITEM_SPECIALITY_SpecialityId",
                table: "ITEM",
                column: "SpecialityId",
                principalTable: "SPECIALITY",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ITEM_STAFF_StaffId",
                table: "ITEM",
                column: "StaffId",
                principalTable: "STAFF",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ITEM_VMP_VmpId",
                table: "ITEM",
                column: "VmpId",
                principalTable: "VMP",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ITEM_WEBUSER_CreaterId",
                table: "ITEM",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ITEM_WEBUSER_EditerId",
                table: "ITEM",
                column: "EditerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ITEM_WEBUSER_RetirerId",
                table: "ITEM",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ITEMBATCH_WEBUSER_CreaterId",
                table: "ITEMBATCH",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ITEMBATCH_WEBUSER_EditerId",
                table: "ITEMBATCH",
                column: "EditerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ITEMBATCH_WEBUSER_RetirerId",
                table: "ITEMBATCH",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ITEMMAPPING_WEBUSER_CreaterId",
                table: "ITEMMAPPING",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ITEMMAPPING_WEBUSER_RetirerId",
                table: "ITEMMAPPING",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ITEMSCATEGORIES_WEBUSER_CreaterId",
                table: "ITEMSCATEGORIES",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ITEMSCATEGORIES_WEBUSER_RetirerId",
                table: "ITEMSCATEGORIES",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_LOGINS_WEBUSER_WebUserId",
                table: "LOGINS",
                column: "WebUserId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MACHINE_WEBUSER_CreaterId",
                table: "MACHINE",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MEASUREMENTUNIT_WEBUSER_CreaterId",
                table: "MEASUREMENTUNIT",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MEASUREMENTUNIT_WEBUSER_EditerId",
                table: "MEASUREMENTUNIT",
                column: "EditerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MEASUREMENTUNIT_WEBUSER_RetirerId",
                table: "MEASUREMENTUNIT",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MEDICALPACKAGEITEM_WEBUSER_CreaterId",
                table: "MEDICALPACKAGEITEM",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MEDICALPACKAGEITEM_WEBUSER_RetirerId",
                table: "MEDICALPACKAGEITEM",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MEMBERSHIPSCHEME_WEBUSER_CreaterId",
                table: "MEMBERSHIPSCHEME",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_MEMBERSHIPSCHEME_WEBUSER_RetirerId",
                table: "MEMBERSHIPSCHEME",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NOTIFICATION_WEBUSER_CreaterId",
                table: "NOTIFICATION",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NOTIFICATION_WEBUSER_RetirerId",
                table: "NOTIFICATION",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PACKAGEITEM_WEBUSER_CreaterId",
                table: "PACKAGEITEM",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PACKAGEITEM_WEBUSER_RetirerId",
                table: "PACKAGEITEM",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PATIENT_PERSON_PersonId",
                table: "PATIENT",
                column: "PersonId",
                principalTable: "PERSON",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PATIENT_WEBUSER_CreaterId",
                table: "PATIENT",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PATIENT_WEBUSER_EditerId",
                table: "PATIENT",
                column: "EditerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PATIENT_WEBUSER_RetirerId",
                table: "PATIENT",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PATIENTDEPOSIT_WEBUSER_CreaterId",
                table: "PATIENTDEPOSIT",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PATIENTDEPOSIT_WEBUSER_RetirerId",
                table: "PATIENTDEPOSIT",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PATIENTDEPOSITHISTORY_WEBUSER_CreaterId",
                table: "PATIENTDEPOSITHISTORY",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PATIENTDEPOSITHISTORY_WEBUSER_RetirerId",
                table: "PATIENTDEPOSITHISTORY",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PATIENTENCOUNTER_STAFF_ReferringStaffId",
                table: "PATIENTENCOUNTER",
                column: "ReferringStaffId",
                principalTable: "STAFF",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PATIENTENCOUNTER_WEBUSER_CreaterId",
                table: "PATIENTENCOUNTER",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PATIENTENCOUNTER_WEBUSER_EditerId",
                table: "PATIENTENCOUNTER",
                column: "EditerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PATIENTENCOUNTER_WEBUSER_RetirerId",
                table: "PATIENTENCOUNTER",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PATIENTFLAG_WEBUSER_CreaterId",
                table: "PATIENTFLAG",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PATIENTFLAG_WEBUSER_RetirerId",
                table: "PATIENTFLAG",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PATIENTITEM_WEBUSER_CreaterId",
                table: "PATIENTITEM",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PATIENTITEM_WEBUSER_RetirerId",
                table: "PATIENTITEM",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PATIENTROOM_ROOM_RoomId",
                table: "PATIENTROOM",
                column: "RoomId",
                principalTable: "ROOM",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PATIENTROOM_WEBUSER_CreaterId",
                table: "PATIENTROOM",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PAYMENT_WEBUSER_CreaterId",
                table: "PAYMENT",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PAYMENT_WEBUSER_RealizerId",
                table: "PAYMENT",
                column: "RealizerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PAYMENT_WEBUSER_RetirerId",
                table: "PAYMENT",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PAYMENTGATEWAYTRANSACTION_WEBUSER_CreaterId",
                table: "PAYMENTGATEWAYTRANSACTION",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PAYMENTGATEWAYTRANSACTION_WEBUSER_RetirerId",
                table: "PAYMENTGATEWAYTRANSACTION",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PAYMENTSCHEME_WEBUSER_CreaterId",
                table: "PAYMENTSCHEME",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PAYMENTSCHEME_WEBUSER_RetirerId",
                table: "PAYMENTSCHEME",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PERSON_WEBUSER_CreaterId",
                table: "PERSON",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PERSON_WEBUSER_EditerId",
                table: "PERSON",
                column: "EditerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PERSON_WEBUSER_RetirerId",
                table: "PERSON",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PERSONINSTITUTION_STAFF_StaffId",
                table: "PERSONINSTITUTION",
                column: "StaffId",
                principalTable: "STAFF",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PERSONINSTITUTION_WEBUSER_CreaterId",
                table: "PERSONINSTITUTION",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PERSONINSTITUTION_WEBUSER_RetirerId",
                table: "PERSONINSTITUTION",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PHARMACEUTICALBILLITEM_WEBUSER_CreaterId",
                table: "PHARMACEUTICALBILLITEM",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PRICEMATRIX_WEBUSER_CreaterId",
                table: "PRICEMATRIX",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PRICEMATRIX_WEBUSER_EditerId",
                table: "PRICEMATRIX",
                column: "EditerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PRICEMATRIX_WEBUSER_RetirerId",
                table: "PRICEMATRIX",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_REPORTTEMPLATE_WEBUSER_CreaterId",
                table: "REPORTTEMPLATE",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_REPORTTEMPLATE_WEBUSER_RetirerId",
                table: "REPORTTEMPLATE",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ROOM_ROOMCATEGORY_RoomCategoryId",
                table: "ROOM",
                column: "RoomCategoryId",
                principalTable: "ROOMCATEGORY",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ROOM_WEBUSER_CreaterId",
                table: "ROOM",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ROOMCATEGORY_WEBUSER_CreaterId",
                table: "ROOMCATEGORY",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ROSTER_WEBUSER_CreaterId",
                table: "ROSTER",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ROUTES_WEBUSER_CreaterId",
                table: "ROUTES",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ROUTES_WEBUSER_RetirerId",
                table: "ROUTES",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SERVICESESESSIONINSTANCE_SPECIALITY_SpecialityId",
                table: "SERVICESESESSIONINSTANCE",
                column: "SpecialityId",
                principalTable: "SPECIALITY",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SERVICESESESSIONINSTANCE_STAFF_StaffId",
                table: "SERVICESESESSIONINSTANCE",
                column: "StaffId",
                principalTable: "STAFF",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SERVICESESESSIONINSTANCE_WEBUSER_CreaterId",
                table: "SERVICESESESSIONINSTANCE",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SERVICESESESSIONINSTANCE_WEBUSER_EditerId",
                table: "SERVICESESESSIONINSTANCE",
                column: "EditerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SERVICESESESSIONINSTANCE_WEBUSER_RetirerId",
                table: "SERVICESESESSIONINSTANCE",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SHIFT_WEBUSER_CreaterId",
                table: "SHIFT",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SMS_WEBUSER_CreaterId",
                table: "SMS",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SMS_WEBUSER_RetirerId",
                table: "SMS",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SPECIALITY_WEBUSER_CreaterId",
                table: "SPECIALITY",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SPECIALITY_WEBUSER_RetirerId",
                table: "SPECIALITY",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_STAFF_WEBUSER_CreaterId",
                table: "STAFF",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_STAFF_WEBUSER_EditerId",
                table: "STAFF",
                column: "EditerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_STAFF_WEBUSER_RetirerId",
                table: "STAFF",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_STAFFCATEGORY_WEBUSER_CreaterId",
                table: "STAFFCATEGORY",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_STAFFSALARY_WEBUSER_CreaterId",
                table: "STAFFSALARY",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_STAFFSALARY_WEBUSER_EditerId",
                table: "STAFFSALARY",
                column: "EditerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_STAFFSALARY_WEBUSER_RetirerId",
                table: "STAFFSALARY",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_STOCK_WEBUSER_CreaterId",
                table: "STOCK",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_STOCK_WEBUSER_EditerId",
                table: "STOCK",
                column: "EditerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TOKEN_WEBUSER_CreatedById",
                table: "TOKEN",
                column: "CreatedById",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TOKEN_WEBUSER_RetiredById",
                table: "TOKEN",
                column: "RetiredById",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TRIGGERSUBSCRIPTION_WEBUSERROLE_WebUserRoleId",
                table: "TRIGGERSUBSCRIPTION",
                column: "WebUserRoleId",
                principalTable: "WEBUSERROLE",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TRIGGERSUBSCRIPTION_WEBUSER_CreaterId",
                table: "TRIGGERSUBSCRIPTION",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TRIGGERSUBSCRIPTION_WEBUSER_EditerId",
                table: "TRIGGERSUBSCRIPTION",
                column: "EditerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TRIGGERSUBSCRIPTION_WEBUSER_RetirerId",
                table: "TRIGGERSUBSCRIPTION",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_TRIGGERSUBSCRIPTION_WEBUSER_WebUserId",
                table: "TRIGGERSUBSCRIPTION",
                column: "WebUserId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UPLOAD_WEBUSER_CreaterId",
                table: "UPLOAD",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UPLOAD_WEBUSER_RetirerId",
                table: "UPLOAD",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_USERICON_WEBUSERROLE_WebUserRoleId",
                table: "USERICON",
                column: "WebUserRoleId",
                principalTable: "WEBUSERROLE",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_USERICON_WEBUSER_WebUserId",
                table: "USERICON",
                column: "WebUserId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_USERNOTIFICATION_WEBUSER_CreaterId",
                table: "USERNOTIFICATION",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_USERNOTIFICATION_WEBUSER_RetirerId",
                table: "USERNOTIFICATION",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_USERNOTIFICATION_WEBUSER_WebUserId",
                table: "USERNOTIFICATION",
                column: "WebUserId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_USERPREFERENCE_WEBUSER_WebUserId",
                table: "USERPREFERENCE",
                column: "WebUserId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VMP_VTM_VtmId",
                table: "VMP",
                column: "VtmId",
                principalTable: "VTM",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VMP_WEBUSER_CreaterId",
                table: "VMP",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VMPP_WEBUSER_CreaterId",
                table: "VMPP",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_VTM_WEBUSER_CreaterId",
                table: "VTM",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WEBTHEME_WEBUSER_CreaterId",
                table: "WEBTHEME",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WEBTHEME_WEBUSER_RetirerId",
                table: "WEBTHEME",
                column: "RetirerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WEBUSER_WEBUSERROLE_SelfRoleUserId",
                table: "WEBUSER",
                column: "SelfRoleUserId",
                principalTable: "WEBUSERROLE",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_WEBUSER_WEBUSERROLE_WebUserRoleUserId",
                table: "WEBUSER",
                column: "WebUserRoleUserId",
                principalTable: "WEBUSERROLE",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ADMISSION_PATIENTENCOUNTER_PatientEncounterId",
                table: "ADMISSION");

            migrationBuilder.DropForeignKey(
                name: "FK_BILL_PATIENTENCOUNTER_PatientEncounterId",
                table: "BILL");

            migrationBuilder.DropForeignKey(
                name: "FK_BILLFEE_PATIENTENCOUNTER_ChildEncounterId",
                table: "BILLFEE");

            migrationBuilder.DropForeignKey(
                name: "FK_BILLFEE_PATIENTENCOUNTER_PatienEncounterId",
                table: "BILLFEE");

            migrationBuilder.DropForeignKey(
                name: "FK_BILLITEM_PATIENTENCOUNTER_PatientEncounterId",
                table: "BILLITEM");

            migrationBuilder.DropForeignKey(
                name: "FK_PATIENTROOM_PATIENTENCOUNTER_PatientEncounterId",
                table: "PATIENTROOM");

            migrationBuilder.DropForeignKey(
                name: "FK_ADMISSION_WEBUSER_CreaterId",
                table: "ADMISSION");

            migrationBuilder.DropForeignKey(
                name: "FK_AREA_WEBUSER_CreaterId",
                table: "AREA");

            migrationBuilder.DropForeignKey(
                name: "FK_AREA_WEBUSER_RetirerId",
                table: "AREA");

            migrationBuilder.DropForeignKey(
                name: "FK_BILL_WEBUSER_CheckedById",
                table: "BILL");

            migrationBuilder.DropForeignKey(
                name: "FK_BILL_WEBUSER_CreaterId",
                table: "BILL");

            migrationBuilder.DropForeignKey(
                name: "FK_BILL_WEBUSER_EditorId",
                table: "BILL");

            migrationBuilder.DropForeignKey(
                name: "FK_BILL_WEBUSER_RetirerId",
                table: "BILL");

            migrationBuilder.DropForeignKey(
                name: "FK_BILLFEE_WEBUSER_CreaterId",
                table: "BILLFEE");

            migrationBuilder.DropForeignKey(
                name: "FK_BILLFEE_WEBUSER_EditorId",
                table: "BILLFEE");

            migrationBuilder.DropForeignKey(
                name: "FK_BILLFEE_WEBUSER_RetirerId",
                table: "BILLFEE");

            migrationBuilder.DropForeignKey(
                name: "FK_BILLITEM_WEBUSER_CreaterId",
                table: "BILLITEM");

            migrationBuilder.DropForeignKey(
                name: "FK_BILLITEM_WEBUSER_EditorId",
                table: "BILLITEM");

            migrationBuilder.DropForeignKey(
                name: "FK_BILLITEM_WEBUSER_RetirerId",
                table: "BILLITEM");

            migrationBuilder.DropForeignKey(
                name: "FK_CASHTRANSACTION_WEBUSER_CreaterId",
                table: "CASHTRANSACTION");

            migrationBuilder.DropForeignKey(
                name: "FK_CATEGORY_WEBUSER_CreaterId",
                table: "CATEGORY");

            migrationBuilder.DropForeignKey(
                name: "FK_CATEGORY_WEBUSER_EditerId",
                table: "CATEGORY");

            migrationBuilder.DropForeignKey(
                name: "FK_CATEGORY_WEBUSER_RetirerId",
                table: "CATEGORY");

            migrationBuilder.DropForeignKey(
                name: "FK_DEPARTMENT_WEBUSER_CreaterId",
                table: "DEPARTMENT");

            migrationBuilder.DropForeignKey(
                name: "FK_DEPARTMENT_WEBUSER_EditerId",
                table: "DEPARTMENT");

            migrationBuilder.DropForeignKey(
                name: "FK_DEPARTMENT_WEBUSER_RetirerId",
                table: "DEPARTMENT");

            migrationBuilder.DropForeignKey(
                name: "FK_DRAWER_WEBUSER_CashierId",
                table: "DRAWER");

            migrationBuilder.DropForeignKey(
                name: "FK_DRAWER_WEBUSER_CreaterId",
                table: "DRAWER");

            migrationBuilder.DropForeignKey(
                name: "FK_FEE_WEBUSER_CreaterId",
                table: "FEE");

            migrationBuilder.DropForeignKey(
                name: "FK_FEE_WEBUSER_EditerId",
                table: "FEE");

            migrationBuilder.DropForeignKey(
                name: "FK_FEE_WEBUSER_RetirerId",
                table: "FEE");

            migrationBuilder.DropForeignKey(
                name: "FK_INSTITUTION_WEBUSER_CreaterId",
                table: "INSTITUTION");

            migrationBuilder.DropForeignKey(
                name: "FK_INSTITUTION_WEBUSER_EditerId",
                table: "INSTITUTION");

            migrationBuilder.DropForeignKey(
                name: "FK_INSTITUTION_WEBUSER_RetirerId",
                table: "INSTITUTION");

            migrationBuilder.DropForeignKey(
                name: "FK_ITEM_WEBUSER_CreaterId",
                table: "ITEM");

            migrationBuilder.DropForeignKey(
                name: "FK_ITEM_WEBUSER_EditerId",
                table: "ITEM");

            migrationBuilder.DropForeignKey(
                name: "FK_ITEM_WEBUSER_RetirerId",
                table: "ITEM");

            migrationBuilder.DropForeignKey(
                name: "FK_MACHINE_WEBUSER_CreaterId",
                table: "MACHINE");

            migrationBuilder.DropForeignKey(
                name: "FK_MEASUREMENTUNIT_WEBUSER_CreaterId",
                table: "MEASUREMENTUNIT");

            migrationBuilder.DropForeignKey(
                name: "FK_MEASUREMENTUNIT_WEBUSER_EditerId",
                table: "MEASUREMENTUNIT");

            migrationBuilder.DropForeignKey(
                name: "FK_MEASUREMENTUNIT_WEBUSER_RetirerId",
                table: "MEASUREMENTUNIT");

            migrationBuilder.DropForeignKey(
                name: "FK_MEMBERSHIPSCHEME_WEBUSER_CreaterId",
                table: "MEMBERSHIPSCHEME");

            migrationBuilder.DropForeignKey(
                name: "FK_MEMBERSHIPSCHEME_WEBUSER_RetirerId",
                table: "MEMBERSHIPSCHEME");

            migrationBuilder.DropForeignKey(
                name: "FK_PATIENT_WEBUSER_CreaterId",
                table: "PATIENT");

            migrationBuilder.DropForeignKey(
                name: "FK_PATIENT_WEBUSER_EditerId",
                table: "PATIENT");

            migrationBuilder.DropForeignKey(
                name: "FK_PATIENT_WEBUSER_RetirerId",
                table: "PATIENT");

            migrationBuilder.DropForeignKey(
                name: "FK_PATIENTROOM_WEBUSER_CreaterId",
                table: "PATIENTROOM");

            migrationBuilder.DropForeignKey(
                name: "FK_PAYMENTSCHEME_WEBUSER_CreaterId",
                table: "PAYMENTSCHEME");

            migrationBuilder.DropForeignKey(
                name: "FK_PAYMENTSCHEME_WEBUSER_RetirerId",
                table: "PAYMENTSCHEME");

            migrationBuilder.DropForeignKey(
                name: "FK_PERSON_WEBUSER_CreaterId",
                table: "PERSON");

            migrationBuilder.DropForeignKey(
                name: "FK_PERSON_WEBUSER_EditerId",
                table: "PERSON");

            migrationBuilder.DropForeignKey(
                name: "FK_PERSON_WEBUSER_RetirerId",
                table: "PERSON");

            migrationBuilder.DropForeignKey(
                name: "FK_PRICEMATRIX_WEBUSER_CreaterId",
                table: "PRICEMATRIX");

            migrationBuilder.DropForeignKey(
                name: "FK_PRICEMATRIX_WEBUSER_EditerId",
                table: "PRICEMATRIX");

            migrationBuilder.DropForeignKey(
                name: "FK_PRICEMATRIX_WEBUSER_RetirerId",
                table: "PRICEMATRIX");

            migrationBuilder.DropForeignKey(
                name: "FK_ROOM_WEBUSER_CreaterId",
                table: "ROOM");

            migrationBuilder.DropForeignKey(
                name: "FK_ROOMCATEGORY_WEBUSER_CreaterId",
                table: "ROOMCATEGORY");

            migrationBuilder.DropForeignKey(
                name: "FK_ROSTER_WEBUSER_CreaterId",
                table: "ROSTER");

            migrationBuilder.DropForeignKey(
                name: "FK_ROUTES_WEBUSER_CreaterId",
                table: "ROUTES");

            migrationBuilder.DropForeignKey(
                name: "FK_ROUTES_WEBUSER_RetirerId",
                table: "ROUTES");

            migrationBuilder.DropForeignKey(
                name: "FK_SPECIALITY_WEBUSER_CreaterId",
                table: "SPECIALITY");

            migrationBuilder.DropForeignKey(
                name: "FK_SPECIALITY_WEBUSER_RetirerId",
                table: "SPECIALITY");

            migrationBuilder.DropForeignKey(
                name: "FK_STAFF_WEBUSER_CreaterId",
                table: "STAFF");

            migrationBuilder.DropForeignKey(
                name: "FK_STAFF_WEBUSER_EditerId",
                table: "STAFF");

            migrationBuilder.DropForeignKey(
                name: "FK_STAFF_WEBUSER_RetirerId",
                table: "STAFF");

            migrationBuilder.DropForeignKey(
                name: "FK_VMP_WEBUSER_CreaterId",
                table: "VMP");

            migrationBuilder.DropForeignKey(
                name: "FK_VTM_WEBUSER_CreaterId",
                table: "VTM");

            migrationBuilder.DropForeignKey(
                name: "FK_WEBUSERROLE_WEBUSER_CreaterId",
                table: "WEBUSERROLE");

            migrationBuilder.DropForeignKey(
                name: "FK_WEBUSERROLE_WEBUSER_RetirerId",
                table: "WEBUSERROLE");

            migrationBuilder.DropForeignKey(
                name: "FK_BILLFEE_BILLITEM_BillItemId",
                table: "BILLFEE");

            migrationBuilder.DropForeignKey(
                name: "FK_BILLFEE_BILLITEM_ReferenceBillItemId",
                table: "BILLFEE");

            migrationBuilder.DropForeignKey(
                name: "FK_INSTITUTION_DEPARTMENT_LabDepartmentId",
                table: "INSTITUTION");

            migrationBuilder.DropForeignKey(
                name: "FK_ITEM_DEPARTMENT_DepartmentId",
                table: "ITEM");

            migrationBuilder.DropForeignKey(
                name: "FK_ITEM_DEPARTMENT_ForDepartmentId",
                table: "ITEM");

            migrationBuilder.DropForeignKey(
                name: "FK_MACHINE_DEPARTMENT_DepartmentId",
                table: "MACHINE");

            migrationBuilder.DropForeignKey(
                name: "FK_PAYMENTSCHEME_DEPARTMENT_DepartmentId",
                table: "PAYMENTSCHEME");

            migrationBuilder.DropForeignKey(
                name: "FK_PERSON_DEPARTMENT_DepartmentId",
                table: "PERSON");

            migrationBuilder.DropForeignKey(
                name: "FK_STAFF_DEPARTMENT_ChannelingDestinationDepartmentId",
                table: "STAFF");

            migrationBuilder.DropForeignKey(
                name: "FK_STAFF_DEPARTMENT_DepartmentId",
                table: "STAFF");

            migrationBuilder.DropForeignKey(
                name: "FK_STAFF_DEPARTMENT_WorkingDepartmentId",
                table: "STAFF");

            migrationBuilder.DropForeignKey(
                name: "FK_AREA_INSTITUTION_InstitutionId",
                table: "AREA");

            migrationBuilder.DropForeignKey(
                name: "FK_CATEGORY_INSTITUTION_InstitutionId",
                table: "CATEGORY");

            migrationBuilder.DropForeignKey(
                name: "FK_ITEM_INSTITUTION_ForInstitutionId",
                table: "ITEM");

            migrationBuilder.DropForeignKey(
                name: "FK_ITEM_INSTITUTION_ImporterId",
                table: "ITEM");

            migrationBuilder.DropForeignKey(
                name: "FK_ITEM_INSTITUTION_InstitutionId",
                table: "ITEM");

            migrationBuilder.DropForeignKey(
                name: "FK_ITEM_INSTITUTION_ManufacturerId",
                table: "ITEM");

            migrationBuilder.DropForeignKey(
                name: "FK_MACHINE_INSTITUTION_InstitutionId",
                table: "MACHINE");

            migrationBuilder.DropForeignKey(
                name: "FK_MEMBERSHIPSCHEME_INSTITUTION_InstitutionId",
                table: "MEMBERSHIPSCHEME");

            migrationBuilder.DropForeignKey(
                name: "FK_PAYMENTSCHEME_INSTITUTION_InstitutionId",
                table: "PAYMENTSCHEME");

            migrationBuilder.DropForeignKey(
                name: "FK_PERSON_INSTITUTION_InstitutionId",
                table: "PERSON");

            migrationBuilder.DropForeignKey(
                name: "FK_ROSTER_INSTITUTION_InstitutionId",
                table: "ROSTER");

            migrationBuilder.DropForeignKey(
                name: "FK_ROUTES_INSTITUTION_InstitutionId",
                table: "ROUTES");

            migrationBuilder.DropForeignKey(
                name: "FK_SPECIALITY_INSTITUTION_InstitutionId",
                table: "SPECIALITY");

            migrationBuilder.DropForeignKey(
                name: "FK_STAFF_INSTITUTION_ChannelingDestinationInstitutionId",
                table: "STAFF");

            migrationBuilder.DropForeignKey(
                name: "FK_STAFF_INSTITUTION_InstitutionId",
                table: "STAFF");

            migrationBuilder.DropForeignKey(
                name: "FK_ITEM_CATEGORY_CategoryId",
                table: "ITEM");

            migrationBuilder.DropForeignKey(
                name: "FK_ITEM_CATEGORY_FinancialCategoryId",
                table: "ITEM");

            migrationBuilder.DropForeignKey(
                name: "FK_ITEM_CATEGORY_ReportFormatId",
                table: "ITEM");

            migrationBuilder.DropForeignKey(
                name: "FK_ITEM_CATEGORY_SessionNumberGeneratorId",
                table: "ITEM");

            migrationBuilder.DropForeignKey(
                name: "FK_ITEM_CATEGORY_WorksheetId",
                table: "ITEM");

            migrationBuilder.DropForeignKey(
                name: "FK_STAFF_CATEGORY_DesignationId",
                table: "STAFF");

            migrationBuilder.DropForeignKey(
                name: "FK_STAFF_CATEGORY_GradeId",
                table: "STAFF");

            migrationBuilder.DropForeignKey(
                name: "FK_STAFF_CATEGORY_StaffCategoryId",
                table: "STAFF");

            migrationBuilder.DropForeignKey(
                name: "FK_PERSON_ITEM_BloodGroupId",
                table: "PERSON");

            migrationBuilder.DropForeignKey(
                name: "FK_PERSON_ITEM_CivilStatusId",
                table: "PERSON");

            migrationBuilder.DropForeignKey(
                name: "FK_PERSON_ITEM_OccupationId",
                table: "PERSON");

            migrationBuilder.DropForeignKey(
                name: "FK_PERSON_ITEM_RaceId",
                table: "PERSON");

            migrationBuilder.DropForeignKey(
                name: "FK_PERSON_ITEM_ReligionId",
                table: "PERSON");

            migrationBuilder.DropTable(
                name: "AGENTHISTORY");

            migrationBuilder.DropTable(
                name: "AMP");

            migrationBuilder.DropTable(
                name: "APIKEY");

            migrationBuilder.DropTable(
                name: "APPEMAIL");

            migrationBuilder.DropTable(
                name: "AUDITEVENT");

            migrationBuilder.DropTable(
                name: "BILLCOMPONENT");

            migrationBuilder.DropTable(
                name: "BILLENTRY");

            migrationBuilder.DropTable(
                name: "BILLFEEPAYMENT");

            migrationBuilder.DropTable(
                name: "BILLNUMBER");

            migrationBuilder.DropTable(
                name: "CATEGORYITEM");

            migrationBuilder.DropTable(
                name: "CONFIGOPTION");

            migrationBuilder.DropTable(
                name: "DESIGNATION");

            migrationBuilder.DropTable(
                name: "ENCOUNTERCREDITCOMPANY");

            migrationBuilder.DropTable(
                name: "FAMILYMEMBER");

            migrationBuilder.DropTable(
                name: "FEECHANGE");

            migrationBuilder.DropTable(
                name: "FEEVALUE");

            migrationBuilder.DropTable(
                name: "FORM");

            migrationBuilder.DropTable(
                name: "GRADE");

            migrationBuilder.DropTable(
                name: "ISSUERATEMARGINS");

            migrationBuilder.DropTable(
                name: "ITEMMAPPING");

            migrationBuilder.DropTable(
                name: "ITEMPACKAGE");

            migrationBuilder.DropTable(
                name: "ITEMSCATEGORIES");

            migrationBuilder.DropTable(
                name: "LOGINS");

            migrationBuilder.DropTable(
                name: "MAPPING");

            migrationBuilder.DropTable(
                name: "MEDICALPACKAGEITEM");

            migrationBuilder.DropTable(
                name: "PACKAGEITEM");

            migrationBuilder.DropTable(
                name: "PATIENTDEPOSITHISTORY");

            migrationBuilder.DropTable(
                name: "PATIENTFLAG");

            migrationBuilder.DropTable(
                name: "PATIENTITEM");

            migrationBuilder.DropTable(
                name: "PAYMENTGATEWAYTRANSACTION");

            migrationBuilder.DropTable(
                name: "PERSONINSTITUTION");

            migrationBuilder.DropTable(
                name: "PHARMACEUTICALBILLITEM");

            migrationBuilder.DropTable(
                name: "REPORTTEMPLATE");

            migrationBuilder.DropTable(
                name: "SERVICESESESSIONINSTANCE");

            migrationBuilder.DropTable(
                name: "SHIFT");

            migrationBuilder.DropTable(
                name: "SMS");

            migrationBuilder.DropTable(
                name: "STAFFCATEGORY");

            migrationBuilder.DropTable(
                name: "STAFFSALARY");

            migrationBuilder.DropTable(
                name: "STOCK");

            migrationBuilder.DropTable(
                name: "TOKEN");

            migrationBuilder.DropTable(
                name: "TRIGGERSUBSCRIPTION");

            migrationBuilder.DropTable(
                name: "UPLOAD");

            migrationBuilder.DropTable(
                name: "USERICON");

            migrationBuilder.DropTable(
                name: "USERNOTIFICATION");

            migrationBuilder.DropTable(
                name: "USERPREFERENCE");

            migrationBuilder.DropTable(
                name: "VMPP");

            migrationBuilder.DropTable(
                name: "WEBTHEME");

            migrationBuilder.DropTable(
                name: "WEBUSERDASHBOARD");

            migrationBuilder.DropTable(
                name: "WEBUSERDEPARTMENT");

            migrationBuilder.DropTable(
                name: "WEBUSERPAYMENTSCHEME");

            migrationBuilder.DropTable(
                name: "WEBUSERPRIVILEGE");

            migrationBuilder.DropTable(
                name: "WEBUSERROLEPRIVILEGE");

            migrationBuilder.DropTable(
                name: "WEBUSERROLEUSER");

            migrationBuilder.DropTable(
                name: "WEBUSERROUTE");

            migrationBuilder.DropTable(
                name: "PAYMENT");

            migrationBuilder.DropTable(
                name: "FAMILY");

            migrationBuilder.DropTable(
                name: "BILLSESSION");

            migrationBuilder.DropTable(
                name: "PATIENTDEPOSIT");

            migrationBuilder.DropTable(
                name: "ITEMBATCH");

            migrationBuilder.DropTable(
                name: "WEBCONTENT");

            migrationBuilder.DropTable(
                name: "NOTIFICATION");

            migrationBuilder.DropTable(
                name: "CASHBOOKENTRY");

            migrationBuilder.DropTable(
                name: "WEBLANGUAGE");

            migrationBuilder.DropTable(
                name: "CASHBOOK");

            migrationBuilder.DropTable(
                name: "PATIENTENCOUNTER");

            migrationBuilder.DropTable(
                name: "WEBUSER");

            migrationBuilder.DropTable(
                name: "WEBUSERROLE");

            migrationBuilder.DropTable(
                name: "BILLITEM");

            migrationBuilder.DropTable(
                name: "BILLFEE");

            migrationBuilder.DropTable(
                name: "BILL");

            migrationBuilder.DropTable(
                name: "FEE");

            migrationBuilder.DropTable(
                name: "PATIENTROOM");

            migrationBuilder.DropTable(
                name: "PRICEMATRIX");

            migrationBuilder.DropTable(
                name: "CASHTRANSACTION");

            migrationBuilder.DropTable(
                name: "PATIENT");

            migrationBuilder.DropTable(
                name: "ADMISSION");

            migrationBuilder.DropTable(
                name: "ROOM");

            migrationBuilder.DropTable(
                name: "DRAWER");

            migrationBuilder.DropTable(
                name: "ROOMCATEGORY");

            migrationBuilder.DropTable(
                name: "DEPARTMENT");

            migrationBuilder.DropTable(
                name: "INSTITUTION");

            migrationBuilder.DropTable(
                name: "ROUTES");

            migrationBuilder.DropTable(
                name: "CATEGORY");

            migrationBuilder.DropTable(
                name: "PAYMENTSCHEME");

            migrationBuilder.DropTable(
                name: "ITEM");

            migrationBuilder.DropTable(
                name: "MACHINE");

            migrationBuilder.DropTable(
                name: "MEASUREMENTUNIT");

            migrationBuilder.DropTable(
                name: "STAFF");

            migrationBuilder.DropTable(
                name: "VMP");

            migrationBuilder.DropTable(
                name: "PERSON");

            migrationBuilder.DropTable(
                name: "ROSTER");

            migrationBuilder.DropTable(
                name: "SPECIALITY");

            migrationBuilder.DropTable(
                name: "VTM");

            migrationBuilder.DropTable(
                name: "AREA");

            migrationBuilder.DropTable(
                name: "MEMBERSHIPSCHEME");
        }
    }
}
