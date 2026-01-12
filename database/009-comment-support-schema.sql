-- =============================================
-- MotoRent Comment and Support Request Schema
-- Cross-tenant entities for comments, follows, and support requests
-- =============================================

SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Comment Table
-- Comments associated with any entity type
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Comment' AND schema_id = SCHEMA_ID('Core'))
BEGIN
    CREATE TABLE [Core].[Comment]
    (
        [CommentId]         INT            NOT NULL PRIMARY KEY IDENTITY(1,1),
        -- Computed columns from JSON
        [Title]             AS CAST(JSON_VALUE([Json], '$.Title') AS NVARCHAR(255)),
        [Type]              AS CAST(JSON_VALUE([Json], '$.Type') AS VARCHAR(50)),
        [ObjectWebId]       AS CAST(JSON_VALUE([Json], '$.ObjectWebId') AS VARCHAR(50)),
        [AccountNo]         AS CAST(JSON_VALUE([Json], '$.AccountNo') AS VARCHAR(50)),
        [User]              AS CAST(JSON_VALUE([Json], '$.User') AS VARCHAR(100)),
        [EntityId]          AS CAST(JSON_VALUE([Json], '$.EntityId') AS INT),
        [Root]              AS CAST(JSON_VALUE([Json], '$.Root') AS INT),
        [ReplyTo]           AS CAST(JSON_VALUE([Json], '$.ReplyTo') AS INT),
        [SupportComment]    AS CAST(JSON_VALUE([Json], '$.SupportComment') AS BIT),
        [SupportStatus]     AS CAST(JSON_VALUE([Json], '$.SupportStatus') AS VARCHAR(25)),
        [Timestamp]         DATETIMEOFFSET NULL,
        -- JSON storage
        [Json]              NVARCHAR(MAX)  NOT NULL,
        -- Audit columns
        [CreatedBy]         VARCHAR(50)    NOT NULL DEFAULT 'system',
        [ChangedBy]         VARCHAR(50)    NOT NULL DEFAULT 'system',
        [CreatedTimestamp]  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [ChangedTimestamp]  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );

    CREATE CLUSTERED INDEX [IX_Comment_EntityId] ON [Core].[Comment]([EntityId] DESC);
    CREATE INDEX [IX_Comment_Type_AccountNo] ON [Core].[Comment]([Type], [AccountNo], [Root]);
    CREATE INDEX [IX_Comment_ObjectWebId] ON [Core].[Comment]([ObjectWebId]);
END
GO

-- =============================================
-- Follow Table
-- Track users following entities for notifications
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Follow' AND schema_id = SCHEMA_ID('Core'))
BEGIN
    CREATE TABLE [Core].[Follow]
    (
        [FollowId]          INT            NOT NULL PRIMARY KEY IDENTITY(1,1),
        -- Computed columns from JSON
        [Type]              AS CAST(JSON_VALUE([Json], '$.Type') AS VARCHAR(50)),
        [User]              AS CAST(JSON_VALUE([Json], '$.User') AS VARCHAR(100)),
        [IsActive]          AS CAST(JSON_VALUE([Json], '$.IsActive') AS BIT),
        [EntityId]          AS CAST(JSON_VALUE([Json], '$.EntityId') AS INT),
        -- JSON storage
        [Json]              NVARCHAR(MAX)  NOT NULL,
        -- Audit columns
        [CreatedBy]         VARCHAR(50)    NOT NULL DEFAULT 'system',
        [ChangedBy]         VARCHAR(50)    NOT NULL DEFAULT 'system',
        [CreatedTimestamp]  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [ChangedTimestamp]  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );

    CREATE INDEX [IX_Follow_EntityId_User_Type] ON [Core].[Follow]([EntityId], [User], [Type]);
END
GO

-- =============================================
-- SupportRequest Table
-- Support tickets created from comments
-- =============================================
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SupportRequest' AND schema_id = SCHEMA_ID('Core'))
BEGIN
    CREATE TABLE [Core].[SupportRequest]
    (
        [SupportRequestId]  INT            NOT NULL PRIMARY KEY IDENTITY(1,1),
        -- Computed columns from JSON
        [No]                AS CAST(JSON_VALUE([Json], '$.No') AS VARCHAR(15)),
        [Title]             AS CAST(JSON_VALUE([Json], '$.Title') AS NVARCHAR(255)),
        [AccountNo]         AS CAST(JSON_VALUE([Json], '$.AccountNo') AS VARCHAR(50)),
        [Status]            AS CAST(JSON_VALUE([Json], '$.Status') AS VARCHAR(50)),
        [Priority]          AS CAST(JSON_VALUE([Json], '$.Priority') AS VARCHAR(50)),
        [AssignedTo]        AS CAST(JSON_VALUE([Json], '$.AssignedTo') AS VARCHAR(100)),
        [CommentId]         AS CAST(JSON_VALUE([Json], '$.CommentId') AS INT),
        [Type]              AS CAST(JSON_VALUE([Json], '$.Type') AS VARCHAR(50)),
        [ObjectWebId]       AS CAST(JSON_VALUE([Json], '$.ObjectWebId') AS VARCHAR(50)),
        [RequestedBy]       AS CAST(JSON_VALUE([Json], '$.RequestedBy') AS VARCHAR(100)),
        [EntityId]          AS CAST(JSON_VALUE([Json], '$.EntityId') AS INT),
        [TotalMinutesResponded] AS CAST(JSON_VALUE([Json], '$.TotalMinutesResponded') AS FLOAT),
        [TotalMinutesResolved]  AS CAST(JSON_VALUE([Json], '$.TotalMinutesResolved') AS FLOAT),
        [Timestamp]         DATETIMEOFFSET NULL,
        [ResolvedTimestamp] DATETIMEOFFSET NULL,
        [ClosedTimestamp]   DATETIMEOFFSET NULL,
        -- JSON storage
        [Json]              NVARCHAR(MAX)  NOT NULL,
        -- Audit columns
        [CreatedBy]         VARCHAR(50)    NOT NULL DEFAULT 'system',
        [ChangedBy]         VARCHAR(50)    NOT NULL DEFAULT 'system',
        [CreatedTimestamp]  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [ChangedTimestamp]  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );

    CREATE CLUSTERED INDEX [IX_SupportRequest_EntityId] ON [Core].[SupportRequest]([EntityId] DESC);
    CREATE INDEX [IX_SupportRequest_Type_AccountNo] ON [Core].[SupportRequest]([Type], [AccountNo], [CommentId]);
    CREATE INDEX [IX_SupportRequest_No] ON [Core].[SupportRequest]([No], [AccountNo]);
    CREATE INDEX [IX_SupportRequest_Status] ON [Core].[SupportRequest]([Status], [Priority]);
END
GO
