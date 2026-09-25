using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HmisWeb.Migrations
{
    /// <inheritdoc />
    public partial class ExpandOpdIpdCapture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AdjustedTotal",
                table: "PATIENTENCOUNTER",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AdmissionType",
                table: "PATIENTENCOUNTER",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BhtLong",
                table: "PATIENTENCOUNTER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BhtNo",
                table: "PATIENTENCOUNTER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Bmi",
                table: "PATIENTENCOUNTER",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Claimable",
                table: "PATIENTENCOUNTER",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<long>(
                name: "CreditCompanyId",
                table: "PATIENTENCOUNTER",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CreditLimit",
                table: "PATIENTENCOUNTER",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CreditPaidAmount",
                table: "PATIENTENCOUNTER",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "CreditUsedAmount",
                table: "PATIENTENCOUNTER",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Dbp",
                table: "PATIENTENCOUNTER",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Discharged",
                table: "PATIENTENCOUNTER",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "Discount",
                table: "PATIENTENCOUNTER",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "FinalBillId",
                table: "PATIENTENCOUNTER",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Foriegner",
                table: "PATIENTENCOUNTER",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<double>(
                name: "GrantTotal",
                table: "PATIENTENCOUNTER",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "GuardianId",
                table: "PATIENTENCOUNTER",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "GuardianRelationshipToPatientId",
                table: "PATIENTENCOUNTER",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Height",
                table: "PATIENTENCOUNTER",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "PATIENTENCOUNTER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "NetTotal",
                table: "PATIENTENCOUNTER",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "OpdDoctorId",
                table: "PATIENTENCOUNTER",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PaymentFinalized",
                table: "PATIENTENCOUNTER",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "PaymentMethod",
                table: "PATIENTENCOUNTER",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Pfr",
                table: "PATIENTENCOUNTER",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PolicyNo",
                table: "PATIENTENCOUNTER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Pr",
                table: "PATIENTENCOUNTER",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenceNo",
                table: "PATIENTENCOUNTER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferralId",
                table: "PATIENTENCOUNTER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ReferringConsultantId",
                table: "PATIENTENCOUNTER",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "RespiratoryRate",
                table: "PATIENTENCOUNTER",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Saturation",
                table: "PATIENTENCOUNTER",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Sbp",
                table: "PATIENTENCOUNTER",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TimeOfAdmission",
                table: "PATIENTENCOUNTER",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "TimeOfDischarge",
                table: "PATIENTENCOUNTER",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Weight",
                table: "PATIENTENCOUNTER",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "WorkplaceId",
                table: "PATIENTENCOUNTER",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTENCOUNTER_CreditCompanyId",
                table: "PATIENTENCOUNTER",
                column: "CreditCompanyId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTENCOUNTER_FinalBillId",
                table: "PATIENTENCOUNTER",
                column: "FinalBillId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTENCOUNTER_GuardianId",
                table: "PATIENTENCOUNTER",
                column: "GuardianId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTENCOUNTER_GuardianRelationshipToPatientId",
                table: "PATIENTENCOUNTER",
                column: "GuardianRelationshipToPatientId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTENCOUNTER_OpdDoctorId",
                table: "PATIENTENCOUNTER",
                column: "OpdDoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTENCOUNTER_ReferringConsultantId",
                table: "PATIENTENCOUNTER",
                column: "ReferringConsultantId");

            migrationBuilder.CreateIndex(
                name: "IX_PATIENTENCOUNTER_WorkplaceId",
                table: "PATIENTENCOUNTER",
                column: "WorkplaceId");

            migrationBuilder.AddForeignKey(
                name: "FK_PATIENTENCOUNTER_BILL_FinalBillId",
                table: "PATIENTENCOUNTER",
                column: "FinalBillId",
                principalTable: "BILL",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PATIENTENCOUNTER_CATEGORY_GuardianRelationshipToPatientId",
                table: "PATIENTENCOUNTER",
                column: "GuardianRelationshipToPatientId",
                principalTable: "CATEGORY",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PATIENTENCOUNTER_INSTITUTION_CreditCompanyId",
                table: "PATIENTENCOUNTER",
                column: "CreditCompanyId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PATIENTENCOUNTER_INSTITUTION_WorkplaceId",
                table: "PATIENTENCOUNTER",
                column: "WorkplaceId",
                principalTable: "INSTITUTION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PATIENTENCOUNTER_PERSON_GuardianId",
                table: "PATIENTENCOUNTER",
                column: "GuardianId",
                principalTable: "PERSON",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PATIENTENCOUNTER_STAFF_OpdDoctorId",
                table: "PATIENTENCOUNTER",
                column: "OpdDoctorId",
                principalTable: "STAFF",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PATIENTENCOUNTER_STAFF_ReferringConsultantId",
                table: "PATIENTENCOUNTER",
                column: "ReferringConsultantId",
                principalTable: "STAFF",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PATIENTENCOUNTER_BILL_FinalBillId",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropForeignKey(
                name: "FK_PATIENTENCOUNTER_CATEGORY_GuardianRelationshipToPatientId",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropForeignKey(
                name: "FK_PATIENTENCOUNTER_INSTITUTION_CreditCompanyId",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropForeignKey(
                name: "FK_PATIENTENCOUNTER_INSTITUTION_WorkplaceId",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropForeignKey(
                name: "FK_PATIENTENCOUNTER_PERSON_GuardianId",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropForeignKey(
                name: "FK_PATIENTENCOUNTER_STAFF_OpdDoctorId",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropForeignKey(
                name: "FK_PATIENTENCOUNTER_STAFF_ReferringConsultantId",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropIndex(
                name: "IX_PATIENTENCOUNTER_CreditCompanyId",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropIndex(
                name: "IX_PATIENTENCOUNTER_FinalBillId",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropIndex(
                name: "IX_PATIENTENCOUNTER_GuardianId",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropIndex(
                name: "IX_PATIENTENCOUNTER_GuardianRelationshipToPatientId",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropIndex(
                name: "IX_PATIENTENCOUNTER_OpdDoctorId",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropIndex(
                name: "IX_PATIENTENCOUNTER_ReferringConsultantId",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropIndex(
                name: "IX_PATIENTENCOUNTER_WorkplaceId",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "AdjustedTotal",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "AdmissionType",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "BhtLong",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "BhtNo",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "Bmi",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "Claimable",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "CreditCompanyId",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "CreditLimit",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "CreditPaidAmount",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "CreditUsedAmount",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "Dbp",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "Discharged",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "Discount",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "FinalBillId",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "Foriegner",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "GrantTotal",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "GuardianId",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "GuardianRelationshipToPatientId",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "NetTotal",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "OpdDoctorId",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "PaymentFinalized",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "Pfr",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "PolicyNo",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "Pr",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "ReferenceNo",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "ReferralId",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "ReferringConsultantId",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "RespiratoryRate",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "Saturation",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "Sbp",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "TimeOfAdmission",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "TimeOfDischarge",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "WorkplaceId",
                table: "PATIENTENCOUNTER");
        }
    }
}
