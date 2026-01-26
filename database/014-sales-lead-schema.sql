-- =============================================
-- MotoRent Sales Lead Schema
-- Tracks leads from contact forms and other sources
-- through the sales funnel: Lead -> Trial -> Customer
-- =============================================

SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- SalesLead Table
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables t
    JOIN sys.schemas s ON t.schema_id = s.schema_id
    WHERE s.name = 'Core' AND t.name = 'SalesLead')
BEGIN
    CREATE TABLE [Core].[SalesLead]
    (
        [SalesLeadId]       INT            NOT NULL PRIMARY KEY IDENTITY(1,1),
        -- Computed columns from JSON
        [No]                AS CAST(JSON_VALUE([Json], '$.No') AS VARCHAR(20)) PERSISTED,
        [Name]              AS CAST(JSON_VALUE([Json], '$.Name') AS NVARCHAR(200)),
        [Email]             AS CAST(JSON_VALUE([Json], '$.Email') AS VARCHAR(200)),
        [Phone]             AS CAST(JSON_VALUE([Json], '$.Phone') AS VARCHAR(50)),
        [Company]           AS CAST(JSON_VALUE([Json], '$.Company') AS NVARCHAR(200)),
        [Status]            AS CAST(JSON_VALUE([Json], '$.Status') AS VARCHAR(20)),
        [Source]            AS CAST(JSON_VALUE([Json], '$.Source') AS VARCHAR(20)),
        [PlanInterested]    AS CAST(JSON_VALUE([Json], '$.PlanInterested') AS VARCHAR(20)),
        [FleetSize]         AS CAST(JSON_VALUE([Json], '$.FleetSize') AS INT),
        [OrganizationId]    AS CAST(JSON_VALUE([Json], '$.OrganizationId') AS INT),
        [AccountNo]         AS CAST(JSON_VALUE([Json], '$.AccountNo') AS VARCHAR(20)),
        -- JSON storage
        [Json]              NVARCHAR(MAX)  NOT NULL,
        -- Audit columns
        [CreatedBy]         VARCHAR(50)    NOT NULL DEFAULT 'system',
        [ChangedBy]         VARCHAR(50)    NOT NULL DEFAULT 'system',
        [CreatedTimestamp]  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [ChangedTimestamp]  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );
END
GO

-- Create indexes
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SalesLead_No')
BEGIN
    -- Note: Unique index without filter since No is a computed column
    CREATE UNIQUE INDEX [IX_SalesLead_No] ON [Core].[SalesLead]([No]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SalesLead_Email')
BEGIN
    CREATE INDEX [IX_SalesLead_Email] ON [Core].[SalesLead]([Email]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_SalesLead_Status')
BEGIN
    CREATE INDEX [IX_SalesLead_Status] ON [Core].[SalesLead]([Status], [Source], [CreatedTimestamp]);
END
GO

PRINT 'SalesLead table and indexes created successfully';
GO
