using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HmisWeb.Migrations
{
    /// <summary>
    /// Registration screens follow-up: ward / bed movements and panel company
    /// transfers.
    ///
    ///   [PATIENTENCOUNTER] - extended with the transfer-time company data of the
    ///       legacy register (employee code / name / relation, company card,
    ///       reference valid upto, bed entitlement) and the discharge status.
    ///   [PATIENTROOM] - now carries the ward + bed position the patient is
    ///       staying in, so a stay is linked to [WARD]/[BED_POS] instead of the
    ///       empty legacy [ROOM] master.
    ///   [BEDTRANSFER]  - new movement history from the Ward Transfer screen
    ///       (bedtrans.scx): admission bed assignment + transfers, with the
    ///       generated hospitalisation indent.
    ///   [COMPANYTRANSFER] - new panel-company transfer from the Company Transfer
    ///       screen (cmptrans.scx): from company -> to company, employee, card,
    ///       bed entitlement and the "update rates in all indents" flag.
    ///
    /// Guarded: the new tables are only created when they are missing and the
    /// added columns are added when they do not exist yet, so the migration can
    /// be re-run safely after the scratch database is rebuilt.
    ///
    /// Developer : [TAJWER JALAL]
    /// Date      : 22-Oct-2026
    /// Time      : 16:40
    /// Change Log: 22-Oct-2026 - created (registration transfers).
    /// </summary>
    [DbContext(typeof(Data.ApplicationDbContext))]
    [Migration("20261022120000_RegistrationTransfers")]
    public partial class RegistrationTransfers : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.PATIENTENCOUNTER', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('dbo.PATIENTENCOUNTER')
                      AND name = 'EmployeeCode')
        ALTER TABLE dbo.PATIENTENCOUNTER ADD EmployeeCode nvarchar(20) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('dbo.PATIENTENCOUNTER')
                      AND name = 'EmployeeName')
        ALTER TABLE dbo.PATIENTENCOUNTER ADD EmployeeName nvarchar(100) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('dbo.PATIENTENCOUNTER')
                      AND name = 'EmployeeRelation')
        ALTER TABLE dbo.PATIENTENCOUNTER ADD EmployeeRelation nvarchar(30) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('dbo.PATIENTENCOUNTER')
                      AND name = 'CardNo')
        ALTER TABLE dbo.PATIENTENCOUNTER ADD CardNo nvarchar(30) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('dbo.PATIENTENCOUNTER')
                      AND name = 'RefValid')
        ALTER TABLE dbo.PATIENTENCOUNTER ADD RefValid date NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('dbo.PATIENTENCOUNTER')
                      AND name = 'BedEntitlement')
        ALTER TABLE dbo.PATIENTENCOUNTER ADD BedEntitlement varchar(10) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('dbo.PATIENTENCOUNTER')
                      AND name = 'DischargeStatus')
        ALTER TABLE dbo.PATIENTENCOUNTER ADD DischargeStatus nvarchar(30) NULL;
END;

IF OBJECT_ID('dbo.PATIENTROOM', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('dbo.PATIENTROOM')
                      AND name = 'WardId')
        ALTER TABLE dbo.PATIENTROOM ADD WardId bigint NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('dbo.PATIENTROOM')
                      AND name = 'BedPositionId')
        ALTER TABLE dbo.PATIENTROOM ADD BedPositionId bigint NULL;
