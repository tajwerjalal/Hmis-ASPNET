-- ============================================================================
-- Migration : 01_ipd_bill_schema.sql
-- Purpose   : Additive schema for the Indoor Patient (IPD Bill) form
--             (VFP BS0110 "Indoor Patient" replacement).
--
-- RULE APPLIED (user directive):
--   Tables that are ALREADY migrated and already hold data are NOT recreated
--   and NOT re-populated.  Only additive ALTER/CREATE of missing objects.
--
--   REGISTER  : migrated, 174,698 rows  -> ALTER ADD nullable columns only
--   IBIL_MST  : migrated,   768,037 rows -> surrogate Id only
--   IBIL_DET  : migrated, 3,744,809 rows -> surrogate Id only
--   IPDBILL   : migrated,    73,442 rows -> untouched
--   BCONTROL  : created earlier (1 row)  -> unique constraint only
--   IpdBillAudit  : missing -> CREATE
--   LegacyDepartment : missing -> CREATE + seed 8 rows
--
-- Business rules encoded here
--   * REGISTER.BILLSTATUS  0=Admitted 1=Discharged 2=Bill Updating
--                          3=Bill Generated 4=Settled
--   * IBIL_MST / IBIL_DET business keys are NOT unique
--       IBIL_MST (HOS_CODE,DEPT,SLIPNO) : 72 duplicate groups exist
--       IBIL_DET (DEPT,SLIPNO,CODE)     : 43,551 duplicate groups exist
--     so every editable grid row is addressed by the surrogate Id.
--   * No table is dropped, no row is deleted by this script.
--   * Side effects: IBIL_DET and IBIL_MST are rebuilt in place to add the
--     identity column (logged, ~733 MB / ~119 MB).  TempDB is used lightly;
--     no big SORT because identity add is a metadata + copy operation.
--
-- Developer : Tajwer Jalal
-- Date      : 12-Oct-2026
-- Time      : 12:04
-- ============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

-- ---------------------------------------------------------------------------
-- 1. REGISTER : status lifecycle columns (nullable, legacy imports untouched)
-- ---------------------------------------------------------------------------
IF COL_LENGTH('REGISTER','BILLSTATUS') IS NULL
    ALTER TABLE dbo.REGISTER ADD BILLSTATUS TINYINT NULL;

IF COL_LENGTH('REGISTER','BILLSTATUSDT') IS NULL
    ALTER TABLE dbo.REGISTER ADD BILLSTATUSDT DATETIME2(3) NULL;

IF COL_LENGTH('REGISTER','BILLSTATUSBY') IS NULL
    ALTER TABLE dbo.REGISTER ADD BILLSTATUSBY VARCHAR(16) NULL;
GO

-- Backfill (order matters). Existing rows keep their data; this only fills NULLs.
UPDATE dbo.REGISTER SET BILLSTATUS = 0 WHERE BILLSTATUS IS NULL AND DDATE IS NULL;
UPDATE dbo.REGISTER SET BILLSTATUS = 4 WHERE BILLSTATUS IS NULL AND SETTLE = 1;
UPDATE dbo.REGISTER SET BILLSTATUS = 3 WHERE BILLSTATUS IS NULL AND BILLGEN = 1;
UPDATE dbo.REGISTER SET BILLSTATUS = 1 WHERE BILLSTATUS IS NULL AND DDATE IS NOT NULL;
-- Anything still NULL and already billed/settled elsewhere -> Generated
UPDATE dbo.REGISTER SET BILLSTATUS = 3 WHERE BILLSTATUS IS NULL AND (BILLGEN = 1 OR SETTLE = 1);
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_REGISTER_BILLSTATUS' AND object_id=OBJECT_ID('REGISTER'))
    CREATE INDEX IX_REGISTER_BILLSTATUS ON dbo.REGISTER (TYPE, BILLSTATUS) INCLUDE (DDATE, BILLGEN, SETTLE);
GO

-- ---------------------------------------------------------------------------
-- 2. Surrogate keys for the editable grids (business keys are not unique)
-- ---------------------------------------------------------------------------
IF COL_LENGTH('IBIL_MST','Id') IS NULL
BEGIN
    ALTER TABLE dbo.IBIL_MST ADD Id BIGINT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_IBIL_MST_Id PRIMARY KEY CLUSTERED;
END
GO

IF COL_LENGTH('IBIL_DET','Id') IS NULL
BEGIN
    ALTER TABLE dbo.IBIL_DET ADD Id BIGINT IDENTITY(1,1) NOT NULL
        CONSTRAINT PK_IBIL_DET_Id PRIMARY KEY CLUSTERED;
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_IBIL_MST_HOSCODE' AND object_id=OBJECT_ID('IBIL_MST'))
    CREATE INDEX IX_IBIL_MST_HOSCODE ON dbo.IBIL_MST (HOS_CODE, DEPT) INCLUDE (SLIPNO, NTOTAL, TYPE, BILL);
GO

-- Detail lookup by (dept, slip) - the join used by the detail grid
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='IX_IBIL_DET_DEPT_SLIPNO' AND object_id=OBJECT_ID('IBIL_DET'))
    CREATE INDEX IX_IBIL_DET_DEPT_SLIPNO ON dbo.IBIL_DET (DEPT, SLIPNO) INCLUDE (CODE, QTY, RATE, NAMT, REM, CONCODE);
GO

