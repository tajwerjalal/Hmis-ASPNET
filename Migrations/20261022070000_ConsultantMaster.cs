using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HmisWeb.Migrations
{
    /// <summary>
    /// Consultant register: [HMIS].[dbo].[CONSULT] -> manageable master.
    ///
    /// The legacy table arrived with ten varchar(4000) columns and no key.
    /// This migration gives every column the width of the source file and adds
    /// what a maintenance screen needs:
    ///
    ///   PREFIX   varchar(10)    NAME   varchar(100)   ID     varchar(5)
    ///   TITLE    varchar(100)   PHONE  varchar(50)    DIGREE varchar(120)
    ///   DEPT     varchar(4)     USERID varchar(15)    UDATE  varchar(8)
    ///   UTIME    varchar(5)
    ///
    ///   RowId          bigint identity primary key, added by
    ///                  20261022080000_ConsultantMasterRowKey (the legacy code
    ///                  column is ID and Id/ID are one name to SQL Server)
    ///   Retired        bit, soft-retire flag
    ///   RetireComments varchar(500), retire reason
    ///
    /// Every statement is guarded: the migration is safe on a database where the
    /// work was already applied by hand, and it performs the change on a fresh
    /// one. The legacy UDATE/UTIME/USERID stamp is kept as text on purpose - it
    /// is the stamp the old forms printed, not a datetime.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 22-Oct-2026
    /// Time      : 12:10
    /// Change Log: 22-Oct-2026 12:10 - created (consultant master: widths,
    ///             identity key, retire columns, code index).
    /// </summary>
    [DbContext(typeof(Data.ApplicationDbContext))]
    [Migration("20261022070000_ConsultantMaster")]
    public partial class ConsultantMaster : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1) surrogate key: the legacy consultant code is not unique, so the
            //    row identity has to come from the database. It is added by
            //    20261022080000_ConsultantMasterRowKey as RowId - the name Id
            //    cannot be used because the legacy code column is ID and SQL
            //    Server compares column names without case.

            // 2) soft-retire columns (the screen never deletes a consultant)
            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;

IF COL_LENGTH('dbo.CONSULT', 'Retired') IS NULL
    ALTER TABLE dbo.CONSULT ADD Retired bit NOT NULL
        CONSTRAINT DF_CONSULT_Retired DEFAULT(0);
IF COL_LENGTH('dbo.CONSULT', 'RetireComments') IS NULL
    ALTER TABLE dbo.CONSULT ADD RetireComments varchar(500) NULL;
");

            // 3) realistic widths (measured against the 201 live rows)
            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;

IF COL_LENGTH('dbo.CONSULT', 'PREFIX') IS NOT NULL
    ALTER TABLE dbo.CONSULT ALTER COLUMN PREFIX varchar(10) NULL;
IF COL_LENGTH('dbo.CONSULT', 'NAME') IS NOT NULL
    ALTER TABLE dbo.CONSULT ALTER COLUMN NAME varchar(100) NULL;
IF COL_LENGTH('dbo.CONSULT', 'ID') IS NOT NULL
    ALTER TABLE dbo.CONSULT ALTER COLUMN ID varchar(5) NULL;
IF COL_LENGTH('dbo.CONSULT', 'TITLE') IS NOT NULL
    ALTER TABLE dbo.CONSULT ALTER COLUMN TITLE varchar(100) NULL;
IF COL_LENGTH('dbo.CONSULT', 'PHONE') IS NOT NULL
    ALTER TABLE dbo.CONSULT ALTER COLUMN PHONE varchar(50) NULL;
IF COL_LENGTH('dbo.CONSULT', 'DIGREE') IS NOT NULL
    ALTER TABLE dbo.CONSULT ALTER COLUMN DIGREE varchar(120) NULL;
IF COL_LENGTH('dbo.CONSULT', 'DEPT') IS NOT NULL
    ALTER TABLE dbo.CONSULT ALTER COLUMN DEPT varchar(4) NULL;
IF COL_LENGTH('dbo.CONSULT', 'USERID') IS NOT NULL
    ALTER TABLE dbo.CONSULT ALTER COLUMN USERID varchar(15) NULL;
IF COL_LENGTH('dbo.CONSULT', 'UDATE') IS NOT NULL
    ALTER TABLE dbo.CONSULT ALTER COLUMN UDATE varchar(8) NULL;
IF COL_LENGTH('dbo.CONSULT', 'UTIME') IS NOT NULL
    ALTER TABLE dbo.CONSULT ALTER COLUMN UTIME varchar(5) NULL;
");

            // 4) the consultant code lookup (the link to STAFF.Code)
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes
               WHERE object_id = OBJECT_ID('dbo.CONSULT')
                 AND name = 'IX_CONSULT_ID')
    EXEC('CREATE INDEX IX_CONSULT_ID ON dbo.CONSULT (ID);');
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;

IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE object_id = OBJECT_ID('dbo.CONSULT')
             AND name = 'IX_CONSULT_ID')
    DROP INDEX IX_CONSULT_ID ON dbo.CONSULT;

IF EXISTS (SELECT 1 FROM sys.key_constraints
           WHERE parent_object_id = OBJECT_ID('dbo.CONSULT')
             AND name = 'PK_CONSULT')
    ALTER TABLE dbo.CONSULT DROP CONSTRAINT PK_CONSULT;

IF COL_LENGTH('dbo.CONSULT', 'Id') IS NOT NULL
    ALTER TABLE dbo.CONSULT DROP COLUMN Id;

IF COL_LENGTH('dbo.CONSULT', 'RetireComments') IS NOT NULL
    ALTER TABLE dbo.CONSULT DROP COLUMN RetireComments;

IF COL_LENGTH('dbo.CONSULT', 'Retired') IS NOT NULL
    ALTER TABLE dbo.CONSULT DROP COLUMN Retired;

ALTER TABLE dbo.CONSULT ALTER COLUMN PREFIX varchar(4000) NULL;
ALTER TABLE dbo.CONSULT ALTER COLUMN NAME varchar(4000) NULL;
ALTER TABLE dbo.CONSULT ALTER COLUMN ID varchar(4000) NULL;
ALTER TABLE dbo.CONSULT ALTER COLUMN TITLE varchar(4000) NULL;
ALTER TABLE dbo.CONSULT ALTER COLUMN PHONE varchar(4000) NULL;
ALTER TABLE dbo.CONSULT ALTER COLUMN DIGREE varchar(4000) NULL;
ALTER TABLE dbo.CONSULT ALTER COLUMN DEPT varchar(4000) NULL;
ALTER TABLE dbo.CONSULT ALTER COLUMN USERID varchar(4000) NULL;
ALTER TABLE dbo.CONSULT ALTER COLUMN UDATE varchar(4000) NULL;
ALTER TABLE dbo.CONSULT ALTER COLUMN UTIME varchar(4000) NULL;
");
        }
    }
}