END;
");

            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.BEDTRANSFER', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.BEDTRANSFER
    (
        Id                bigint IDENTITY(1,1) NOT NULL,
        Type             varchar(1)  NOT NULL CONSTRAINT DF_BEDTRANSFER_Type         DEFAULT ('I'),
        RegNo            varchar(30) NULL,
        PatientEncounterId bigint NULL,
        AdmissionId     bigint NULL,
        FromBedPositionId bigint NULL,
        ToBedPositionId bigint NULL,
        FromBedCode     varchar(10) NULL,
        ToBedCode        varchar(10) NULL,
        TransferDate    datetime2 NULL,
        IndentNo         varchar(30) NULL,
        IndentDate       date NULL,
        Retired          bit          NOT NULL CONSTRAINT DF_BEDTRANSFER_Retired   DEFAULT (0),
        RetireComments  varchar(500) NULL,
        CreaterId        bigint NULL,
        CreatedAt        datetime2 NULL,
        EditerId         bigint NULL,
        EditedAt         datetime2 NULL,
        RetirerId        bigint NULL,
        RetiredAt        datetime2 NULL,
        UserId           varchar(15) NULL,
        UDate            date NULL,
        UTime            varchar(5) NULL,
        CONSTRAINT PK_BEDTRANSFER PRIMARY KEY (Id),
        CONSTRAINT FK_BedTransfer_PatientEncounter_PatientEncounter FOREIGN KEY (PatientEncounterId) REFERENCES dbo.PATIENTENCOUNTER (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_BedTransfer_Admission_ADMISSION FOREIGN KEY (AdmissionId) REFERENCES dbo.ADMISSION (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_BedTransfer_FromBedPosition_BED_POS FOREIGN KEY (FromBedPositionId) REFERENCES dbo.BED_POS (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_BedTransfer_ToBedPosition_BED_POS FOREIGN KEY (ToBedPositionId) REFERENCES dbo.BED_POS (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_BedTransfer_Creator_WebUser FOREIGN KEY (CreaterId) REFERENCES dbo.WEBUSER (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_BedTransfer_Editor_WebUser FOREIGN KEY (EditerId) REFERENCES dbo.WEBUSER (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_BedTransfer_Retirer_WebUser FOREIGN KEY (RetirerId) REFERENCES dbo.WEBUSER (Id) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.BEDTRANSFER') AND name = 'IX_BEDTRANSFER_PatientEncounterId')
    CREATE INDEX IX_BEDTRANSFER_PatientEncounterId ON dbo.BEDTRANSFER (PatientEncounterId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.BEDTRANSFER') AND name = 'IX_BEDTRANSFER_RegNo')
    CREATE INDEX IX_BEDTRANSFER_RegNo ON dbo.BEDTRANSFER (RegNo);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.BEDTRANSFER') AND name = 'IX_BEDTRANSFER_TransferDate')
    CREATE INDEX IX_BEDTRANSFER_TransferDate ON dbo.BEDTRANSFER (TransferDate);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.BEDTRANSFER') AND name = 'IX_BEDTRANSFER_ToBedPositionId')
    CREATE INDEX IX_BEDTRANSFER_ToBedPositionId ON dbo.BEDTRANSFER (ToBedPositionId);
");

            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.COMPANYTRANSFER', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.COMPANYTRANSFER
    (
        Id               bigint IDENTITY(1,1) NOT NULL,
        Type            varchar(1)  NOT NULL CONSTRAINT DF_COMPANYTRANSFER_Type      DEFAULT ('I'),
        RegNo           varchar(30) NULL,
        PatientEncounterId bigint NULL,
        FromCompanyId   bigint NULL,
        ToCompanyId     bigint NULL,
        FromCompanyCode varchar(4)  NULL,
        ToCompanyCode   varchar(4)  NULL,
        EmployeeCode    varchar(8)  NULL,
        EmployeeName    varchar(100) NULL,
        EmployeeDesignation varchar(50) NULL,
        EmployeeRelation varchar(30) NULL,
        ReferenceNo     varchar(30) NULL,
        ReferenceValid  date NULL,
        CardNo          varchar(30) NULL,
        BedEntitlement  varchar(10) NULL,
        UpdateRates     bit          NOT NULL CONSTRAINT DF_COMPANYTRANSFER_UpdateRates DEFAULT (0),
        TransferDate    datetime2 NULL,
        Retired         bit          NOT NULL CONSTRAINT DF_COMPANYTRANSFER_Retired   DEFAULT (0),
        RetireComments varchar(500) NULL,
        CreaterId       bigint NULL,
        CreatedAt       datetime2 NULL,
        EditerId        bigint NULL,
        EditedAt        datetime2 NULL,
        RetirerId       bigint NULL,
        RetiredAt       datetime2 NULL,
        UserId          varchar(15) NULL,
        UDate           date NULL,
        UTime           varchar(5) NULL,
        CONSTRAINT PK_COMPANYTRANSFER PRIMARY KEY (Id),
        CONSTRAINT FK_CompanyTransfer_PatientEncounter_PatientEncounter FOREIGN KEY (PatientEncounterId) REFERENCES dbo.PATIENTENCOUNTER (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_CompanyTransfer_FromCompany_INSTITUTION FOREIGN KEY (FromCompanyId) REFERENCES dbo.INSTITUTION (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_CompanyTransfer_ToCompany_INSTITUTION FOREIGN KEY (ToCompanyId) REFERENCES dbo.INSTITUTION (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_CompanyTransfer_Creator_WebUser FOREIGN KEY (CreaterId) REFERENCES dbo.WEBUSER (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_CompanyTransfer_Editor_WebUser FOREIGN KEY (EditerId) REFERENCES dbo.WEBUSER (Id) ON DELETE NO ACTION,
        CONSTRAINT FK_CompanyTransfer_Retirer_WebUser FOREIGN KEY (RetirerId) REFERENCES dbo.WEBUSER (Id) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.COMPANYTRANSFER') AND name = 'IX_COMPANYTRANSFER_PatientEncounterId')
    CREATE INDEX IX_COMPANYTRANSFER_PatientEncounterId ON dbo.COMPANYTRANSFER (PatientEncounterId);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.COMPANYTRANSFER') AND name = 'IX_COMPANYTRANSFER_RegNo')
    CREATE INDEX IX_COMPANYTRANSFER_RegNo ON dbo.COMPANYTRANSFER (RegNo);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.COMPANYTRANSFER') AND name = 'IX_COMPANYTRANSFER_TransferDate')
    CREATE INDEX IX_COMPANYTRANSFER_TransferDate ON dbo.COMPANYTRANSFER (TransferDate);
");

            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.PATIENTROOM', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('dbo.PATIENTROOM')
                      AND name = 'WardId')
        ALTER TABLE dbo.PATIENTROOM ADD WardId bigint NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('dbo.PATIENTROOM')
                      AND name = 'BedPositionId')
        ALTER TABLE dbo.PATIENTROOM ADD BedPositionId bigint NULL;
END;

IF OBJECT_ID('dbo.PATIENTENCOUNTER', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('dbo.PATIENTENCOUNTER')
                      AND name = 'EmployeeCode')
        ALTER TABLE dbo.PATIENTENCOUNTER ADD EmployeeCode nvarchar(20) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('dbo.PATIENTENCOUNTER')
                      AND name = 'EmployeeName')
        ALTER TABLE dbo.PATIENTENCOUNTER ADD EmployeeName nvarchar(100) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('dbo.PATIENTENCOUNTER')
                      AND name = 'EmployeeRelation')
        ALTER TABLE dbo.PATIENTENCOUNTER ADD EmployeeRelation nvarchar(30) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('dbo.PATIENTENCOUNTER')
                      AND name = 'CardNo')
        ALTER TABLE dbo.PATIENTENCOUNTER ADD CardNo nvarchar(30) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('dbo.PATIENTENCOUNTER')
                      AND name = 'RefValid')
        ALTER TABLE dbo.PATIENTENCOUNTER ADD RefValid date NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('dbo.PATIENTENCOUNTER')
                      AND name = 'BedEntitlement')
        ALTER TABLE dbo.PATIENTENCOUNTER ADD BedEntitlement varchar(10) NULL;
    IF NOT EXISTS (SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('dbo.PATIENTENCOUNTER')
                      AND name = 'DischargeStatus')
        ALTER TABLE dbo.PATIENTENCOUNTER ADD DischargeStatus nvarchar(30) NULL;
END;
");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
IF OBJECT_ID('dbo.COMPANYTRANSFER', 'U') IS NOT NULL
    DROP TABLE dbo.COMPANYTRANSFER;

IF OBJECT_ID('dbo.BEDTRANSFER', 'U') IS NOT NULL
    DROP TABLE dbo.BEDTRANSFER;

IF OBJECT_ID('dbo.PATIENTROOM', 'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID('dbo.PATIENTROOM')
                  AND name = 'WardId')
        ALTER TABLE dbo.PATIENTROOM DROP COLUMN WardId;
    IF EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID('dbo.PATIENTROOM')
                  AND name = 'BedPositionId')
        ALTER TABLE dbo.PATIENTROOM DROP COLUMN BedPositionId;
END;

IF OBJECT_ID('dbo.PATIENTENCOUNTER', 'U') IS NOT NULL
BEGIN
    IF EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID('dbo.PATIENTENCOUNTER')
                  AND name = 'EmployeeCode')
        ALTER TABLE dbo.PATIENTENCOUNTER DROP COLUMN EmployeeCode;
    IF EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID('dbo.PATIENTENCOUNTER')
                  AND name = 'EmployeeName')
        ALTER TABLE dbo.PATIENTENCOUNTER DROP COLUMN EmployeeName;
    IF EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID('dbo.PATIENTENCOUNTER')
                  AND name = 'EmployeeRelation')
        ALTER TABLE dbo.PATIENTENCOUNTER DROP COLUMN EmployeeRelation;
    IF EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID('dbo.PATIENTENCOUNTER')
                  AND name = 'CardNo')
        ALTER TABLE dbo.PATIENTENCOUNTER DROP COLUMN CardNo;
    IF EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID('dbo.PATIENTENCOUNTER')
                  AND name = 'RefValid')
        ALTER TABLE dbo.PATIENTENCOUNTER DROP COLUMN RefValid;
    IF EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID('dbo.PATIENTENCOUNTER')
                  AND name = 'BedEntitlement')
        ALTER TABLE dbo.PATIENTENCOUNTER DROP COLUMN BedEntitlement;
    IF EXISTS (SELECT 1 FROM sys.columns
                WHERE object_id = OBJECT_ID('dbo.PATIENTENCOUNTER')
                  AND name = 'DischargeStatus')
        ALTER TABLE dbo.PATIENTENCOUNTER DROP COLUMN DischargeStatus;
END;
");
        }
    }
}
