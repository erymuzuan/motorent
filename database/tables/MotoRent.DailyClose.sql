-- DailyClose table - Daily close records for shops
CREATE TABLE [<schema>].[DailyClose]
(
    [DailyCloseId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for indexing
    [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)),
    [ClosedByUserName] AS CAST(JSON_VALUE([Json], '$.ClosedByUserName') AS NVARCHAR(100)),
    [TotalVariance] AS CAST(JSON_VALUE([Json], '$.TotalVariance') AS DECIMAL(18,2)),
    [WasReopened] AS CAST(JSON_VALUE([Json], '$.WasReopened') AS BIT),
    -- Computed columns for DATE/DATETIMEOFFSET (non-persisted, cannot be indexed)
    [Date] AS CAST(JSON_VALUE([Json], '$.Date') AS DATE),
    [ClosedAt] AS TRY_CONVERT(DATETIMEOFFSET, JSON_VALUE([Json], '$.ClosedAt'), 127),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE UNIQUE INDEX IX_DailyClose_ShopId_Date ON [<schema>].[DailyClose]([ShopId], [Date])
CREATE INDEX IX_DailyClose_Status ON [<schema>].[DailyClose]([Status])
CREATE INDEX IX_DailyClose_ClosedAt ON [<schema>].[DailyClose]([ClosedAt])
