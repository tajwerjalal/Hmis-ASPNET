using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HmisWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddSampleCollectionFieldsToInvestigationOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add SampleCollection fields to INVESTIGATIONORDER table only
            // Consultant properties (DefaultHospitalFee, DefaultRoomNo, etc.) already exist in STAFF table

            migrationBuilder.AddColumn<string>(
                name: "Barcode",
                table: "INVESTIGATIONORDER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BarcodeLabel",
                table: "INVESTIGATIONORDER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CollectionNotes",
                table: "INVESTIGATIONORDER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CollectionDateTime",
                table: "INVESTIGATIONORDER",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "CollectorId",
                table: "INVESTIGATIONORDER",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SampleCollected",
                table: "INVESTIGATIONORDER",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SampleCollectedAt",
                table: "INVESTIGATIONORDER",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "SampleCollectedById",
                table: "INVESTIGATIONORDER",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SampleCondition",
                table: "INVESTIGATIONORDER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SampleContainer",
                table: "INVESTIGATIONORDER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SampleType",
                table: "INVESTIGATIONORDER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SampleVolume",
                table: "INVESTIGATIONORDER",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_INVESTIGATIONORDER_CollectorId",
                table: "INVESTIGATIONORDER",
                column: "CollectorId");

            migrationBuilder.CreateIndex(
                name: "IX_INVESTIGATIONORDER_SampleCollectedById",
                table: "INVESTIGATIONORDER",
                column: "SampleCollectedById");

            migrationBuilder.AddForeignKey(
                name: "FK_INVESTIGATIONORDER_WEBUSER_CollectorId",
                table: "INVESTIGATIONORDER",
                column: "CollectorId",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_INVESTIGATIONORDER_WEBUSER_SampleCollectedById",
                table: "INVESTIGATIONORDER",
                column: "SampleCollectedById",
                principalTable: "WEBUSER",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_INVESTIGATIONORDER_WEBUSER_CollectorId",
                table: "INVESTIGATIONORDER");

            migrationBuilder.DropForeignKey(
                name: "FK_INVESTIGATIONORDER_WEBUSER_SampleCollectedById",
                table: "INVESTIGATIONORDER");

            migrationBuilder.DropIndex(
                name: "IX_INVESTIGATIONORDER_CollectorId",
                table: "INVESTIGATIONORDER");

            migrationBuilder.DropIndex(
                name: "IX_INVESTIGATIONORDER_SampleCollectedById",
                table: "INVESTIGATIONORDER");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "INVESTIGATIONORDER");

            migrationBuilder.DropColumn(
                name: "BarcodeLabel",
                table: "INVESTIGATIONORDER");

            migrationBuilder.DropColumn(
                name: "CollectionNotes",
                table: "INVESTIGATIONORDER");

            migrationBuilder.DropColumn(
                name: "CollectionDateTime",
                table: "INVESTIGATIONORDER");

            migrationBuilder.DropColumn(
                name: "CollectorId",
                table: "INVESTIGATIONORDER");

            migrationBuilder.DropColumn(
                name: "SampleCollected",
                table: "INVESTIGATIONORDER");

            migrationBuilder.DropColumn(
                name: "SampleCollectedAt",
                table: "INVESTIGATIONORDER");

            migrationBuilder.DropColumn(
                name: "SampleCollectedById",
                table: "INVESTIGATIONORDER");

            migrationBuilder.DropColumn(
                name: "SampleCondition",
                table: "INVESTIGATIONORDER");

            migrationBuilder.DropColumn(
                name: "SampleContainer",
                table: "INVESTIGATIONORDER");

            migrationBuilder.DropColumn(
                name: "SampleType",
                table: "INVESTIGATIONORDER");

            migrationBuilder.DropColumn(
                name: "SampleVolume",
                table: "INVESTIGATIONORDER");
        }
    }
}