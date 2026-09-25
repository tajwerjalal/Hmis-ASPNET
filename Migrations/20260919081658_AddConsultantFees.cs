using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HmisWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddConsultantFees : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Degrees",
                table: "STAFF",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "FirstVisitFee",
                table: "STAFF",
                type: "float",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "SubsequentVisitFee",
                table: "STAFF",
                type: "float",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Degrees",
                table: "STAFF");

            migrationBuilder.DropColumn(
                name: "FirstVisitFee",
                table: "STAFF");

            migrationBuilder.DropColumn(
                name: "SubsequentVisitFee",
                table: "STAFF");
        }
    }
}
