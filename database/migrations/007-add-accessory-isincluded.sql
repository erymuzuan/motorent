-- Migration: Add IsIncluded computed column to Accessory table
-- Run this for each tenant schema

DECLARE @schema NVARCHAR(50) = 'KrabiBeachRentals'; -- Change this for each schema

DECLARE @sql NVARCHAR(MAX) = N'
    IF NOT EXISTS (
        SELECT 1 FROM INFORMATION_SCHEMA.COLUMNS 
        WHERE TABLE_SCHEMA = ''' + @schema + ''' 
        AND TABLE_NAME = ''Accessory'' 
        AND COLUMN_NAME = ''IsIncluded''
    )
    BEGIN
        ALTER TABLE [' + @schema + '].[Accessory]
        ADD [IsIncluded] AS CAST(JSON_VALUE([Json], ''$.IsIncluded'') AS BIT);
        PRINT ''Added IsIncluded column to ' + @schema + '.Accessory'';
    END
    ELSE
    BEGIN
        PRINT ''IsIncluded column already exists in ' + @schema + '.Accessory'';
    END
';

EXEC sp_executesql @sql;
GO
