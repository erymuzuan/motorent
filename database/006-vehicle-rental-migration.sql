-- Migration: Add new columns to Rental table for multi-vehicle type support
-- Run this script to update existing Rental table schema

-- First, check if the column exists, and if not, add it
-- Note: Replace <schema> with your actual schema name (e.g., KrabiBeachRentals)

-- Add RentedFromShopId column if it doesn't exist (with COALESCE for backward compatibility)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[<schema>].[Rental]') AND name = 'RentedFromShopId')
BEGIN
    ALTER TABLE [<schema>].[Rental] ADD [RentedFromShopId] AS CAST(COALESCE(JSON_VALUE([Json], '$.RentedFromShopId'), JSON_VALUE([Json], '$.ShopId')) AS INT)
END
GO

-- Add ReturnedToShopId column if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[<schema>].[Rental]') AND name = 'ReturnedToShopId')
BEGIN
    ALTER TABLE [<schema>].[Rental] ADD [ReturnedToShopId] AS CAST(JSON_VALUE([Json], '$.ReturnedToShopId') AS INT)
END
GO

-- Add VehiclePoolId column if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[<schema>].[Rental]') AND name = 'VehiclePoolId')
BEGIN
    ALTER TABLE [<schema>].[Rental] ADD [VehiclePoolId] AS CAST(JSON_VALUE([Json], '$.VehiclePoolId') AS INT)
END
GO

-- Add VehicleId column if it doesn't exist (with COALESCE for backward compatibility)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[<schema>].[Rental]') AND name = 'VehicleId')
BEGIN
    ALTER TABLE [<schema>].[Rental] ADD [VehicleId] AS CAST(COALESCE(JSON_VALUE([Json], '$.VehicleId'), JSON_VALUE([Json], '$.MotorbikeId')) AS INT)
END
GO

-- Add DurationType column if it doesn't exist (defaults to Daily for backward compatibility)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[<schema>].[Rental]') AND name = 'DurationType')
BEGIN
    ALTER TABLE [<schema>].[Rental] ADD [DurationType] AS CAST(COALESCE(JSON_VALUE([Json], '$.DurationType'), 'Daily') AS NVARCHAR(20))
END
GO

-- Add IntervalMinutes column if it doesn't exist
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[<schema>].[Rental]') AND name = 'IntervalMinutes')
BEGIN
    ALTER TABLE [<schema>].[Rental] ADD [IntervalMinutes] AS CAST(JSON_VALUE([Json], '$.IntervalMinutes') AS INT)
END
GO

-- Add IncludeDriver column if it doesn't exist (defaults to false for backward compatibility)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[<schema>].[Rental]') AND name = 'IncludeDriver')
BEGIN
    ALTER TABLE [<schema>].[Rental] ADD [IncludeDriver] AS CAST(COALESCE(JSON_VALUE([Json], '$.IncludeDriver'), 'false') AS BIT)
END
GO

-- Add IncludeGuide column if it doesn't exist (defaults to false for backward compatibility)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[<schema>].[Rental]') AND name = 'IncludeGuide')
BEGIN
    ALTER TABLE [<schema>].[Rental] ADD [IncludeGuide] AS CAST(COALESCE(JSON_VALUE([Json], '$.IncludeGuide'), 'false') AS BIT)
END
GO

-- Create indexes if they don't exist
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('[<schema>].[Rental]') AND name = 'IX_Rental_RentedFromShopId_Status')
BEGIN
    CREATE INDEX IX_Rental_RentedFromShopId_Status ON [<schema>].[Rental]([RentedFromShopId], [Status])
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('[<schema>].[Rental]') AND name = 'IX_Rental_VehiclePoolId')
BEGIN
    CREATE INDEX IX_Rental_VehiclePoolId ON [<schema>].[Rental]([VehiclePoolId]) WHERE [VehiclePoolId] IS NOT NULL
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('[<schema>].[Rental]') AND name = 'IX_Rental_VehicleId')
BEGIN
    CREATE INDEX IX_Rental_VehicleId ON [<schema>].[Rental]([VehicleId])
END
GO

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('[<schema>].[Rental]') AND name = 'IX_Rental_DurationType')
BEGIN
    CREATE INDEX IX_Rental_DurationType ON [<schema>].[Rental]([DurationType])
END
GO

PRINT 'Rental table migration completed successfully'
GO
