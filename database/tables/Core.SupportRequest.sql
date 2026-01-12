-- SupportRequest Table
-- Support tickets created from comments
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
)
GO

CREATE CLUSTERED INDEX [IX_SupportRequest_EntityId] ON [Core].[SupportRequest]([EntityId] DESC)
GO

CREATE INDEX [IX_SupportRequest_Type_AccountNo] ON [Core].[SupportRequest]([Type], [AccountNo], [CommentId])
GO

CREATE INDEX [IX_SupportRequest_No] ON [Core].[SupportRequest]([No], [AccountNo])
GO

CREATE INDEX [IX_SupportRequest_Status] ON [Core].[SupportRequest]([Status], [Priority])
GO
