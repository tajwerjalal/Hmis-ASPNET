using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HmisWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddEmrOpdClinicalCapture : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ChiefComplaint",
                table: "PATIENTENCOUNTER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Diagnosis",
                table: "PATIENTENCOUNTER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Examination",
                table: "PATIENTENCOUNTER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FollowUp",
                table: "PATIENTENCOUNTER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HistoryOfPresentingComplaint",
                table: "PATIENTENCOUNTER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Investigations",
                table: "PATIENTENCOUNTER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Management",
                table: "PATIENTENCOUNTER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PastMedicalHistory",
                table: "PATIENTENCOUNTER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TreatmentPlan",
                table: "PATIENTENCOUNTER",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ChiefComplaint",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "Diagnosis",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "Examination",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "FollowUp",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "HistoryOfPresentingComplaint",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "Investigations",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "Management",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "PastMedicalHistory",
                table: "PATIENTENCOUNTER");

            migrationBuilder.DropColumn(
                name: "TreatmentPlan",
                table: "PATIENTENCOUNTER");
        }
    }
}
