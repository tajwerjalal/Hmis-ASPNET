using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HmisWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddUsOrderBillLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<long>(
                name: "BillId",
                table: "USORDER",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Qty",
                table: "USORDER",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_USORDER_BillId",
                table: "USORDER",
                column: "BillId");

            migrationBuilder.AddForeignKey(
                name: "FK_USORDER_BILL_BillId",
                table: "USORDER",
                column: "BillId",
                principalTable: "BILL",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_USORDER_BILL_BillId",
                table: "USORDER");

            migrationBuilder.DropIndex(
                name: "IX_USORDER_BillId",
                table: "USORDER");

            migrationBuilder.DropColumn(
                name: "BillId",
                table: "USORDER");

            migrationBuilder.DropColumn(
                name: "Qty",
                table: "USORDER");
        }
    }
}
