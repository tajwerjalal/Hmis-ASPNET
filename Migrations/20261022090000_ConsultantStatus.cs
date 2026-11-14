using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HmisWeb.Migrations
{
    /// <summary>
    /// Consultant lifecycle status.
    ///
    /// The master screen started with a single Retired bit; the hospital needs
    /// to say why a doctor left the list, so the row now carries a Status:
    ///
    ///   Active / Inactive / Expired / Resigned / Retired / Other
    ///
    /// Active keeps Retired = 0; every other status keeps Retired = 1, so the
    /// existing "active only" filters, the linked STAFF row and the billing
    /// pickers keep working without knowing about the new wording.
    ///
    /// Guarded: on a database that already has the column nothing happens; the
    /// backfill only runs when the column is created, and derives the status of
    /// the rows that were retired before the column existed.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 22-Oct-2026
    /// Time      : 13:05
    /// Change Log: 22-Oct-2026 13:05 - created (Status varchar(20) + backfill).
    /// </summary>
    [DbContext(typeof(Data.ApplicationDbContext))]
    [Migration("20261022090000_ConsultantStatus")]
    public partial class ConsultantStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;

IF COL_LENGTH('dbo.CONSULT', 'Status') IS NULL
BEGIN
    ALTER TABLE dbo.CONSULT ADD Status varchar(20) NOT NULL
        CONSTRAINT DF_CONSULT_Status DEFAULT('Active');

    -- dynamic SQL: the batch is compiled before the new column exists, so the
    -- backfill cannot reference Status directly
    EXEC('UPDATE dbo.CONSULT SET Status = ''Retired'' WHERE Retired = 1;');
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;

IF EXISTS (SELECT 1 FROM sys.default_constraints
           WHERE parent_object_id = OBJECT_ID('dbo.CONSULT')
             AND name = 'DF_CONSULT_Status')
    ALTER TABLE dbo.CONSULT DROP CONSTRAINT DF_CONSULT_Status;

IF COL_LENGTH('dbo.CONSULT', 'Status') IS NOT NULL
    ALTER TABLE dbo.CONSULT DROP COLUMN Status;
");
        }
    }
}
