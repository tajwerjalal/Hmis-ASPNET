using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HmisWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddXrModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_USORDER_USREPORTTEMPLATE_ReportTemplateId",
                table: "USORDER");

            migrationBuilder.DropForeignKey(
                name: "FK_USORDER_WEBUSER_FinalizedById",
                table: "USORDER");

            migrationBuilder.DropForeignKey(
                name: "FK_USREPORTTEMPLATE_USSERVICE_UsServiceId",
                table: "USREPORTTEMPLATE");

            migrationBuilder.DropForeignKey(
                name: "FK_USREPORTTEMPLATE_WEBUSER_CreaterId",
                table: "USREPORTTEMPLATE");

            migrationBuilder.DropForeignKey(
                name: "FK_USREPORTTEMPLATE_WEBUSER_EditerId",
                table: "USREPORTTEMPLATE");

            migrationBuilder.CreateTable(
                name: "XRMEDICALTERM",
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
                    table.PrimaryKey("PK_XRMEDICALTERM", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "XRSERVICECATEGORY",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_XRSERVICECATEGORY", x => x.Id);
                    table.ForeignKey(
                        name: "FK_XRSERVICECATEGORY_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_XRSERVICECATEGORY_WEBUSER_CreaterId",
                        column: x => x.CreaterId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_XRSERVICECATEGORY_WEBUSER_EditerId",
                        column: x => x.EditerId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_XRSERVICECATEGORY_WEBUSER_RetirerId",
                        column: x => x.RetirerId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "XRSERVICE",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sname = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PrintName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Requirements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Rate = table.Column<double>(type: "float", nullable: true),
                    RateForeigner = table.Column<double>(type: "float", nullable: true),
                    DiscountAllowed = table.Column<bool>(type: "bit", nullable: false),
                    CategoryId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    ItemType = table.Column<int>(type: "int", nullable: true),
                    Priority = table.Column<int>(type: "int", nullable: true),
                    Billable = table.Column<bool>(type: "bit", nullable: false),
                    Active = table.Column<bool>(type: "bit", nullable: false),
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
                    table.PrimaryKey("PK_XRSERVICE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_XRSERVICE_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_XRSERVICE_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_XRSERVICE_WEBUSER_CreaterId",
                        column: x => x.CreaterId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_XRSERVICE_WEBUSER_EditerId",
                        column: x => x.EditerId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_XRSERVICE_WEBUSER_RetirerId",
                        column: x => x.RetirerId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_XRSERVICE_XRSERVICECATEGORY_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "XRSERVICECATEGORY",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "XREPORTTEMPLATE",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    XrServiceId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_XREPORTTEMPLATE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_XREPORTTEMPLATE_WEBUSER_CreaterId",
                        column: x => x.CreaterId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_XREPORTTEMPLATE_WEBUSER_EditerId",
                        column: x => x.EditerId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_XREPORTTEMPLATE_XRSERVICE_XrServiceId",
                        column: x => x.XrServiceId,
                        principalTable: "XRSERVICE",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "XRORDER",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientEncounterId = table.Column<long>(type: "bigint", nullable: true),
                    PatientId = table.Column<long>(type: "bigint", nullable: true),
                    VisitId = table.Column<long>(type: "bigint", nullable: true),
                    XrServiceId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
                    BillId = table.Column<long>(type: "bigint", nullable: true),
                    Qty = table.Column<int>(type: "int", nullable: false),
                    OrderName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Ordered = table.Column<bool>(type: "bit", nullable: false),
                    SampleCollected = table.Column<bool>(type: "bit", nullable: false),
                    SampleCollectedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    SampleCollectedById = table.Column<long>(type: "bigint", nullable: true),
                    SampleType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SampleCondition = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Barcode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BarcodeLabel = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CollectorId = table.Column<long>(type: "bigint", nullable: true),
                    CollectionDateTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CollectionNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Completed = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Result = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultNotes = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImagesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ResultHtml = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ReportStatus = table.Column<int>(type: "int", nullable: false),
                    FinalizedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    FinalizedById = table.Column<long>(type: "bigint", nullable: true),
                    LastSavedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WordCount = table.Column<int>(type: "int", nullable: false),
                    CharCount = table.Column<int>(type: "int", nullable: false),
                    ReportTemplateId = table.Column<long>(type: "bigint", nullable: true),
                    DoctorId = table.Column<long>(type: "bigint", nullable: true),
                    CreaterId = table.Column<long>(type: "bigint", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Retired = table.Column<bool>(type: "bit", nullable: false),
                    RetiredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RetireComments = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EditerId = table.Column<long>(type: "bigint", nullable: true),
                    EditedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_XRORDER", x => x.Id);
                    table.ForeignKey(
                        name: "FK_XRORDER_BILL_BillId",
                        column: x => x.BillId,
                        principalTable: "BILL",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_XRORDER_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_XRORDER_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_XRORDER_PATIENTENCOUNTER_PatientEncounterId",
                        column: x => x.PatientEncounterId,
                        principalTable: "PATIENTENCOUNTER",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_XRORDER_PATIENT_PatientId",
                        column: x => x.PatientId,
                        principalTable: "PATIENT",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_XRORDER_STAFF_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "STAFF",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_XRORDER_WEBUSER_CollectorId",
                        column: x => x.CollectorId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_XRORDER_WEBUSER_CreaterId",
                        column: x => x.CreaterId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_XRORDER_WEBUSER_FinalizedById",
                        column: x => x.FinalizedById,
                        principalTable: "WEBUSER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_XRORDER_WEBUSER_SampleCollectedById",
                        column: x => x.SampleCollectedById,
                        principalTable: "WEBUSER",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_XRORDER_XREPORTTEMPLATE_ReportTemplateId",
                        column: x => x.ReportTemplateId,
                        principalTable: "XREPORTTEMPLATE",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_XRORDER_XRSERVICE_XrServiceId",
                        column: x => x.XrServiceId,
                        principalTable: "XRSERVICE",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_XREPORTTEMPLATE_CreaterId",
                table: "XREPORTTEMPLATE",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_XREPORTTEMPLATE_EditerId",
                table: "XREPORTTEMPLATE",
                column: "EditerId");

            migrationBuilder.CreateIndex(
                name: "IX_XREPORTTEMPLATE_XrServiceId",
                table: "XREPORTTEMPLATE",
                column: "XrServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_XRORDER_BillId",
                table: "XRORDER",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_XRORDER_CollectorId",
                table: "XRORDER",
                column: "CollectorId");

            migrationBuilder.CreateIndex(
                name: "IX_XRORDER_CreaterId",
                table: "XRORDER",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_XRORDER_DepartmentId",
                table: "XRORDER",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_XRORDER_DoctorId",
                table: "XRORDER",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_XRORDER_FinalizedById",
                table: "XRORDER",
                column: "FinalizedById");

            migrationBuilder.CreateIndex(
                name: "IX_XRORDER_InstitutionId",
                table: "XRORDER",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_XRORDER_PatientEncounterId",
                table: "XRORDER",
                column: "PatientEncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_XRORDER_PatientId",
                table: "XRORDER",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_XRORDER_ReportTemplateId",
                table: "XRORDER",
                column: "ReportTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_XRORDER_SampleCollectedById",
                table: "XRORDER",
                column: "SampleCollectedById");

            migrationBuilder.CreateIndex(
                name: "IX_XRORDER_XrServiceId",
                table: "XRORDER",
                column: "XrServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_XRSERVICE_CategoryId",
                table: "XRSERVICE",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_XRSERVICE_CreaterId",
                table: "XRSERVICE",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_XRSERVICE_DepartmentId",
                table: "XRSERVICE",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_XRSERVICE_EditerId",
                table: "XRSERVICE",
                column: "EditerId");

            migrationBuilder.CreateIndex(
                name: "IX_XRSERVICE_InstitutionId",
                table: "XRSERVICE",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_XRSERVICE_RetirerId",
                table: "XRSERVICE",
                column: "RetirerId");

            migrationBuilder.CreateIndex(
                name: "IX_XRSERVICECATEGORY_CreaterId",
                table: "XRSERVICECATEGORY",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_XRSERVICECATEGORY_EditerId",
                table: "XRSERVICECATEGORY",
                column: "EditerId");

            migrationBuilder.CreateIndex(
                name: "IX_XRSERVICECATEGORY_InstitutionId",
                table: "XRSERVICECATEGORY",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_XRSERVICECATEGORY_RetirerId",
                table: "XRSERVICECATEGORY",
                column: "RetirerId");

            migrationBuilder.AddForeignKey(
                name: "FK_USORDER_USREPORTTEMPLATE_ReportTemplateId",
                table: "USORDER",
                column: "ReportTemplateId",
                principalTable: "USREPORTTEMPLATE",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_USORDER_WEBUSER_FinalizedById",
                table: "USORDER",
                column: "FinalizedById",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_USREPORTTEMPLATE_USSERVICE_UsServiceId",
                table: "USREPORTTEMPLATE",
                column: "UsServiceId",
                principalTable: "USSERVICE",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_USREPORTTEMPLATE_WEBUSER_CreaterId",
                table: "USREPORTTEMPLATE",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_USREPORTTEMPLATE_WEBUSER_EditerId",
                table: "USREPORTTEMPLATE",
                column: "EditerId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
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

            migrationBuilder.DropForeignKey(
                name: "FK_USREPORTTEMPLATE_USSERVICE_UsServiceId",
                table: "USREPORTTEMPLATE");

            migrationBuilder.DropForeignKey(
                name: "FK_USREPORTTEMPLATE_WEBUSER_CreaterId",
                table: "USREPORTTEMPLATE");

            migrationBuilder.DropForeignKey(
                name: "FK_USREPORTTEMPLATE_WEBUSER_EditerId",
                table: "USREPORTTEMPLATE");

            migrationBuilder.DropTable(
                name: "XRMEDICALTERM");

            migrationBuilder.DropTable(
                name: "XRORDER");

            migrationBuilder.DropTable(
                name: "XREPORTTEMPLATE");

            migrationBuilder.DropTable(
                name: "XRSERVICE");

            migrationBuilder.DropTable(
                name: "XRSERVICECATEGORY");

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

            migrationBuilder.AddForeignKey(
                name: "FK_USREPORTTEMPLATE_USSERVICE_UsServiceId",
                table: "USREPORTTEMPLATE",
                column: "UsServiceId",
                principalTable: "USSERVICE",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_USREPORTTEMPLATE_WEBUSER_CreaterId",
                table: "USREPORTTEMPLATE",
                column: "CreaterId",
                principalTable: "WEBUSER",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_USREPORTTEMPLATE_WEBUSER_EditerId",
                table: "USREPORTTEMPLATE",
                column: "EditerId",
                principalTable: "WEBUSER",
                principalColumn: "Id");
        }
    }
}
