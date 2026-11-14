using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HmisWeb.Migrations
{
    /// <summary>
    /// Consultant rate card - loads the legacy file con_rat.DBF into
    /// [CONSULTANTSERVICERATE].
    ///
    /// The table already existed (consultant + item rates, one test row) but had
    /// no company, no ward class and no legacy stamps, so it could not hold the
    /// legacy rows. This migration:
    ///
    ///   * adds CompanyCode (con_rat.CMP_CD), ConsultantCode (CM_CD),
    ///     WardCode (WD_CD), ServiceCode (SCODE), RateFlag (RT_FLG) and the
    ///     USERID / UDATE / UTIME stamp;
    ///   * makes ConsultantId optional - 8 of the 108 codes in the file
    ///     (A16, A21, I03, J02, M09, N08, N09, V04) are no longer in STAFF;
    ///   * converts StaffRate / UnitRate from float to decimal(8,2), the money
    ///     type used by the other rate tables.
    ///
    /// Guarded per column / index, so it is safe on a database that was patched
    /// by hand. The rows themselves are loaded by
    /// workspace\temp\consultant_rate_import.sql (idempotent, upsert by
    /// company + consultant + ward + service).
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 22-Oct-2026
    /// Time      : 14:45
    /// Change Log: 22-Oct-2026 14:45 - created.
    /// </summary>
    [DbContext(typeof(Data.ApplicationDbContext))]
    [Migration("20261022110000_ConsultantRateLegacy")]
    public partial class ConsultantRateLegacy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;

IF COL_LENGTH('dbo.CONSULTANTSERVICERATE', 'ConsultantCode') IS NULL
    ALTER TABLE dbo.CONSULTANTSERVICERATE ADD ConsultantCode varchar(10) NULL;

IF COL_LENGTH('dbo.CONSULTANTSERVICERATE', 'CompanyCode') IS NULL
    ALTER TABLE dbo.CONSULTANTSERVICERATE ADD CompanyCode varchar(5) NULL;

IF COL_LENGTH('dbo.CONSULTANTSERVICERATE', 'WardCode') IS NULL
    ALTER TABLE dbo.CONSULTANTSERVICERATE ADD WardCode varchar(2) NULL;

IF COL_LENGTH('dbo.CONSULTANTSERVICERATE', 'ServiceCode') IS NULL
    ALTER TABLE dbo.CONSULTANTSERVICERATE ADD ServiceCode varchar(5) NULL;

IF COL_LENGTH('dbo.CONSULTANTSERVICERATE', 'RateFlag') IS NULL
    ALTER TABLE dbo.CONSULTANTSERVICERATE ADD RateFlag varchar(1) NULL;

IF COL_LENGTH('dbo.CONSULTANTSERVICERATE', 'UserId') IS NULL
    ALTER TABLE dbo.CONSULTANTSERVICERATE ADD UserId varchar(10) NULL;

IF COL_LENGTH('dbo.CONSULTANTSERVICERATE', 'UDate') IS NULL
    ALTER TABLE dbo.CONSULTANTSERVICERATE ADD UDate date NULL;

IF COL_LENGTH('dbo.CONSULTANTSERVICERATE', 'UTime') IS NULL
    ALTER TABLE dbo.CONSULTANTSERVICERATE ADD UTime varchar(5) NULL;

-- the legacy consultant codes that were dropped from STAFF keep ConsultantId NULL
IF EXISTS (SELECT 1
           FROM sys.columns c
           JOIN sys.types t ON t.user_type_id = c.user_type_id
           WHERE c.object_id = OBJECT_ID('dbo.CONSULTANTSERVICERATE')
             AND c.name = 'ConsultantId'
             AND c.is_nullable = 0)
    ALTER TABLE dbo.CONSULTANTSERVICERATE ALTER COLUMN ConsultantId bigint NULL;

-- money as decimal(8,2) (float -> decimal keeps the existing values).
-- Both columns carry a system-named default constraint, and SQL Server refuses
-- ALTER COLUMN while one exists, so the defaults are dropped and re-created.
DECLARE @dfStaff sysname, @dfUnit sysname;

SELECT @dfStaff = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.CONSULTANTSERVICERATE')
  AND c.name = 'StaffRate';

SELECT @dfUnit = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.CONSULTANTSERVICERATE')
  AND c.name = 'UnitRate';

IF @dfStaff IS NOT NULL
    EXEC('ALTER TABLE dbo.CONSULTANTSERVICERATE DROP CONSTRAINT ' + @dfStaff);

IF @dfUnit IS NOT NULL
    EXEC('ALTER TABLE dbo.CONSULTANTSERVICERATE DROP CONSTRAINT ' + @dfUnit);

IF EXISTS (SELECT 1
           FROM sys.columns c
           JOIN sys.types t ON t.user_type_id = c.user_type_id
           WHERE c.object_id = OBJECT_ID('dbo.CONSULTANTSERVICERATE')
             AND c.name = 'StaffRate'
             AND t.name = 'float')
    ALTER TABLE dbo.CONSULTANTSERVICERATE ALTER COLUMN StaffRate decimal(8,2) NOT NULL;

IF EXISTS (SELECT 1
           FROM sys.columns c
           JOIN sys.types t ON t.user_type_id = c.user_type_id
           WHERE c.object_id = OBJECT_ID('dbo.CONSULTANTSERVICERATE')
             AND c.name = 'UnitRate'
             AND t.name = 'float')
    ALTER TABLE dbo.CONSULTANTSERVICERATE ALTER COLUMN UnitRate decimal(8,2) NOT NULL;

IF NOT EXISTS (SELECT 1
               FROM sys.default_constraints dc
               JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
               WHERE dc.parent_object_id = OBJECT_ID('dbo.CONSULTANTSERVICERATE')
                 AND c.name = 'StaffRate')
    ALTER TABLE dbo.CONSULTANTSERVICERATE
        ADD CONSTRAINT DF_CONSULTANTSERVICERATE_StaffRate DEFAULT (0) FOR StaffRate;

IF NOT EXISTS (SELECT 1
               FROM sys.default_constraints dc
               JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
               WHERE dc.parent_object_id = OBJECT_ID('dbo.CONSULTANTSERVICERATE')
                 AND c.name = 'UnitRate')
    ALTER TABLE dbo.CONSULTANTSERVICERATE
        ADD CONSTRAINT DF_CONSULTANTSERVICERATE_UnitRate DEFAULT (0) FOR UnitRate;

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID('dbo.CONSULTANTSERVICERATE')
                 AND name = 'IX_CONSULTANTSERVICERATE_Company')
    EXEC('CREATE INDEX IX_CONSULTANTSERVICERATE_Company
          ON dbo.CONSULTANTSERVICERATE (CompanyCode, WardCode);');

IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID('dbo.CONSULTANTSERVICERATE')
                 AND name = 'IX_CONSULTANTSERVICERATE_ConsultantCode')
    EXEC('CREATE INDEX IX_CONSULTANTSERVICERATE_ConsultantCode
          ON dbo.CONSULTANTSERVICERATE (ConsultantCode);');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE object_id = OBJECT_ID('dbo.CONSULTANTSERVICERATE')
             AND name = 'IX_CONSULTANTSERVICERATE_Company')
    DROP INDEX IX_CONSULTANTSERVICERATE_Company ON dbo.CONSULTANTSERVICERATE;

IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE object_id = OBJECT_ID('dbo.CONSULTANTSERVICERATE')
             AND name = 'IX_CONSULTANTSERVICERATE_ConsultantCode')
    DROP INDEX IX_CONSULTANTSERVICERATE_ConsultantCode ON dbo.CONSULTANTSERVICERATE;

-- only restore NOT NULL when the legacy-only rows are gone
IF NOT EXISTS (SELECT 1 FROM dbo.CONSULTANTSERVICERATE WHERE ConsultantId IS NULL)
    ALTER TABLE dbo.CONSULTANTSERVICERATE ALTER COLUMN ConsultantId bigint NOT NULL;

-- decimal -> float: the default constraints have to go first (same reason as Up)
DECLARE @dfStaff sysname, @dfUnit sysname;

SELECT @dfStaff = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.CONSULTANTSERVICERATE')
  AND c.name = 'StaffRate';

SELECT @dfUnit = dc.name
FROM sys.default_constraints dc
JOIN sys.columns c ON c.object_id = dc.parent_object_id AND c.column_id = dc.parent_column_id
WHERE dc.parent_object_id = OBJECT_ID('dbo.CONSULTANTSERVICERATE')
  AND c.name = 'UnitRate';

IF @dfStaff IS NOT NULL
    EXEC('ALTER TABLE dbo.CONSULTANTSERVICERATE DROP CONSTRAINT ' + @dfStaff);

IF @dfUnit IS NOT NULL
    EXEC('ALTER TABLE dbo.CONSULTANTSERVICERATE DROP CONSTRAINT ' + @dfUnit);

ALTER TABLE dbo.CONSULTANTSERVICERATE ALTER COLUMN StaffRate float NOT NULL;
ALTER TABLE dbo.CONSULTANTSERVICERATE ALTER COLUMN UnitRate float NOT NULL;

ALTER TABLE dbo.CONSULTANTSERVICERATE
    ADD CONSTRAINT DF_CONSULTANTSERVICERATE_StaffRate DEFAULT (0) FOR StaffRate;
ALTER TABLE dbo.CONSULTANTSERVICERATE
    ADD CONSTRAINT DF_CONSULTANTSERVICERATE_UnitRate DEFAULT (0) FOR UnitRate;

IF COL_LENGTH('dbo.CONSULTANTSERVICERATE', 'ConsultantCode') IS NOT NULL
    ALTER TABLE dbo.CONSULTANTSERVICERATE DROP COLUMN ConsultantCode;

IF COL_LENGTH('dbo.CONSULTANTSERVICERATE', 'CompanyCode') IS NOT NULL
    ALTER TABLE dbo.CONSULTANTSERVICERATE DROP COLUMN CompanyCode;

IF COL_LENGTH('dbo.CONSULTANTSERVICERATE', 'WardCode') IS NOT NULL
    ALTER TABLE dbo.CONSULTANTSERVICERATE DROP COLUMN WardCode;

IF COL_LENGTH('dbo.CONSULTANTSERVICERATE', 'ServiceCode') IS NOT NULL
    ALTER TABLE dbo.CONSULTANTSERVICERATE DROP COLUMN ServiceCode;

IF COL_LENGTH('dbo.CONSULTANTSERVICERATE', 'RateFlag') IS NOT NULL
    ALTER TABLE dbo.CONSULTANTSERVICERATE DROP COLUMN RateFlag;

IF COL_LENGTH('dbo.CONSULTANTSERVICERATE', 'UserId') IS NOT NULL
    ALTER TABLE dbo.CONSULTANTSERVICERATE DROP COLUMN UserId;

IF COL_LENGTH('dbo.CONSULTANTSERVICERATE', 'UDate') IS NOT NULL
    ALTER TABLE dbo.CONSULTANTSERVICERATE DROP COLUMN UDate;

IF COL_LENGTH('dbo.CONSULTANTSERVICERATE', 'UTime') IS NOT NULL
    ALTER TABLE dbo.CONSULTANTSERVICERATE DROP COLUMN UTime;
");
        }
    }
}