-- ---------------------------------------------------------------------------
-- 3. BCONTROL : make the counter row impossible to duplicate
-- ---------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name='UQ_BCONTROL_Id' AND object_id=OBJECT_ID('BCONTROL'))
    ALTER TABLE dbo.BCONTROL ADD CONSTRAINT UQ_BCONTROL_Id UNIQUE (Id);
GO

-- ---------------------------------------------------------------------------
-- 4. IpdBillAudit : one row per audited action (never updated, never deleted)
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.IpdBillAudit') IS NULL
BEGIN
    CREATE TABLE dbo.IpdBillAudit
    (
        Id           BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_IpdBillAudit PRIMARY KEY CLUSTERED,
        Regno        VARCHAR(16)      NOT NULL,
        EncounterId  BIGINT           NULL,
        Action       VARCHAR(20)      NOT NULL,   -- LINE_ADD/LINE_EDIT/LINE_DEL/GENERATE/REVERSE/SETTLE/UNSETTLE/DISCOUNT/STATUS
        Entity       VARCHAR(20)      NOT NULL,   -- IBIL_DET / IBIL_MST / IPDBILL / REGISTER
        EntityKey    VARCHAR(80)      NULL,       -- dept|slipno|code  or  row Id
        Field        VARCHAR(40)      NULL,
        OldValue     NVARCHAR(200)    NULL,
        NewValue     NVARCHAR(200)    NULL,
        Reason       NVARCHAR(300)    NULL,       -- mandatory for REVERSE / UNSETTLE / DISCOUNT
        WebUserId    BIGINT           NULL,
        UserName     VARCHAR(80)      NULL,
        RoleName     VARCHAR(64)      NULL,
        CreatedAt    DATETIME2(3)     NOT NULL CONSTRAINT DF_IpdBillAudit_CreatedAt DEFAULT (SYSUTCDATETIME())
    );

    CREATE INDEX IX_IpdBillAudit_Regno_CreatedAt ON dbo.IpdBillAudit (Regno, CreatedAt);
    CREATE INDEX IX_IpdBillAudit_EntityKey       ON dbo.IpdBillAudit (Entity, EntityKey, CreatedAt);
END
GO

-- ---------------------------------------------------------------------------
-- 5. LegacyDepartment : the 8 legacy departments (replaces hard-coded dict)
-- ---------------------------------------------------------------------------
IF OBJECT_ID('dbo.LegacyDepartment') IS NULL
BEGIN
    CREATE TABLE dbo.LegacyDepartment
    (
        Id      INT           NOT NULL CONSTRAINT PK_LegacyDepartment PRIMARY KEY,
        Code    VARCHAR(2)    NOT NULL CONSTRAINT UQ_LegacyDepartment_Code UNIQUE,
        Name    VARCHAR(40)   NOT NULL,
        CatCode VARCHAR(10)   NOT NULL,   -- category code used to scope item lookup
        Retired BIT           NOT NULL CONSTRAINT DF_LegacyDepartment_Retired DEFAULT (0)
    );

    INSERT INTO dbo.LegacyDepartment (Id, Code, Name, CatCode) VALUES
        (1,'01','Laboratory',             'LAB'),
        (2,'02','X-Ray / CT Scan',        '02'),
        (3,'03','Ultra Sound',            '03'),
        (4,'04','Dental Clinic',          '04'),
        (5,'06','Operation Theatre',      '06'),
        (6,'07','Hospitalization',        '07'),
        (7,'10','Pharmacy',               '10'),
        (8,'11','Consultancy & Procedure','11');
END
GO

-- ---------------------------------------------------------------------------
-- 6. Report only : the 72 non-unique (HOS_CODE,DEPT,SLIPNO) groups
--    (kept as a query for manual review - no automatic merge, no deletes)
-- ---------------------------------------------------------------------------
-- SELECT HOS_CODE, DEPT, SLIPNO, COUNT(*) AS n
--   FROM dbo.IBIL_MST
--  GROUP BY HOS_CODE, DEPT, SLIPNO
-- HAVING COUNT(*) > 1
--  ORDER BY n DESC;

-- ---------------------------------------------------------------------------
-- 7. Verification
-- ---------------------------------------------------------------------------
SELECT 'REGISTER.BILLSTATUS backfill' AS chk,
       SUM(CASE WHEN BILLSTATUS = 0 THEN 1 ELSE 0 END) AS admitted,
       SUM(CASE WHEN BILLSTATUS = 1 THEN 1 ELSE 0 END) AS discharged,
       SUM(CASE WHEN BILLSTATUS = 3 THEN 1 ELSE 0 END) AS generated,
       SUM(CASE WHEN BILLSTATUS = 4 THEN 1 ELSE 0 END) AS settled,
       SUM(CASE WHEN BILLSTATUS IS NULL THEN 1 ELSE 0 END) AS still_null
  FROM dbo.REGISTER WHERE TYPE = 'I';

SELECT 'IBIL_MST.Id'  AS chk, COUNT(*) AS rows_with_id FROM dbo.IBIL_MST;
SELECT 'IBIL_DET.Id'  AS chk, COUNT(*) AS rows_with_id FROM dbo.IBIL_DET;
SELECT 'IpdBillAudit' AS chk, COUNT(*) AS row_count     FROM dbo.IpdBillAudit;
SELECT 'LegacyDepartment' AS chk, COUNT(*) AS row_count FROM dbo.LegacyDepartment;
