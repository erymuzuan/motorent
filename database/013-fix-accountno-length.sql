-- =============================================
-- Migration: Fix AccountNo computed column length
-- Issue: VARCHAR(20) is too short for some AccountNo values
-- Solution: Increase to VARCHAR(50)
-- =============================================

-- Drop the index first
IF EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_Organization_AccountNo' AND object_id = OBJECT_ID('[Core].[Organization]'))
BEGIN
    DROP INDEX [IX_Organization_AccountNo] ON [Core].[Organization];
END
GO

-- Drop the computed column
ALTER TABLE [Core].[Organization] DROP COLUMN [AccountNo];
GO

-- Re-create with larger size
ALTER TABLE [Core].[Organization] ADD [AccountNo] AS CAST(JSON_VALUE([Json], '$.AccountNo') AS VARCHAR(50)) PERSISTED;
GO

-- Re-create the index
CREATE UNIQUE INDEX [IX_Organization_AccountNo] ON [Core].[Organization]([AccountNo]);
GO
