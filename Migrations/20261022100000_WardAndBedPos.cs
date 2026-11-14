using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HmisWeb.Migrations
{
    /// <summary>
    /// Wards and bed positions - the two legacy hospital masters the Rooms &amp;
    /// Beds screen now runs on:
    ///
    ///   [WARD]     <- E:\Projects\HMC\Master\wards.DBF      (15 rows)
    ///   [BED_POS]  <- E:\Projects\HMC\Master\bed_pos.DBF    (113 live rows)
    ///
    /// The tables were empty before (the screen used [ROOM], which never got
    /// data), so both are created here with the real widths of the source files
    /// and the application columns (Retired/RetireComments). The rows themselves
    /// are loaded by workspace\temp\ward_bedpos_import.sql, which upserts by
    /// code so it can be re-run after the VFP masters change.
    ///
    /// Guarded: nothing happens on a database that already has the tables.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 22-Oct-2026
    /// Time      : 13:50
    /// Change Log: 22-Oct-2026 13:50 - created (WARD + BED_POS).
    /// </summary>
    [DbContext(typeof(Data.ApplicationDbContext))]
    [Migration("20261022100000_WardAndBedPos")]
    public partial class WardAndBedPos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;

IF OBJECT_ID('dbo.WARD', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.WARD
    (
        Id             bigint IDENTITY(1,1) NOT NULL,
        Code           varchar(10)  NOT NULL,
        Name           varchar(50)  NULL,
        UserId         varchar(15)  NULL,
        UDate          date         NULL,
        UTime          varchar(5)   NULL,
        Retired        bit          NOT NULL CONSTRAINT DF_WARD_Retired DEFAULT(0),
        RetireComments varchar(500) NULL,
        CONSTRAINT PK_WARD PRIMARY KEY (Id)
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID('dbo.WARD')
                 AND name = 'UX_WARD_Code_Active')
    EXEC('CREATE UNIQUE INDEX UX_WARD_Code_Active ON dbo.WARD (Code) WHERE Retired = 0;');

IF OBJECT_ID('dbo.BED_POS', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BED_POS
    (
        Id             bigint IDENTITY(1,1) NOT NULL,
        WardCode       varchar(10)  NULL,
        BedCode        varchar(10)  NOT NULL,
        Description    varchar(50)  NULL,
        Status         varchar(10)  NULL,
        PatientCode    varchar(20)  NULL,
        ServiceCode    varchar(10)  NULL,
        UserId         varchar(15)  NULL,
        UDate          date         NULL,
        UTime          varchar(5)   NULL,
        Retired        bit          NOT NULL CONSTRAINT DF_BED_POS_Retired DEFAULT(0),
        RetireComments varchar(500) NULL,
        CONSTRAINT PK_BED_POS PRIMARY KEY (Id)
    );
END

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID('dbo.BED_POS')
                 AND name = 'UX_BED_POS_BedCode_Active')
    EXEC('CREATE UNIQUE INDEX UX_BED_POS_BedCode_Active ON dbo.BED_POS (BedCode) WHERE Retired = 0;');

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID('dbo.BED_POS')
                 AND name = 'IX_BED_POS_Ward')
    EXEC('CREATE INDEX IX_BED_POS_Ward ON dbo.BED_POS (WardCode);');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.BED_POS', 'U') IS NOT NULL
    DROP TABLE dbo.BED_POS;

IF OBJECT_ID('dbo.WARD', 'U') IS NOT NULL
    DROP TABLE dbo.WARD;
");
        }
    }
}
