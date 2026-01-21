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
    -- Computed columns for DATE/DATETIMEOFFSET (non-persisted, cannot be indexed)
    [OpenedAt] AS TRY_CONVERT(DATETIMEOFFSET, JSON_VALUE([Json], '$.OpenedAt'), 127),
    [ClosedAt] AS TRY_CONVERT(DATETIMEOFFSET, JSON_VALUE([Json], '$.ClosedAt'), 127),
    [VerifiedAt] AS TRY_CONVERT(DATETIMEOFFSET, JSON_VALUE([Json], '$.VerifiedAt'), 127),
    [ExpectedCloseDate] AS CAST(JSON_VALUE([Json], '$.ExpectedCloseDate') AS DATE),
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
-- Note: OpenedAt/ClosedAt are non-persisted computed columns and cannot be indexed
