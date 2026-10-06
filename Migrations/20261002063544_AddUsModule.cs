using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HmisWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddUsModule : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "USSERVICE",
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
                    table.PrimaryKey("PK_USSERVICE", x => x.Id);
                    table.ForeignKey(
                        name: "FK_USSERVICE_CATEGORY_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "CATEGORY",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_USSERVICE_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_USSERVICE_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_USSERVICE_WEBUSER_CreaterId",
                        column: x => x.CreaterId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_USSERVICE_WEBUSER_EditerId",
                        column: x => x.EditerId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_USSERVICE_WEBUSER_RetirerId",
                        column: x => x.RetirerId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "USORDER",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PatientEncounterId = table.Column<long>(type: "bigint", nullable: true),
                    PatientId = table.Column<long>(type: "bigint", nullable: true),
                    VisitId = table.Column<long>(type: "bigint", nullable: true),
                    UsServiceId = table.Column<long>(type: "bigint", nullable: true),
                    InstitutionId = table.Column<long>(type: "bigint", nullable: true),
                    DepartmentId = table.Column<long>(type: "bigint", nullable: true),
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
                    table.PrimaryKey("PK_USORDER", x => x.Id);
                    table.ForeignKey(
                        name: "FK_USORDER_DEPARTMENT_DepartmentId",
                        column: x => x.DepartmentId,
                        principalTable: "DEPARTMENT",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_USORDER_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_USORDER_PATIENTENCOUNTER_PatientEncounterId",
                        column: x => x.PatientEncounterId,
                        principalTable: "PATIENTENCOUNTER",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_USORDER_PATIENT_PatientId",
                        column: x => x.PatientId,
                        principalTable: "PATIENT",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_USORDER_STAFF_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "STAFF",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_USORDER_USSERVICE_UsServiceId",
                        column: x => x.UsServiceId,
                        principalTable: "USSERVICE",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_USORDER_WEBUSER_CollectorId",
                        column: x => x.CollectorId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_USORDER_WEBUSER_CreaterId",
                        column: x => x.CreaterId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_USORDER_WEBUSER_SampleCollectedById",
                        column: x => x.SampleCollectedById,
                        principalTable: "WEBUSER",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_USORDER_CollectorId",
                table: "USORDER",
                column: "CollectorId");

            migrationBuilder.CreateIndex(
                name: "IX_USORDER_CreaterId",
                table: "USORDER",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_USORDER_DepartmentId",
                table: "USORDER",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_USORDER_DoctorId",
                table: "USORDER",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_USORDER_InstitutionId",
                table: "USORDER",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_USORDER_PatientEncounterId",
                table: "USORDER",
                column: "PatientEncounterId");

            migrationBuilder.CreateIndex(
                name: "IX_USORDER_PatientId",
                table: "USORDER",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_USORDER_SampleCollectedById",
                table: "USORDER",
                column: "SampleCollectedById");

            migrationBuilder.CreateIndex(
                name: "IX_USORDER_UsServiceId",
                table: "USORDER",
                column: "UsServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_USSERVICE_CategoryId",
                table: "USSERVICE",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_USSERVICE_CreaterId",
                table: "USSERVICE",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_USSERVICE_DepartmentId",
                table: "USSERVICE",
                column: "DepartmentId");

            migrationBuilder.CreateIndex(
                name: "IX_USSERVICE_EditerId",
                table: "USSERVICE",
                column: "EditerId");

            migrationBuilder.CreateIndex(
                name: "IX_USSERVICE_InstitutionId",
                table: "USSERVICE",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_USSERVICE_RetirerId",
                table: "USSERVICE",
                column: "RetirerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "USORDER");

            migrationBuilder.DropTable(
                name: "USSERVICE");
        }
    }
}
