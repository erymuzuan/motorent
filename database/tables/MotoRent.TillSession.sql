-- TillSession table - Cashier till sessions for staff
CREATE TABLE [<schema>].[TillSession]
(
    [TillSessionId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for indexing
    [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
    [StaffUserName] AS CAST(JSON_VALUE([Json], '$.StaffUserName') AS NVARCHAR(100)),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(30)),
    [VerifiedByUserName] AS CAST(JSON_VALUE([Json], '$.VerifiedByUserName') AS NVARCHAR(100)),
    [ClosedByUserName] AS CAST(JSON_VALUE([Json], '$.ClosedByUserName') AS NVARCHAR(100)),
    [IsForceClose] AS CAST(JSON_VALUE([Json], '$.IsForceClose') AS BIT),
    [IsLateClose] AS CAST(JSON_VALUE([Json], '$.IsLateClose') AS BIT),
    -- Persistent columns for DATE/DATETIMEOFFSET (not computed from JSON)
    [OpenedAt] DATETIMEOFFSET NOT NULL,
    [ClosedAt] DATETIMEOFFSET NULL,
    [VerifiedAt] DATETIMEOFFSET NULL,
    [ExpectedCloseDate] DATE NULL,
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
