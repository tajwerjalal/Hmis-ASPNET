using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HmisWeb.Migrations
{
    /// <inheritdoc />
    public partial class AddUsServiceCategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_USSERVICE_CATEGORY_CategoryId",
                table: "USSERVICE");

            migrationBuilder.CreateTable(
                name: "USSERVICECATEGORY",
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
                    table.PrimaryKey("PK_USSERVICECATEGORY", x => x.Id);
                    table.ForeignKey(
                        name: "FK_USSERVICECATEGORY_INSTITUTION_InstitutionId",
                        column: x => x.InstitutionId,
                        principalTable: "INSTITUTION",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_USSERVICECATEGORY_WEBUSER_CreaterId",
                        column: x => x.CreaterId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_USSERVICECATEGORY_WEBUSER_EditerId",
                        column: x => x.EditerId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_USSERVICECATEGORY_WEBUSER_RetirerId",
                        column: x => x.RetirerId,
                        principalTable: "WEBUSER",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_USSERVICECATEGORY_CreaterId",
                table: "USSERVICECATEGORY",
                column: "CreaterId");

            migrationBuilder.CreateIndex(
                name: "IX_USSERVICECATEGORY_EditerId",
                table: "USSERVICECATEGORY",
                column: "EditerId");

            migrationBuilder.CreateIndex(
                name: "IX_USSERVICECATEGORY_InstitutionId",
                table: "USSERVICECATEGORY",
                column: "InstitutionId");

            migrationBuilder.CreateIndex(
                name: "IX_USSERVICECATEGORY_RetirerId",
                table: "USSERVICECATEGORY",
                column: "RetirerId");

            migrationBuilder.AddForeignKey(
                name: "FK_USSERVICE_USSERVICECATEGORY_CategoryId",
                table: "USSERVICE",
                column: "CategoryId",
                principalTable: "USSERVICECATEGORY",
                principalColumn: "Id");

            migrationBuilder.InsertData(
                table: "USSERVICECATEGORY",
                columns: new[] { "Name", "Sname", "Code", "Description", "Retired" },
                values: new object[]
                {
                    "General", "GEN", "USC-01", "General ultrasound studies", false
                });
            migrationBuilder.InsertData(
                table: "USSERVICECATEGORY",
                columns: new[] { "Name", "Sname", "Code", "Description", "Retired" },
                values: new object[]
                {
                    "Abdomen", "ABD", "USC-02", "Abdominal and upper abdominal studies", false
                });
            migrationBuilder.InsertData(
                table: "USSERVICECATEGORY",
                columns: new[] { "Name", "Sname", "Code", "Description", "Retired" },
                values: new object[]
                {
                    "Obstetrics", "OBS", "USC-03", "Obstetric and antenatal studies", false
                });
            migrationBuilder.InsertData(
                table: "USSERVICECATEGORY",
                columns: new[] { "Name", "Sname", "Code", "Description", "Retired" },
                values: new object[]
                {
                    "Gynaecology", "GYN", "USC-04", "Gynaecological and pelvic studies", false
                });
            migrationBuilder.InsertData(
                table: "USSERVICECATEGORY",
                columns: new[] { "Name", "Sname", "Code", "Description", "Retired" },
                values: new object[]
                {
                    "Cardiac", "CAR", "USC-05", "Cardiac and echocardiography studies", false
                });
            migrationBuilder.InsertData(
                table: "USSERVICECATEGORY",
                columns: new[] { "Name", "Sname", "Code", "Description", "Retired" },
                values: new object[]
                {
                    "Vascular", "VAS", "USC-06", "Vascular and Doppler studies", false
                });
            migrationBuilder.InsertData(
                table: "USSERVICECATEGORY",
                columns: new[] { "Name", "Sname", "Code", "Description", "Retired" },
                values: new object[]
                {
                    "Musculoskeletal", "MSK", "USC-07", "Musculoskeletal and joint studies", false
                });
            migrationBuilder.InsertData(
                table: "USSERVICECATEGORY",
                columns: new[] { "Name", "Sname", "Code", "Description", "Retired" },
                values: new object[]
                {
                    "Small Parts", "SP", "USC-08", "Small parts and superficial structures", false
                });
            migrationBuilder.InsertData(
                table: "USSERVICECATEGORY",
                columns: new[] { "Name", "Sname", "Code", "Description", "Retired" },
                values: new object[]
                {
                    "Neuro", "NEU", "USC-09", "Transcranial and neurosonography studies", false
                });
            migrationBuilder.InsertData(
                table: "USSERVICECATEGORY",
                columns: new[] { "Name", "Sname", "Code", "Description", "Retired" },
                values: new object[]
                {
                    "Urinary", "URO", "USC-10", "Renal, urinary tract and bladder studies", false
                });
            migrationBuilder.InsertData(
                table: "USSERVICECATEGORY",
                columns: new[] { "Name", "Sname", "Code", "Description", "Retired" },
                values: new object[]
                {
                    "Breast", "BRE", "USC-11", "Breast imaging studies", false
                });
            migrationBuilder.InsertData(
                table: "USSERVICECATEGORY",
                columns: new[] { "Name", "Sname", "Code", "Description", "Retired" },
                values: new object[]
                {
                    "Doppler", "DOP", "USC-12", "General Doppler studies", false
                });
            migrationBuilder.InsertData(
                table: "USSERVICECATEGORY",
                columns: new[] { "Name", "Sname", "Code", "Description", "Retired" },
                values: new object[]
                {
                    "Interventional", "INT", "USC-13", "Image guided interventional procedures", false
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_USSERVICE_USSERVICECATEGORY_CategoryId",
                table: "USSERVICE");

            migrationBuilder.DropTable(
                name: "USSERVICECATEGORY");

            migrationBuilder.AddForeignKey(
                name: "FK_USSERVICE_CATEGORY_CategoryId",
                table: "USSERVICE",
                column: "CategoryId",
                principalTable: "CATEGORY",
                principalColumn: "Id");
        }
    }
}
