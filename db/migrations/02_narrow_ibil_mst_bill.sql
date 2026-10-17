-- ============================================================================
-- Migration : 02_narrow_ibil_mst_bill.sql
-- Purpose   : Narrow IBIL_MST.BILL from VARCHAR(20) to BIGINT.
--
-- WHY
--   The column is the bill-consumption stamp written by VFP GENBILLI:
--     NULL / 0 = unbilled, otherwise the IPDBILL.BILLNO that consumed the slip.
--   Every bill generation runs "WHERE HOS_CODE = @r AND (BILL IS NULL OR BILL = 0)"
--   over 768,037 rows, and a varchar comparison cannot seek efficiently.
--   The model (IbilMst.Bill) maps it as BIGINT?, so the varchar type was also
--   causing InvalidCastException at runtime.
--
-- SAFETY
--   Verified before conversion:
--     non-numeric values : 0 rows
--     numeric values     : 758,045
--     NULL               :   9,992
--     literal '0'        :  15,847   (unbilled)
--   The legacy '******' masking was already resolved to the sentinel -1 during
--   the original migration, so nothing is lost by converting to a number.
--
-- ADDITIVE-ONLY RULE: no row is deleted or updated; only the column type changes.
--
-- Developer : Tajwer Jalal
-- Date      : 12-Oct-2026
-- Time      : 12:30
-- ============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

-- Guard: abort loudly if any value cannot be converted.
IF EXISTS (SELECT 1 FROM dbo.IBIL_MST WHERE BILL IS NOT NULL AND TRY_CAST(BILL AS BIGINT) IS NULL)
BEGIN
    RAISERROR('IBIL_MST.BILL contains non-numeric values - aborting conversion.', 16, 1);
    RETURN;
END
GO

-- 1. Drop the old index that covers the old (varchar) column.
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IBIL_MST_BILL' AND object_id = OBJECT_ID('dbo.IBIL_MST'))
    DROP INDEX IX_IBIL_MST_BILL ON dbo.IBIL_MST;
GO

-- 2. Convert. ALTER COLUMN is metadata-only here because the type narrows
--    (varchar(20) -> bigint) and every value is already numeric.
ALTER TABLE dbo.IBIL_MST ALTER COLUMN BILL BIGINT NULL;
GO

-- 3. Recreate the index on the numeric column - makes the unbilled filter a seek.
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_IBIL_MST_BILL' AND object_id = OBJECT_ID('dbo.IBIL_MST'))
    CREATE INDEX IX_IBIL_MST_BILL ON dbo.IBIL_MST (BILL) INCLUDE (HOS_CODE, DEPT, NTOTAL, TYPE, SLIPDT);
GO

-- 4. Verification
SELECT 'IBIL_MST.BILL type'        AS chk,
       TYPE_NAME(system_type_id)   AS data_type,
       max_length
  FROM sys.columns
 WHERE object_id = OBJECT_ID('dbo.IBIL_MST') AND name = 'BILL';

SELECT 'unbilled slips'            AS chk, COUNT(*) AS n FROM dbo.IBIL_MST WHERE BILL IS NULL OR BILL = 0;
SELECT 'billed slips'              AS chk, COUNT(*) AS n FROM dbo.IBIL_MST WHERE BILL > 0;
SELECT 'sentinel -1'               AS chk, COUNT(*) AS n FROM dbo.IBIL_MST WHERE BILL = -1;
