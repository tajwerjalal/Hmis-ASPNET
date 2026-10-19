-- ============================================================================
-- Migration : 04_pharmacy_ledger_schema.sql
-- Purpose   : Add the accounts ledger and the extra legacy masters to the
--             pharmacy schema, plus the columns the FULL Basement data turned
--             out to carry.
--
-- CONTEXT
--   Migration 03 was written against the reduced PHARSYS copy that contained
--   only BASEMENT\INVMAST. The complete Basement data was then copied in and
--   carries considerably more:
--
--     BASEMENT\CURRENT\MAINHEAD.DBF   80,168  voucher headers
--     BASEMENT\CURRENT\MAINDET.DBF   179,893  voucher lines
--     BASEMENT\CURRENT\CHART.DBF         636  chart of accounts
--     BASEMENT\CURRENT\GENERIC.DBF     1,193  generic names
--     BASEMENT\CURRENT\CATALOG.DBF        19  item catalogues
--     BASEMENT\CURRENT\INVMAST.DBF     9,234  drug master, now 27 fields
--
--   MAINHEAD/MAINDET are the double-entry ledger, not a stock table:
--     MAINHEAD  TTYPE, TNO, TDATE, DEBIT, CREDIT
--     MAINDET   TTYPE, TNO, TDATE, ACCODE, DESC, AMOUNT, CHQNO, BANK
--   with TTYPE 'PV' (48,087 purchase vouchers) and 'RV' (32,081 sales
--   vouchers). PUR_MAST/SAL_MAST carry GLNO, which joins a document to its
--   voucher.
--
--   This is the true source of OPEN BALANCE. The user asked for "their open
--   balance", and a balance taken from the item stock would be the stock
--   valuation; a balance taken from here is the amount owed to or by the
--   store. Both are provided, they are different things.
--
--   MAINDET.DESC carries the counterparty - 64,191 of the lines mention
--   "Satellite" (e.g. 'Medicine sale on credit to HMC Satellite'), which is
--   how the main store's receivable from the sub store is identified.
--
-- ADDITIVE-ONLY RULE: no pre-existing table or row is altered or dropped.
--   PHITEM/PHSUPPLIER/PHMANUFACTURER gain nullable columns only.
--
-- Developer : Tajwer Jalal
-- Date      : 19-Oct-2026
-- Time      : 11:48
-- ============================================================================
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

-- ============================================================================
-- 1. Extra columns the full Basement INVMAST / supplier / manufac carry
-- ============================================================================
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PHITEM') AND name = 'TRADEPRICE')
    -- Legacy TP: the tax-inclusive trade price, kept separate from TRADECOST
    -- (legacy TRADE) because the two differ once S_TAX_TP is non-zero.
    ALTER TABLE dbo.PHITEM ADD TRADEPRICE DECIMAL(18,4) NOT NULL CONSTRAINT DF_PHITEM_TRADEPRICE DEFAULT 0;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PHITEM') AND name = 'CATEGORYCODE')
    ALTER TABLE dbo.PHITEM ADD CATEGORYCODE VARCHAR(20) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PHITEM') AND name = 'GENERICCODE')
    ALTER TABLE dbo.PHITEM ADD GENERICCODE VARCHAR(20) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PHITEM') AND name = 'LOCATIONCODE')
    ALTER TABLE dbo.PHITEM ADD LOCATIONCODE VARCHAR(20) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PHITEM') AND name = 'HOSPITALCOST')
    -- Legacy HOSP_COST (carried on the transaction lines) promoted onto the
    -- master because it is a per-drug figure the pharmacy prices against.
    ALTER TABLE dbo.PHITEM ADD HOSPITALCOST DECIMAL(18,4) NOT NULL CONSTRAINT DF_PHITEM_HOSPITALCOST DEFAULT 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PHSUPPLIER') AND name = 'SHORTNAME')
    ALTER TABLE dbo.PHSUPPLIER ADD SHORTNAME VARCHAR(100) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PHSUPPLIER') AND name = 'SUPPLIERTYPE')
    ALTER TABLE dbo.PHSUPPLIER ADD SUPPLIERTYPE VARCHAR(1) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PHSUPPLIER') AND name = 'ACTIVE')
    ALTER TABLE dbo.PHSUPPLIER ADD ACTIVE BIT NOT NULL CONSTRAINT DF_PHSUPPLIER_ACTIVE DEFAULT 1;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PHSUPPLIER') AND name = 'BALANCE')
    ALTER TABLE dbo.PHSUPPLIER ADD BALANCE DECIMAL(18,4) NOT NULL CONSTRAINT DF_PHSUPPLIER_BALANCE DEFAULT 0;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PHMANUFACTURER') AND name = 'SHORTNAME')
    ALTER TABLE dbo.PHMANUFACTURER ADD SHORTNAME VARCHAR(100) NULL;
