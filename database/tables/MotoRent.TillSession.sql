-- TillSession table - Cashier till sessions for staff
CREATE TABLE [<schema>].[TillSession]
(
    [TillSessionId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for indexing
    [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
    [StaffUserName] AS CAST(JSON_VALUE([Json], '$.StaffUserName') AS NVARCHAR(100)),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(30)),
    [OpenedAt] AS CONVERT(DATETIMEOFFSET, JSON_VALUE([Json], '$.OpenedAt'), 127) PERSISTED,
    [ClosedAt] AS CONVERT(DATETIMEOFFSET, JSON_VALUE([Json], '$.ClosedAt'), 127) PERSISTED,
    [VerifiedByUserName] AS CAST(JSON_VALUE([Json], '$.VerifiedByUserName') AS NVARCHAR(100)),
    [VerifiedAt] AS CONVERT(DATETIMEOFFSET, JSON_VALUE([Json], '$.VerifiedAt'), 127) PERSISTED,
    [ClosedByUserName] AS CAST(JSON_VALUE([Json], '$.ClosedByUserName') AS NVARCHAR(100)),
    [IsForceClose] AS CAST(JSON_VALUE([Json], '$.IsForceClose') AS BIT),
    [IsLateClose] AS CAST(JSON_VALUE([Json], '$.IsLateClose') AS BIT),
    [ExpectedCloseDate] AS CONVERT(DATE, JSON_VALUE([Json], '$.ExpectedCloseDate'), 23) PERSISTED,
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE INDEX IX_TillSession_ShopId_Status ON [<schema>].[TillSession]([ShopId], [Status])
CREATE INDEX IX_TillSession_StaffUserName ON [<schema>].[TillSession]([StaffUserName])
CREATE INDEX IX_TillSession_OpenedAt ON [<schema>].[TillSession]([OpenedAt])
CREATE INDEX IX_TillSession_ClosedAt ON [<schema>].[TillSession]([ClosedAt])
