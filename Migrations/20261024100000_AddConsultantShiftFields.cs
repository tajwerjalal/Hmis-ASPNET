using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HmisWeb.Migrations
{
    /// <summary>
    /// Consultant clinic windows and fees.
    ///
    /// [STAFF] already carries the legacy single window
    /// (SessionStartTime / SessionEndTime) plus WorkDays, FirstVisitFee and
    /// SubsequentVisitFee. The consultant master now exposes a morning window
    /// (the legacy pair) and an evening window, so the evening pair is added
    /// here:
    ///
    ///   EveningStartTime nvarchar(50) NULL
    ///   EveningEndTime   nvarchar(50) NULL
    ///
    /// The migration is guarded, so re-running it on a database that already
    /// has the columns is harmless.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 24-Oct-2026
    /// Time      : 10:00
    /// Change Log: 24-Oct-2026 10:00 - created.
    /// </summary>
    [DbContext(typeof(Data.ApplicationDbContext))]
    [Migration("20261024100000_AddConsultantShiftFields")]
    public partial class AddConsultantShiftFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;

IF COL_LENGTH('dbo.STAFF', 'EveningStartTime') IS NULL
    ALTER TABLE dbo.STAFF ADD EveningStartTime nvarchar(50) NULL;

IF COL_LENGTH('dbo.STAFF', 'EveningEndTime') IS NULL
    ALTER TABLE dbo.STAFF ADD EveningEndTime nvarchar(50) NULL;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;

IF COL_LENGTH('dbo.STAFF', 'EveningStartTime') IS NOT NULL
    ALTER TABLE dbo.STAFF DROP COLUMN EveningStartTime;

IF COL_LENGTH('dbo.STAFF', 'EveningEndTime') IS NOT NULL
    ALTER TABLE dbo.STAFF DROP COLUMN EveningEndTime;
");
        }
    }
}
