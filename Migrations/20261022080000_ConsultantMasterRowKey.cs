using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HmisWeb.Migrations
{
    /// <summary>
    /// Consultant register: the surrogate primary key.
    ///
    /// The key cannot be called Id: the legacy row already has a column named
    /// ID (the consultant code) and SQL Server matches column names without
    /// case, so "Id" resolves to the legacy code column. The identity key is
    /// therefore RowId.
    ///
    /// Kept apart from 20261022070000_ConsultantMaster because that migration
    /// had already been applied when the name clash surfaced; this one is
    /// guarded, so it is safe on both an empty and a populated CONSULT.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 22-Oct-2026
    /// Time      : 12:45
    /// Change Log: 22-Oct-2026 12:45 - created (RowId identity key).
    /// </summary>
    [DbContext(typeof(Data.ApplicationDbContext))]
    [Migration("20261022080000_ConsultantMasterRowKey")]
    public partial class ConsultantMasterRowKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;

IF COL_LENGTH('dbo.CONSULT', 'RowId') IS NULL
BEGIN
    ALTER TABLE dbo.CONSULT ADD RowId bigint IDENTITY(1,1) NOT NULL;
    -- dynamic SQL: the batch is compiled before the new column exists
    EXEC('ALTER TABLE dbo.CONSULT ADD CONSTRAINT PK_CONSULT_ROW PRIMARY KEY (RowId);');
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
SET QUOTED_IDENTIFIER ON;

IF EXISTS (SELECT 1 FROM sys.key_constraints
           WHERE parent_object_id = OBJECT_ID('dbo.CONSULT')
             AND name = 'PK_CONSULT_ROW')
    ALTER TABLE dbo.CONSULT DROP CONSTRAINT PK_CONSULT_ROW;

IF COL_LENGTH('dbo.CONSULT', 'RowId') IS NOT NULL
    ALTER TABLE dbo.CONSULT DROP COLUMN RowId;
");
        }
    }
}
