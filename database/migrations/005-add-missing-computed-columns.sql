-- Migration 005: Add missing computed columns to existing tenant schemas
-- This script is idempotent - safe to run multiple times
-- Replace <schema> with actual tenant AccountNo when executing
--
-- Usage:
--   1. Replace all occurrences of <schema> with tenant AccountNo (e.g., AdamMotoGolok)
--   2. Execute against the MotoRent database
--
-- Fixes:
--   - Invalid column name 'VehicleId' in Rental table
--   - Invalid column name 'ReportedOn' in DamageReport table

SET NOCOUNT ON;
GO

-- ============================================
-- 1. Add VehicleId to Rental table
-- ============================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[<schema>].[Rental]') AND name = 'VehicleId'
)
BEGIN
    ALTER TABLE [<schema>].[Rental]
    ADD [VehicleId] AS CAST(COALESCE(JSON_VALUE([Json], '$.VehicleId'), JSON_VALUE([Json], '$.MotorbikeId')) AS INT);
    PRINT 'Added VehicleId column to [<schema>].[Rental]';
END
ELSE
BEGIN
    PRINT 'VehicleId column already exists in [<schema>].[Rental]';
END
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('[<schema>].[Rental]') AND name = 'IX_Rental_VehicleId'
)
BEGIN
    CREATE INDEX IX_Rental_VehicleId ON [<schema>].[Rental]([VehicleId]);
    PRINT 'Created index IX_Rental_VehicleId';
END
ELSE
BEGIN
    PRINT 'Index IX_Rental_VehicleId already exists';
END
GO

-- ============================================
-- 2. Add ReportedOn to DamageReport table
-- ============================================
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[<schema>].[DamageReport]') AND name = 'ReportedOn'
)
BEGIN
    ALTER TABLE [<schema>].[DamageReport]
    ADD [ReportedOn] DATETIMEOFFSET NULL;
    PRINT 'Added ReportedOn column to [<schema>].[DamageReport]';
END
ELSE
BEGIN
    PRINT 'ReportedOn column already exists in [<schema>].[DamageReport]';
END
GO

PRINT 'Migration 005 completed successfully for schema [<schema>]';
GO
