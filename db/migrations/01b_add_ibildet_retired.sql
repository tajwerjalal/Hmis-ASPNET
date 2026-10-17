SET NOCOUNT ON;
IF COL_LENGTH('dbo.IBIL_DET','Retired') IS NULL
BEGIN
    ALTER TABLE dbo.IBIL_DET ADD Retired BIT NULL;
END
GO
SELECT 'IBIL_DET.Retired present: ' + CAST(CASE WHEN COL_LENGTH('dbo.IBIL_DET','Retired') IS NULL THEN 0 ELSE 1 END AS varchar)
     + '  marked rows: ' + CAST(COUNT(*) AS varchar) FROM dbo.IBIL_DET WHERE Retired = 1;
GO
