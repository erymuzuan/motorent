-- Migration 006 for AdamMotoGolok
-- Fix missing Hidden column in Comment table

SET NOCOUNT ON;
GO

-- ============================================
-- Add Hidden to Comment table
-- ============================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[AdamMotoGolok].[Comment]') AND name = 'Hidden'
)
BEGIN
    ALTER TABLE [AdamMotoGolok].[Comment]
    ADD [Hidden] AS CAST(JSON_VALUE([Json], '$.Hidden') AS BIT);
    PRINT 'Added Hidden column to [AdamMotoGolok].[Comment]';
END
ELSE
BEGIN
    PRINT 'Hidden column already exists in [AdamMotoGolok].[Comment]';
END
GO

PRINT 'Migration 006 completed successfully for AdamMotoGolok';
GO
