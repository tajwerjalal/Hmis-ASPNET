// ============================================================================
// Migration   : 20261021104014 CompanyServiceRateLegacyReload
// Developer   : [TAJWER JALAL]
// Date        : 22-Oct-2026
// Purpose     : (1) the company service rates are reloaded from the legacy
//               VFP master compchg.DBF (21,197 active records), which is code
//               based (company code + service code + department code) - so
//               the table keeps the legacy codes next to the resolved ids
//               and both ids become optional;
//               (2) the corporate companies screen now reads the legacy
//               company master [COMPANY], which is mapped as a keyless query
//               type (the table has no primary key).
//
// Every step is IF-guarded, so the migration is safe on the live database
// where the same work was already executed by
// workspace\temp\company_service_rates_reload.sql.
//
// Backups    : COMPANYSERVICERATE_BAK_20261022 (before the lab purge) and
//              COMPANYSERVICERATE_BAK_20261022B (the state replaced by the
//              compchg reload).
// Change Log : 22-Oct-2026 - created (tool scaffold rewritten by hand).
// ============================================================================
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HmisWeb.Migrations
{
    /// <inheritdoc />
    public partial class CompanyServiceRateLegacyReload : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---------------------------------------------------------------
            // 1. legacy company master [COMPANY] - only created when it is
            //    missing (the live database already holds the 170 migrated
            //    company rows, the table has no primary key).
            // ---------------------------------------------------------------
            migrationBuilder.Sql(@"
IF OBJECT_ID('COMPANY') IS NULL
CREATE TABLE COMPANY (
    ID       NVARCHAR(150) NULL,
    NAME     NVARCHAR(150) NULL,
    ADDRESS  NVARCHAR(200) NULL,
    CITY     NVARCHAR(50)  NULL,
    ATTN     NVARCHAR(50)  NULL,
    DESG     NVARCHAR(50)  NULL,
    PHONE    NVARCHAR(50)  NULL,
    BILLMODE NVARCHAR(50)  NULL,
    STATUS   NVARCHAR(50)  NULL,
    USERID   NVARCHAR(15)  NULL,
    UDATE    DATETIME2     NULL,
    UTIME    NVARCHAR(5)   NULL
);
");

            // ---------------------------------------------------------------
            // 2. the legacy master columns of the rate table
            // ---------------------------------------------------------------
            migrationBuilder.Sql(@"
IF COL_LENGTH('COMPANYSERVICERATE','CompanyCode') IS NULL
    ALTER TABLE COMPANYSERVICERATE ADD CompanyCode NVARCHAR(10) NULL;
IF COL_LENGTH('COMPANYSERVICERATE','ServiceCode') IS NULL
    ALTER TABLE COMPANYSERVICERATE ADD ServiceCode NVARCHAR(10) NULL;
IF COL_LENGTH('COMPANYSERVICERATE','URate') IS NULL
    ALTER TABLE COMPANYSERVICERATE ADD URate float NULL;
IF COL_LENGTH('COMPANYSERVICERATE','DEPT') IS NULL
    ALTER TABLE COMPANYSERVICERATE ADD DEPT NVARCHAR(5) NULL;
IF COL_LENGTH('COMPANYSERVICERATE','UserId') IS NULL
    ALTER TABLE COMPANYSERVICERATE ADD UserId NVARCHAR(50) NULL;
IF COL_LENGTH('COMPANYSERVICERATE','UDate') IS NULL
    ALTER TABLE COMPANYSERVICERATE ADD UDate DATETIME2 NULL;
IF COL_LENGTH('COMPANYSERVICERATE','UTime') IS NULL
    ALTER TABLE COMPANYSERVICERATE ADD UTime NVARCHAR(5) NULL;
IF COL_LENGTH('COMPANYSERVICERATE','ChangeFlag') IS NULL
    ALTER TABLE COMPANYSERVICERATE ADD ChangeFlag NVARCHAR(1) NULL;
");

            // ---------------------------------------------------------------
            // 3. the ids become optional (214 legacy rows point at a service
            //    that was never migrated)
            // ---------------------------------------------------------------
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('COMPANYSERVICERATE') AND name = 'CompanyId' AND is_nullable = 0)
    ALTER TABLE COMPANYSERVICERATE ALTER COLUMN CompanyId bigint NULL;
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('COMPANYSERVICERATE') AND name = 'ItemId' AND is_nullable = 0)
    ALTER TABLE COMPANYSERVICERATE ALTER COLUMN ItemId bigint NULL;
");

            // ---------------------------------------------------------------
            // 4. the child foreign keys (dropped and re-created as optional)
            // ---------------------------------------------------------------
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_COMPANYSERVICERATE_INSTITUTION_CompanyId')
    ALTER TABLE COMPANYSERVICERATE DROP CONSTRAINT FK_COMPANYSERVICERATE_INSTITUTION_CompanyId;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_COMPANYSERVICERATE_ITEM_ItemId')
    ALTER TABLE COMPANYSERVICERATE DROP CONSTRAINT FK_COMPANYSERVICERATE_ITEM_ItemId;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_COMPANYSERVICERATE_INSTITUTION_CompanyId')
    ALTER TABLE COMPANYSERVICERATE ADD CONSTRAINT FK_COMPANYSERVICERATE_INSTITUTION_CompanyId
        FOREIGN KEY (CompanyId) REFERENCES INSTITUTION (Id);
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_COMPANYSERVICERATE_ITEM_ItemId')
    ALTER TABLE COMPANYSERVICERATE ADD CONSTRAINT FK_COMPANYSERVICERATE_ITEM_ItemId
        FOREIGN KEY (ItemId) REFERENCES ITEM (Id);
");

            // ---------------------------------------------------------------
            // 5. reload from the staged legacy file (only when the table is
            //    empty and the staging table is present)
            // ---------------------------------------------------------------
            migrationBuilder.Sql(@"
IF OBJECT_ID('COMPCHG_STAGE') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM COMPANYSERVICERATE)
BEGIN
    INSERT INTO COMPANYSERVICERATE
        (CompanyId, ItemId, Rate, URate, DiscountPercent, EffectiveFrom, EffectiveTo,
         Retired, RetireComments, CreaterId, CreatedAt, EditerId, EditedAt,
         StaffRate, UnitRate, DepartmentId,
         CompanyCode, ServiceCode, DEPT, UserId, UDate, UTime, ChangeFlag)
    SELECT inst.Id,
           itm.Id,
           ISNULL(s.SRATE, 0),
           ISNULL(s.URATE, 0),
           NULL, NULL, NULL,
           0, NULL, NULL, SYSDATETIME(), NULL, NULL,
           NULL, NULL, NULL,
           NULLIF(s.CID, ''), NULLIF(s.SCODE, ''), NULLIF(s.DEPT, ''),
           NULLIF(s.USERID, ''), s.UDATE, NULLIF(s.UTIME, ''), NULLIF(s.CHG, '')
    FROM   COMPCHG_STAGE s
    LEFT JOIN INSTITUTION inst ON inst.Code = s.CID
    LEFT JOIN ITEM itm
           ON itm.Id = COALESCE(
                  (SELECT TOP 1 i.Id FROM ITEM i WHERE i.Code = CASE s.DEPT
                         WHEN '01' THEN 'INV-LB-' + s.SCODE
                         WHEN '02' THEN 'INV-XR-' + s.SCODE
                         WHEN '03' THEN 'INV-US-' + s.SCODE
                         WHEN '04' THEN 'INV-DNT-' + s.SCODE
                         WHEN '06' THEN 'INV-OT-' + s.SCODE
                         WHEN '07' THEN 'INV-HSP-' + s.SCODE
                         WHEN '08' THEN 'INV-HSP-' + s.SCODE
                         WHEN '11' THEN 'INV-CNP-' + s.SCODE END),
                  (SELECT TOP 1 i.Id FROM ITEM i WHERE i.Code = s.SCODE));
END
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Rollback of the data reload is a restore of
            // COMPANYSERVICERATE_BAK_20261022B; the column changes are kept
            // because the reloaded rows carry the legacy codes.
            // Developer: [TAJWER JALAL]
        }
    }
}
