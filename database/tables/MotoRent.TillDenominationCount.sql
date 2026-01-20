-- TillDenominationCount table - Denomination-level cash counts for till sessions
CREATE TABLE [<schema>].[TillDenominationCount]
(
    [TillDenominationCountId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for indexing
    [TillSessionId] AS CAST(JSON_VALUE([Json], '$.TillSessionId') AS INT),
    [CountType] AS CAST(JSON_VALUE([Json], '$.CountType') AS NVARCHAR(20)),
    [CountedAt] AS CONVERT(DATETIMEOFFSET, JSON_VALUE([Json], '$.CountedAt'), 127) PERSISTED,
    [CountedByUserName] AS CAST(JSON_VALUE([Json], '$.CountedByUserName') AS NVARCHAR(100)),
    [TotalInThb] AS CAST(JSON_VALUE([Json], '$.TotalInThb') AS DECIMAL(18,2)),
    [IsFinal] AS CAST(JSON_VALUE([Json], '$.IsFinal') AS BIT),
    -- JSON storage (contains CurrencyBreakdowns list)
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

-- Index for looking up counts by session and type (e.g., get opening count for session X)
CREATE INDEX IX_TillDenominationCount_Session_Type ON [<schema>].[TillDenominationCount]([TillSessionId], [CountType])

-- Index for looking up counts by user
CREATE INDEX IX_TillDenominationCount_CountedBy ON [<schema>].[TillDenominationCount]([CountedByUserName])

-- Index for looking up counts by date
CREATE INDEX IX_TillDenominationCount_CountedAt ON [<schema>].[TillDenominationCount]([CountedAt])
