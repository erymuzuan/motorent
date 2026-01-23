-- ShortageLog table - Variance accountability records
CREATE TABLE [<schema>].[ShortageLog]
(
    [ShortageLogId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for indexing
    [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
    [TillSessionId] AS CAST(JSON_VALUE([Json], '$.TillSessionId') AS INT),
    [DailyCloseId] AS CAST(JSON_VALUE([Json], '$.DailyCloseId') AS INT),
    [StaffUserName] AS CAST(JSON_VALUE([Json], '$.StaffUserName') AS NVARCHAR(100)),
    [Currency] AS CAST(JSON_VALUE([Json], '$.Currency') AS CHAR(3)),
    [Amount] AS CAST(JSON_VALUE([Json], '$.Amount') AS DECIMAL(18,2)),
    [AmountInThb] AS CAST(JSON_VALUE([Json], '$.AmountInThb') AS DECIMAL(18,2)),
    [LoggedAt] AS CONVERT(DATETIMEOFFSET, JSON_VALUE([Json], '$.LoggedAt'), 127) PERSISTED,
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE INDEX IX_ShortageLog_ShopId_TillSessionId ON [<schema>].[ShortageLog]([ShopId], [TillSessionId])
CREATE INDEX IX_ShortageLog_StaffUserName ON [<schema>].[ShortageLog]([StaffUserName])
CREATE INDEX IX_ShortageLog_LoggedAt ON [<schema>].[ShortageLog]([LoggedAt])
