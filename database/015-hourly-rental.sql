-- =============================================
-- 015: Add Hourly Rental Support
-- Adds HourlyRate computed column to Vehicle table
-- =============================================

-- Add HourlyRate computed column to Vehicle table for each tenant schema
-- Run this for each tenant schema (replace <schema> with actual AccountNo)

-- Example for a specific tenant:
-- ALTER TABLE [AdamMotoGolok].[Vehicle]
-- ADD [HourlyRate] AS CAST(JSON_VALUE([Json], '$.HourlyRate') AS DECIMAL(10,2))

-- Generic template (run per schema):
DECLARE @sql NVARCHAR(MAX) = '';
DECLARE @schema NVARCHAR(128);

DECLARE schema_cursor CURSOR FOR
SELECT s.name
FROM sys.schemas s
INNER JOIN [Core].[Organization] o ON s.name = JSON_VALUE(o.[Json], '$.AccountNo')
WHERE s.name != 'Core';

OPEN schema_cursor;
FETCH NEXT FROM schema_cursor INTO @schema;

WHILE @@FETCH_STATUS = 0
BEGIN
    -- Check if column already exists
    IF NOT EXISTS (
        SELECT 1 FROM sys.columns c
        INNER JOIN sys.tables t ON c.object_id = t.object_id
        INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
        WHERE s.name = @schema AND t.name = 'Vehicle' AND c.name = 'HourlyRate'
    )
    BEGIN
        SET @sql = 'ALTER TABLE [' + @schema + '].[Vehicle] ADD [HourlyRate] AS CAST(JSON_VALUE([Json], ''$.HourlyRate'') AS DECIMAL(10,2))';
        EXEC sp_executesql @sql;
        PRINT 'Added HourlyRate to [' + @schema + '].[Vehicle]';
    END

    FETCH NEXT FROM schema_cursor INTO @schema;
END

CLOSE schema_cursor;
DEALLOCATE schema_cursor;
