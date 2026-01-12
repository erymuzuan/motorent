-- Comment Table
-- Comments associated with any entity type
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
)
GO

CREATE CLUSTERED INDEX [IX_Comment_EntityId] ON [Core].[Comment]([EntityId] DESC)
GO

CREATE INDEX [IX_Comment_Type_AccountNo] ON [Core].[Comment]([Type], [AccountNo], [Root])
GO

CREATE INDEX [IX_Comment_ObjectWebId] ON [Core].[Comment]([ObjectWebId])
GO
