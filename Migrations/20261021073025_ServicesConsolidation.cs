// ============================================================================
// Migration   : 20261021073025 ServicesConsolidation
// Developer   : [TAJWER JALAL]
// Date        : 21-Oct-2026 (hand-authored body)
// Purpose     : ONE canonical department services master.
//               XRSERVICE (private XR copy of the same catalogue) is folded
//               into [SERVICES] and dropped. Also recreates (guarded) the
//               RECOVERY_* billing/recovery tables and the ledger REGNO/LCODE
//               bigint widening that were applied to production as raw SQL
//               before migrations caught up - so a fresh database reaches the
//               identical final state through this single chain.
//
// Everything inside Up() is IF-guarded, so re-running on a database that
// already carries the changes (HMIS live) is a no-op. Backups:
//               XRSERVICE_BAK_20261021 / SERVICES_BAK_20261021.
// Change Log  : 21-Oct-2026 - created (tool scaffold rewritten by hand).
// ============================================================================
using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HmisWeb.Migrations
{
    /// <inheritdoc />
    public partial class ServicesConsolidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ---------------------------------------------------------------
            // 0. backups of both source tables before any destructive step
            // ---------------------------------------------------------------
            migrationBuilder.Sql(@"
IF OBJECT_ID('XRSERVICE_BAK_20261021') IS NULL AND OBJECT_ID('XRSERVICE') IS NOT NULL
    SELECT * INTO XRSERVICE_BAK_20261021 FROM XRSERVICE;
IF OBJECT_ID('SERVICES_BAK_20261021') IS NULL
    SELECT * INTO SERVICES_BAK_20261021 FROM SERVICES;
");

            // ---------------------------------------------------------------
            // 1. ledger REGNO/LCODE widening was DROPPED from this migration
            //    (22-Oct-2026): the legacy columns are primary-key members
            //    (PK_CASHCNT depends on LCODE), so an in-place int->bigint
            //    ALTER cannot run; the app maps them as long and SQL Server
            //    converts transparently. Revisit only with a proper
            //    drop-PK/alter/recreate-PK script.
            // ---------------------------------------------------------------


            // ---------------------------------------------------------------
            // 2. RECOVERY_* tables (guard: create only when absent)
            // ---------------------------------------------------------------
            migrationBuilder.Sql(@"
IF OBJECT_ID('RECOVERY_AUDIT') IS NULL
CREATE TABLE RECOVERY_AUDIT (
    Id         BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    Action     NVARCHAR(20)  NULL,
    Entity     NVARCHAR(20)  NULL,
    EntityKey  NVARCHAR(80)  NULL,
    Field      NVARCHAR(40)  NULL,
    OldValue   NVARCHAR(200) NULL,
    NewValue   NVARCHAR(200) NULL,
    Reason     NVARCHAR(300) NULL,
    InvoiceId  BIGINT        NULL,
    WebUserId  BIGINT        NULL,
    UserName   NVARCHAR(80)  NULL,
    RoleName   NVARCHAR(64)  NULL,
    CreatedAt  DATETIME2     NOT NULL
);
IF OBJECT_ID('RECOVERY_AUDIT') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RECOVERY_AUDIT_Entity_EntityKey_CreatedAt')
    CREATE INDEX IX_RECOVERY_AUDIT_Entity_EntityKey_CreatedAt ON RECOVERY_AUDIT (Entity, EntityKey, CreatedAt);
IF OBJECT_ID('RECOVERY_AUDIT') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RECOVERY_AUDIT_InvoiceId')
    CREATE INDEX IX_RECOVERY_AUDIT_InvoiceId ON RECOVERY_AUDIT (InvoiceId);

IF OBJECT_ID('RECOVERY_INV') IS NULL
CREATE TABLE RECOVERY_INV (
    Id             BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    DocNo          NVARCHAR(20)  NULL,
    DocType        NVARCHAR(1)   NULL,
    CompanyCode    NVARCHAR(8)   NULL,
    InvoiceDate    DATETIME2     NOT NULL,
    SubmissionDate DATETIME2     NULL,
    BillMonth      INT           NULL,
    BillYear       INT           NULL,
    Remarks        NVARCHAR(200) NULL,
    TotAmt         DECIMAL(18,2) NOT NULL,
    Status         NVARCHAR(1)   NULL,
    CreatedBy      NVARCHAR(40)  NULL,
    CreatedAt      DATETIME2     NOT NULL,
    EditedBy       NVARCHAR(40)  NULL,
    EditedAt       DATETIME2     NULL,
    RowVer         ROWVERSION
);
IF OBJECT_ID('RECOVERY_INV') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RECOVERY_INV_CompanyCode')
    CREATE INDEX IX_RECOVERY_INV_CompanyCode ON RECOVERY_INV (CompanyCode);
IF OBJECT_ID('RECOVERY_INV') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RECOVERY_INV_DocNo')
    CREATE UNIQUE INDEX IX_RECOVERY_INV_DocNo ON RECOVERY_INV (DocNo) WHERE [DocNo] IS NOT NULL;
IF OBJECT_ID('RECOVERY_INV') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RECOVERY_INV_InvoiceDate')
    CREATE INDEX IX_RECOVERY_INV_InvoiceDate ON RECOVERY_INV (InvoiceDate);
IF OBJECT_ID('RECOVERY_INV') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RECOVERY_INV_BillYear_BillMonth')
    CREATE INDEX IX_RECOVERY_INV_BillYear_BillMonth ON RECOVERY_INV (BillYear, BillMonth);

IF OBJECT_ID('RECOVERY_INVDET') IS NULL
CREATE TABLE RECOVERY_INVDET (
    Id        BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    InvoiceId BIGINT        NOT NULL,
    RegNo     NVARCHAR(16)  NULL,
    BillAmt   DECIMAL(18,2) NOT NULL,
    Remarks   NVARCHAR(100) NULL,
    CreatedBy NVARCHAR(40)  NULL,
    CreatedAt DATETIME2     NULL
);
IF OBJECT_ID('RECOVERY_INVDET') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_RECOVERY_INVDET_RECOVERY_INV_InvoiceId')
    ALTER TABLE RECOVERY_INVDET ADD CONSTRAINT FK_RECOVERY_INVDET_RECOVERY_INV_InvoiceId
        FOREIGN KEY (InvoiceId) REFERENCES RECOVERY_INV (Id);
IF OBJECT_ID('RECOVERY_INVDET') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RECOVERY_INVDET_InvoiceId_RegNo')
    CREATE UNIQUE INDEX IX_RECOVERY_INVDET_InvoiceId_RegNo ON RECOVERY_INVDET (InvoiceId, RegNo) WHERE [RegNo] IS NOT NULL;
IF OBJECT_ID('RECOVERY_INVDET') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RECOVERY_INVDET_RegNo')
    CREATE INDEX IX_RECOVERY_INVDET_RegNo ON RECOVERY_INVDET (RegNo);

IF OBJECT_ID('RECOVERY_RCPT') IS NULL
CREATE TABLE RECOVERY_RCPT (
    Id         BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ReceiptNo  NVARCHAR(20)  NULL,
    Method     NVARCHAR(1)   NULL,
    RcptDate   DATETIME2     NOT NULL,
    CompanyCode NVARCHAR(8)  NULL,
    ChequeNo   NVARCHAR(32)  NULL,
    Bank       NVARCHAR(80)  NULL,
    ChequeDate DATETIME2     NULL,
    RcvdBy     NVARCHAR(40)  NULL,
    TotAmt     DECIMAL(18,2) NOT NULL,
    Status     NVARCHAR(1)   NULL,
    Remarks    NVARCHAR(200) NULL,
    CreatedBy  NVARCHAR(40)  NULL,
    CreatedAt  DATETIME2     NOT NULL,
    EditedBy   NVARCHAR(40)  NULL,
    EditedAt   DATETIME2     NULL,
    RowVer     ROWVERSION
);
IF OBJECT_ID('RECOVERY_RCPT') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RECOVERY_RCPT_CompanyCode')
    CREATE INDEX IX_RECOVERY_RCPT_CompanyCode ON RECOVERY_RCPT (CompanyCode);
IF OBJECT_ID('RECOVERY_RCPT') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RECOVERY_RCPT_RcptDate')
    CREATE INDEX IX_RECOVERY_RCPT_RcptDate ON RECOVERY_RCPT (RcptDate);
IF OBJECT_ID('RECOVERY_RCPT') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RECOVERY_RCPT_ReceiptNo')
    CREATE UNIQUE INDEX IX_RECOVERY_RCPT_ReceiptNo ON RECOVERY_RCPT (ReceiptNo) WHERE [ReceiptNo] IS NOT NULL;
IF OBJECT_ID('RECOVERY_RCPT') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RECOVERY_RCPT_Status')
    CREATE INDEX IX_RECOVERY_RCPT_Status ON RECOVERY_RCPT (Status);

IF OBJECT_ID('RECOVERY_RCPT_ALLOC') IS NULL
CREATE TABLE RECOVERY_RCPT_ALLOC (
    Id        BIGINT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    ReceiptId BIGINT        NOT NULL,
    InvoiceId BIGINT        NULL,
    RegNo     NVARCHAR(16)  NULL,
    RcptAmt   DECIMAL(18,2) NOT NULL,
    ItaxDed   DECIMAL(18,2) NOT NULL,
    OthDed    DECIMAL(18,2) NOT NULL,
    Remarks   NVARCHAR(50)  NULL,
    CreatedBy NVARCHAR(40)  NULL,
    CreatedAt DATETIME2     NULL
);
IF OBJECT_ID('RECOVERY_RCPT_ALLOC') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name='FK_RECOVERY_RCPT_ALLOC_RECOVERY_RCPT_ReceiptId')
    ALTER TABLE RECOVERY_RCPT_ALLOC ADD CONSTRAINT FK_RECOVERY_RCPT_ALLOC_RECOVERY_RCPT_ReceiptId
        FOREIGN KEY (ReceiptId) REFERENCES RECOVERY_RCPT (Id);
IF OBJECT_ID('RECOVERY_RCPT_ALLOC') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RECOVERY_RCPT_ALLOC_InvoiceId')
    CREATE INDEX IX_RECOVERY_RCPT_ALLOC_InvoiceId ON RECOVERY_RCPT_ALLOC (InvoiceId);
IF OBJECT_ID('RECOVERY_RCPT_ALLOC') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RECOVERY_RCPT_ALLOC_ReceiptId_RegNo')
    CREATE UNIQUE INDEX IX_RECOVERY_RCPT_ALLOC_ReceiptId_RegNo ON RECOVERY_RCPT_ALLOC (ReceiptId, RegNo) WHERE [RegNo] IS NOT NULL;
IF OBJECT_ID('RECOVERY_RCPT_ALLOC') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_RECOVERY_RCPT_ALLOC_RegNo')
    CREATE INDEX IX_RECOVERY_RCPT_ALLOC_RegNo ON RECOVERY_RCPT_ALLOC (RegNo);
");

            // ---------------------------------------------------------------
            // 3. XRSERVICE -> SERVICES consolidation (all steps guarded)
            // ---------------------------------------------------------------
            // 3a. add the app columns to the legacy master
            migrationBuilder.Sql(@"
IF COL_LENGTH('SERVICES','Id') IS NULL
    ALTER TABLE SERVICES ADD Id BIGINT IDENTITY(1,1);
IF COL_LENGTH('SERVICES','SNAME') IS NULL
    ALTER TABLE SERVICES ADD SNAME NVARCHAR(200) NULL;
IF COL_LENGTH('SERVICES','PRIORITY') IS NULL
    ALTER TABLE SERVICES ADD PRIORITY INT NULL;
IF COL_LENGTH('SERVICES','CATEGORYID') IS NULL
    ALTER TABLE SERVICES ADD CATEGORYID BIGINT NULL;
IF COL_LENGTH('SERVICES','BILLABLE') IS NULL
    ALTER TABLE SERVICES ADD BILLABLE BIT NOT NULL CONSTRAINT DF_SERVICES_BILLABLE DEFAULT (1);
IF COL_LENGTH('SERVICES','RETIRED') IS NULL
    ALTER TABLE SERVICES ADD RETIRED BIT NOT NULL CONSTRAINT DF_SERVICES_RETIRED DEFAULT (0);
IF COL_LENGTH('SERVICES','RETIREDAT') IS NULL
    ALTER TABLE SERVICES ADD RETIREDAT DATETIME2(0) NULL;
IF COL_LENGTH('SERVICES','RETCOMMENTS') IS NULL
    ALTER TABLE SERVICES ADD RETCOMMENTS NVARCHAR(200) NULL;
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name = 'PK_SERVICES')
    ALTER TABLE SERVICES ADD CONSTRAINT PK_SERVICES PRIMARY KEY (Id);
IF COL_LENGTH('SERVICES','CATEGORYID') IS NOT NULL AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_SERVICES_XRCATEGORY')
    ALTER TABLE SERVICES ADD CONSTRAINT FK_SERVICES_XRCATEGORY
        FOREIGN KEY (CATEGORYID) REFERENCES XRSERVICECATEGORY (Id);
");

            // 3b. normalise the one 6-char code + its billing mirror item
            migrationBuilder.Sql(@"
IF OBJECT_ID('XRSERVICE') IS NOT NULL
BEGIN
    UPDATE XRSERVICE SET Code = 'XR001' WHERE Code = 'XR-001';
END
UPDATE ITEM SET Code = 'INV-XR-XR001' WHERE Code = 'INV-XR-XR-001';
");

            // 3c. carry the XR-only row + stamp the code-matched rows
            migrationBuilder.Sql(@"
IF OBJECT_ID('XRSERVICE') IS NOT NULL
BEGIN
    INSERT INTO SERVICES (CODE, [DESC], SNAME, SRATE, URATE, DEPT, PRES,
                          PRIORITY, CATEGORYID, BILLABLE, ACTIVE, RETIRED,
                          RETIREDAT, RETCOMMENTS, USERID, UDATE, UTIME, [CHANGE])
    SELECT x.Code, x.[Name], x.Sname,
           CAST(ISNULL(x.Rate, 0) AS numeric(18,2)),
           CAST(ISNULL(x.RateForeigner, 0) AS numeric(18,2)),
           '02',                                -- X-RAY/CT SCAN dept code
           x.Requirements, x.Priority, x.CategoryId, x.Billable,
           CASE WHEN x.Active = 1 THEN 'A' ELSE 'I' END,
           x.Retired, x.RetiredAt, x.RetireComments,
           'XR-SYNC',
           CAST(ISNULL(x.CreatedAt, SYSUTCDATETIME()) AS date),
           CONVERT(varchar(5), ISNULL(x.CreatedAt, SYSUTCDATETIME()), 108),
           'M'
    FROM XRSERVICE x
    WHERE NOT EXISTS (SELECT 1 FROM SERVICES s WHERE s.CODE = x.Code);

    -- stamp the app columns on code-matched rows; legacy DESC/SRATE/URATE
    -- stay authoritative (verified identical rates in the audit)
    UPDATE s
    SET    s.SNAME       = x.Sname,
           s.PRIORITY    = x.Priority,
           s.CATEGORYID  = x.CategoryId,
           s.BILLABLE    = x.Billable,
           s.ACTIVE      = CASE WHEN x.Active = 1 THEN 'A' ELSE 'I' END,
           s.RETIRED     = x.Retired,
           s.RETIREDAT   = x.RetiredAt,
           s.RETCOMMENTS = x.RetireComments,
           s.PRES        = x.Requirements,
           s.[CHANGE]    = 'M'
    FROM  SERVICES s
    JOIN  XRSERVICE x ON s.CODE = x.Code;
END
");

            // 3d. drop the child FKs BEFORE remapping ids (new SERVICES Ids fall
            //     outside the old XRSERVICE Id domain), remap by normalized code
            migrationBuilder.Sql(@"
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_XRORDER_XRSERVICE_XrServiceId')
    ALTER TABLE XRORDER DROP CONSTRAINT FK_XRORDER_XRSERVICE_XrServiceId;
IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_XREPORTTEMPLATE_XRSERVICE_XrServiceId')
    ALTER TABLE XREPORTTEMPLATE DROP CONSTRAINT FK_XREPORTTEMPLATE_XRSERVICE_XrServiceId;
GO
IF OBJECT_ID('XRSERVICE') IS NOT NULL
BEGIN
    UPDATE o SET o.XrServiceId = s.Id
    FROM XRORDER o
    JOIN XRSERVICE x ON o.XrServiceId = x.Id
    JOIN SERVICES  s ON s.CODE = REPLACE(x.Code, '-', '');

    UPDATE o SET o.XrServiceId = s.Id
    FROM XREPORTTEMPLATE o
    JOIN XRSERVICE x ON o.XrServiceId = x.Id
    JOIN SERVICES  s ON s.CODE = REPLACE(x.Code, '-', '');
END
");

            // 3e. drop XRSERVICE and point the children at the shared master
            migrationBuilder.Sql(@"
IF OBJECT_ID('XRSERVICE') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM XRORDER o JOIN XRSERVICE x ON o.XrServiceId = x.Id)
        RAISERROR('XRORDER still references XRSERVICE - aborting.', 16, 1);
    IF EXISTS (SELECT 1 FROM XREPORTTEMPLATE o JOIN XRSERVICE x ON o.XrServiceId = x.Id)
        RAISERROR('XREPORTTEMPLATE still references XRSERVICE - aborting.', 16, 1);

    DROP TABLE XRSERVICE;
END
IF OBJECT_ID('XRSERVICE') IS NULL AND OBJECT_ID('XRORDER') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_XRORDER_SERVICES_XrServiceId')
    ALTER TABLE XRORDER ADD CONSTRAINT FK_XRORDER_SERVICES_XrServiceId
        FOREIGN KEY (XrServiceId) REFERENCES SERVICES (Id);
IF OBJECT_ID('XRSERVICE') IS NULL AND OBJECT_ID('XREPORTTEMPLATE') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_XREPORTTEMPLATE_SERVICES_XrServiceId')
    ALTER TABLE XREPORTTEMPLATE ADD CONSTRAINT FK_XREPORTTEMPLATE_SERVICES_XrServiceId
        FOREIGN KEY (XrServiceId) REFERENCES SERVICES (Id);
");

            // 3f. scope indexes on the shared master (kept by the model too)
            migrationBuilder.Sql(@"
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SERVICES_DEPT')
    CREATE INDEX IX_SERVICES_DEPT ON SERVICES (DEPT);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SERVICES_CODE')
    CREATE INDEX IX_SERVICES_CODE ON SERVICES (CODE);
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_SERVICES_CATEGORYID')
    CREATE INDEX IX_SERVICES_CATEGORYID ON SERVICES (CATEGORYID);
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Down is intentionally a no-op guard: this migration folds a legacy
            // private table into the shared master. True rollback would mean
            // restoring both pre-merge copies
            // (XRSERVICE_BAK_20261021 / SERVICES_BAK_20261021) and re-attaching
            // the child FKs - not something EF should attempt automatically.
            //
            // Reported to the user for manual recovery procedures.
            // Developer: [TAJWER JALAL]
        }
    }
}