GO

-- The ledger joins documents to vouchers through GLNO, which no pharmacy table
-- carries yet.
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PHPURCHASE') AND name = 'GLNO')
    ALTER TABLE dbo.PHPURCHASE ADD GLNO VARCHAR(30) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PHSALE') AND name = 'GLNO')
    ALTER TABLE dbo.PHSALE ADD GLNO VARCHAR(30) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PHSALE') AND name = 'COUNTERPARTY')
    ALTER TABLE dbo.PHSALE ADD COUNTERPARTY VARCHAR(200) NULL;
GO
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PHPURCHASE') AND name = 'COUNTERPARTY')
    ALTER TABLE dbo.PHPURCHASE ADD COUNTERPARTY VARCHAR(200) NULL;
GO

-- ============================================================================
-- 2. PHGENERIC / PHCATEGORY - small masters referenced by INVMAST.GECODE/CACODE
-- ============================================================================
IF OBJECT_ID('dbo.PHGENERIC', 'U') IS NULL
CREATE TABLE dbo.PHGENERIC (
    ID      INT IDENTITY(1,1) NOT NULL,
    CODE    VARCHAR(20)  NOT NULL,
    NAME    VARCHAR(200) NOT NULL,
    RETIRED BIT          NOT NULL CONSTRAINT DF_PHGENERIC_RETIRED DEFAULT 0,
    CONSTRAINT PK_PHGENERIC PRIMARY KEY CLUSTERED (ID)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PHGENERIC_CODE' AND object_id = OBJECT_ID('dbo.PHGENERIC'))
    CREATE UNIQUE INDEX IX_PHGENERIC_CODE ON dbo.PHGENERIC (CODE);
GO

IF OBJECT_ID('dbo.PHCATEGORY', 'U') IS NULL
CREATE TABLE dbo.PHCATEGORY (
    ID      INT IDENTITY(1,1) NOT NULL,
    CODE    VARCHAR(20)  NOT NULL,
    NAME    VARCHAR(200) NOT NULL,
    RETIRED BIT          NOT NULL CONSTRAINT DF_PHCATEGORY_RETIRED DEFAULT 0,
    CONSTRAINT PK_PHCATEGORY PRIMARY KEY CLUSTERED (ID)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PHCATEGORY_CODE' AND object_id = OBJECT_ID('dbo.PHCATEGORY'))
    CREATE UNIQUE INDEX IX_PHCATEGORY_CODE ON dbo.PHCATEGORY (CODE);
GO

-- ============================================================================
-- 3. PHACCOUNT - chart of accounts (legacy CHART.dbf)
--    YDR / YCR are the legacy's own year-to-date debit/credit, i.e. the
--    balance as the legacy system last computed it. Kept verbatim so the
--    migrated figure can be compared against, not silently replaced.
-- ============================================================================
IF OBJECT_ID('dbo.PHACCOUNT', 'U') IS NULL
CREATE TABLE dbo.PHACCOUNT (
    ID           INT IDENTITY(1,1) NOT NULL,
    CODE         VARCHAR(20)   NOT NULL,
    NAME         VARCHAR(200)  NOT NULL,
    ACCOUNTTYPE  VARCHAR(1)    NULL,   -- legacy ACTYPE ('G' group, ...)
    LEVEL        INT           NOT NULL CONSTRAINT DF_PHACCOUNT_LEVEL DEFAULT 1,
    PARENTCODE   VARCHAR(20)   NULL,
    OPENINGBAL   DECIMAL(18,4) NOT NULL CONSTRAINT DF_PHACCOUNT_OPBAL DEFAULT 0,
    YEARDEBIT    DECIMAL(18,4) NOT NULL CONSTRAINT DF_PHACCOUNT_YDR   DEFAULT 0,
    YEARCREDIT   DECIMAL(18,4) NOT NULL CONSTRAINT DF_PHACCOUNT_YCR   DEFAULT 0,
    CONSTRAINT PK_PHACCOUNT PRIMARY KEY CLUSTERED (ID)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PHACCOUNT_CODE' AND object_id = OBJECT_ID('dbo.PHACCOUNT'))
    CREATE UNIQUE INDEX IX_PHACCOUNT_CODE ON dbo.PHACCOUNT (CODE);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PHACCOUNT_PARENT' AND object_id = OBJECT_ID('dbo.PHACCOUNT'))
    CREATE INDEX IX_PHACCOUNT_PARENT ON dbo.PHACCOUNT (PARENTCODE);
GO

-- ============================================================================
-- 4. PHVOUCHER / PHVOUCHERLINE - the double-entry ledger (MAINHEAD / MAINDET)
--    TTYPE is 'PV' for a purchase voucher and 'RV' for a sales voucher.
--    PUR_MAST/SAL_MAST.GLNO holds the VoucherId as text, which is how a
--    document is tied back to its accounting entry.
-- ============================================================================
IF OBJECT_ID('dbo.PHVOUCHER', 'U') IS NULL
CREATE TABLE dbo.PHVOUCHER (
    ID         INT IDENTITY(1,1) NOT NULL,
    DOCTYPE    VARCHAR(2)   NOT NULL,   -- PV | RV
    VOUCHERNO  VARCHAR(30)  NOT NULL,
    VOUCHERDATE DATE         NOT NULL,
    DEBITAMOUNT  DECIMAL(18,4) NOT NULL CONSTRAINT DF_PHVOUCHER_DEBIT  DEFAULT 0,
    CREDITAMOUNT DECIMAL(18,4) NOT NULL CONSTRAINT DF_PHVOUCHER_CREDIT DEFAULT 0,
    CREATEDAT  DATETIME2(0)  NOT NULL CONSTRAINT DF_PHVOUCHER_CREATED DEFAULT SYSDATETIME(),
    CONSTRAINT PK_PHVOUCHER PRIMARY KEY CLUSTERED (ID)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PHVOUCHER_DOCTYPE_NO' AND object_id = OBJECT_ID('dbo.PHVOUCHER'))
    CREATE UNIQUE INDEX IX_PHVOUCHER_DOCTYPE_NO ON dbo.PHVOUCHER (DOCTYPE, VOUCHERNO);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PHVOUCHER_DATE' AND object_id = OBJECT_ID('dbo.PHVOUCHER'))
    CREATE INDEX IX_PHVOUCHER_DATE ON dbo.PHVOUCHER (VOUCHERDATE);
GO

IF OBJECT_ID('dbo.PHVOUCHERLINE', 'U') IS NULL
CREATE TABLE dbo.PHVOUCHERLINE (
    ID          INT IDENTITY(1,1) NOT NULL,
    VOUCHERID   INT           NOT NULL,
    ACCOUNTCODE VARCHAR(20)   NULL,
    NARRATION   VARCHAR(200)  NULL,   -- legacy DESC - carries the counterparty
    AMOUNT      DECIMAL(18,4) NOT NULL CONSTRAINT DF_PHVOUCHERLINE_AMOUNT DEFAULT 0,
    CHEQUENO    VARCHAR(50)   NULL,
    BANK        VARCHAR(100)  NULL,
    CONSTRAINT PK_PHVOUCHERLINE PRIMARY KEY CLUSTERED (ID),
    CONSTRAINT FK_PHVOUCHERLINE_VOUCHER FOREIGN KEY (VOUCHERID) REFERENCES dbo.PHVOUCHER(ID)
);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PHVOUCHERLINE_VOUCHER' AND object_id = OBJECT_ID('dbo.PHVOUCHERLINE'))
    CREATE INDEX IX_PHVOUCHERLINE_VOUCHER ON dbo.PHVOUCHERLINE (VOUCHERID);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_PHVOUCHERLINE_ACCOUNT' AND object_id = OBJECT_ID('dbo.PHVOUCHERLINE'))
    CREATE INDEX IX_PHVOUCHERLINE_ACCOUNT ON dbo.PHVOUCHERLINE (ACCOUNTCODE);
GO

PRINT 'Pharmacy ledger + extra masters created.';
GO
