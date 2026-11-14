using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HmisWeb.Migrations
{
    /// <summary>
    /// compchg.DBF -> COMPANYSERVICERATE: realistic column types.
    ///
    /// The table was imported with blanket nvarchar(max) / float columns. This
    /// migration gives every column the width of the source file or of its
    /// real use:
    ///
    ///   CID      C(4)    -> varchar(4)
    ///   SCODE    C(5)    -> varchar(5)
    ///   SRATE    N(8,2)  -> decimal(8,2)   (money, not float)
    ///   URATE    N(8,2)  -> decimal(8,2)
    ///   DEPT     C(2)    -> varchar(2)
    ///   CHANGE   L       -> varchar(1) 'Y'/'N'
    ///   USERID   C(10)   -> varchar(10)
    ///   UDATE    D       -> date
    ///   UTIME    C(5)    -> varchar(5)
    ///
    /// plus the app columns (StaffRate / UnitRate / DiscountPercent as decimal,
    /// effective dates as date, retire comments as varchar(500)).
    ///
    /// Everything is written as guarded SQL because this environment already
    /// had the rebuild applied by hand - the migration must be a no-op there
    /// and do the work on a fresh database.
    ///
    /// The two indexes support the two real access paths: the live-key unique
    /// index guarantees one rate per company + service + department for
    /// billing, the plain index feeds the corporate rate card.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 22-Oct-2026
    /// Change Log: 22-Oct-2026 - created (compchg realistic schema).
    /// </summary>
    [DbContext(typeof(Data.ApplicationDbContext))]
    [Migration("20261022060000_CompchgRealisticTypes")]
    public partial class CompchgRealisticTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---- 1. column types ----
            // SET QUOTED_IDENTIFIER is required because the table carries a
            // filtered index; without it every ALTER fails with error 1934.
            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;

IF COL_LENGTH('dbo.COMPANYSERVICERATE', 'CompanyCode') IS NOT NULL
    ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN CompanyCode varchar(4) NULL;
IF COL_LENGTH('dbo.COMPANYSERVICERATE', 'ServiceCode') IS NOT NULL
    ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN ServiceCode varchar(5) NULL;
IF COL_LENGTH('dbo.COMPANYSERVICERATE', 'DEPT') IS NOT NULL
    ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN DEPT varchar(2) NULL;
IF COL_LENGTH('dbo.COMPANYSERVICERATE', 'UserId') IS NOT NULL
    ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN UserId varchar(10) NULL;
IF COL_LENGTH('dbo.COMPANYSERVICERATE', 'UTime') IS NOT NULL
    ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN UTime varchar(5) NULL;
IF COL_LENGTH('dbo.COMPANYSERVICERATE', 'ChangeFlag') IS NOT NULL
    ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN ChangeFlag varchar(1) NULL;
IF COL_LENGTH('dbo.COMPANYSERVICERATE', 'RetireComments') IS NOT NULL
    ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN RetireComments varchar(500) NULL;
IF COL_LENGTH('dbo.COMPANYSERVICERATE', 'Rate') IS NOT NULL
    ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN Rate decimal(8,2) NOT NULL;
IF COL_LENGTH('dbo.COMPANYSERVICERATE', 'URate') IS NOT NULL
    ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN URate decimal(8,2) NULL;
IF COL_LENGTH('dbo.COMPANYSERVICERATE', 'StaffRate') IS NOT NULL
    ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN StaffRate decimal(8,2) NULL;
IF COL_LENGTH('dbo.COMPANYSERVICERATE', 'UnitRate') IS NOT NULL
    ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN UnitRate decimal(8,2) NULL;
IF COL_LENGTH('dbo.COMPANYSERVICERATE', 'DiscountPercent') IS NOT NULL
    ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN DiscountPercent decimal(5,2) NULL;
IF COL_LENGTH('dbo.COMPANYSERVICERATE', 'EffectiveFrom') IS NOT NULL
    ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN EffectiveFrom date NULL;
IF COL_LENGTH('dbo.COMPANYSERVICERATE', 'EffectiveTo') IS NOT NULL
    ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN EffectiveTo date NULL;
IF COL_LENGTH('dbo.COMPANYSERVICERATE', 'UDate') IS NOT NULL
    ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN UDate date NULL;
");

            // ---- 2. duplicate live legacy keys ----
            // keep the latest record per company + service + department
            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;

WITH d AS (
    SELECT Id,
           ROW_NUMBER() OVER (
               PARTITION BY CompanyCode, ServiceCode, DEPT
               ORDER BY Id DESC) AS rn
    FROM dbo.COMPANYSERVICERATE
    WHERE Retired = 0
)
DELETE FROM dbo.COMPANYSERVICERATE
WHERE Id IN (SELECT Id FROM d WHERE rn > 1);
");

            // ---- 3. lookup indexes ----
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID('dbo.COMPANYSERVICERATE')
                 AND name = 'IX_COMPANYSERVICERATE_LegacyKey')
    EXEC('CREATE INDEX IX_COMPANYSERVICERATE_LegacyKey
          ON dbo.COMPANYSERVICERATE (CompanyCode, ServiceCode, DEPT)
          INCLUDE (Rate, URate, CompanyId, ItemId, StaffRate, UnitRate, DiscountPercent);');
");

            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID('dbo.COMPANYSERVICERATE')
                 AND name = 'UX_COMPANYSERVICERATE_Active')
    EXEC('CREATE UNIQUE INDEX UX_COMPANYSERVICERATE_Active
          ON dbo.COMPANYSERVICERATE (CompanyCode, ServiceCode, DEPT)
          WHERE Retired = 0;');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;

IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE object_id = OBJECT_ID('dbo.COMPANYSERVICERATE')
             AND name = 'UX_COMPANYSERVICERATE_Active')
    DROP INDEX UX_COMPANYSERVICERATE_Active ON dbo.COMPANYSERVICERATE;

IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE object_id = OBJECT_ID('dbo.COMPANYSERVICERATE')
             AND name = 'IX_COMPANYSERVICERATE_LegacyKey')
    DROP INDEX IX_COMPANYSERVICERATE_LegacyKey ON dbo.COMPANYSERVICERATE;

ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN CompanyCode nvarchar(max) NULL;
ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN ServiceCode nvarchar(max) NULL;
ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN DEPT nvarchar(max) NULL;
ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN UserId nvarchar(max) NULL;
ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN UTime nvarchar(max) NULL;
ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN ChangeFlag nvarchar(max) NULL;
ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN RetireComments nvarchar(max) NULL;
ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN Rate float NOT NULL;
ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN URate float NULL;
ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN StaffRate float NULL;
ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN UnitRate float NULL;
ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN DiscountPercent float NULL;
ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN EffectiveFrom datetime2 NULL;
ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN EffectiveTo datetime2 NULL;
ALTER TABLE dbo.COMPANYSERVICERATE ALTER COLUMN UDate datetime2 NULL;
");
        }
    }
}
