-- =============================================
-- Error Logging Schema Updates
-- Adds computed columns and indexes for efficient error log querying
-- =============================================

SET QUOTED_IDENTIFIER ON
GO

-- Drop existing Severity column (reads from wrong JSON path)
-- and recreate with correct path
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[Core].[LogEntry]') AND name = 'Severity')
BEGIN
    -- Drop dependent index first
    IF EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('[Core].[LogEntry]') AND name = 'IX_LogEntry_Severity')
    BEGIN
        DROP INDEX [IX_LogEntry_Severity] ON [Core].[LogEntry];
    END

    ALTER TABLE [Core].[LogEntry] DROP COLUMN [Severity];
END
GO

-- Add LogSeverity computed column (maps to entity property name)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[Core].[LogEntry]') AND name = 'LogSeverity')
BEGIN
    ALTER TABLE [Core].[LogEntry]
    ADD [LogSeverity] AS CAST(JSON_VALUE([Json], '$.LogSeverity') AS VARCHAR(20));
END
GO

-- Add Status computed column for filtering by resolution status
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[Core].[LogEntry]') AND name = 'Status')
BEGIN
    ALTER TABLE [Core].[LogEntry]
    ADD [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS VARCHAR(20));
END
GO

-- Add Log (EventLog) computed column for filtering by log type (Web, Api, Background, Security)
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[Core].[LogEntry]') AND name = 'Log')
BEGIN
    ALTER TABLE [Core].[LogEntry]
    ADD [Log] AS CAST(JSON_VALUE([Json], '$.Log') AS VARCHAR(20));
END
GO

-- Add IncidentHash computed column for grouping similar errors
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[Core].[LogEntry]') AND name = 'IncidentHash')
BEGIN
    ALTER TABLE [Core].[LogEntry]
    ADD [IncidentHash] AS CAST(JSON_VALUE([Json], '$.IncidentHash') AS VARCHAR(50));
END
GO

-- Add Type computed column for exception type filtering
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('[Core].[LogEntry]') AND name = 'Type')
BEGIN
    ALTER TABLE [Core].[LogEntry]
    ADD [Type] AS CAST(JSON_VALUE([Json], '$.Type') AS VARCHAR(200));
END
GO

-- Create index for severity-based queries
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('[Core].[LogEntry]') AND name = 'IX_LogEntry_LogSeverity')
BEGIN
    CREATE INDEX [IX_LogEntry_LogSeverity] ON [Core].[LogEntry]([LogSeverity]);
END
GO

-- Create index for status-based queries
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('[Core].[LogEntry]') AND name = 'IX_LogEntry_Status')
BEGIN
    CREATE INDEX [IX_LogEntry_Status] ON [Core].[LogEntry]([Status]);
END
GO

-- Create composite index for common query patterns (status + datetime for dashboard)
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('[Core].[LogEntry]') AND name = 'IX_LogEntry_Status_DateTime')
BEGIN
    CREATE INDEX [IX_LogEntry_Status_DateTime] ON [Core].[LogEntry]([Status], [DateTime]);
END
GO

-- Create index for incident grouping
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('[Core].[LogEntry]') AND name = 'IX_LogEntry_IncidentHash')
BEGIN
    CREATE INDEX [IX_LogEntry_IncidentHash] ON [Core].[LogEntry]([IncidentHash]);
END
GO

-- Create index for log type filtering
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('[Core].[LogEntry]') AND name = 'IX_LogEntry_Log')
BEGIN
    CREATE INDEX [IX_LogEntry_Log] ON [Core].[LogEntry]([Log]);
END
GO

PRINT 'Error logging schema updates completed successfully.'
GO
