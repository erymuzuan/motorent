-- DepreciationEntry table - Period depreciation records
CREATE TABLE [<schema>].[DepreciationEntry]
(
    [DepreciationEntryId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Asset reference
    [AssetId] AS CAST(JSON_VALUE([Json], '$.AssetId') AS INT),
    -- Period
    [PeriodStart] DATE NULL,
    [PeriodEnd] DATE NULL,
    -- Amounts
    [Amount] AS CAST(JSON_VALUE([Json], '$.Amount') AS DECIMAL(12,2)),
    [BookValueStart] AS CAST(JSON_VALUE([Json], '$.BookValueStart') AS DECIMAL(12,2)),
    [BookValueEnd] AS CAST(JSON_VALUE([Json], '$.BookValueEnd') AS DECIMAL(12,2)),
    -- Type
    [Method] AS CAST(JSON_VALUE([Json], '$.Method') AS NVARCHAR(30)),
    [EntryType] AS CAST(JSON_VALUE([Json], '$.EntryType') AS NVARCHAR(20)),
    [IsManualOverride] AS CAST(JSON_VALUE([Json], '$.IsManualOverride') AS BIT),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
--

CREATE INDEX IX_DepreciationEntry_AssetId ON [<schema>].[DepreciationEntry]([AssetId])
--
CREATE INDEX IX_DepreciationEntry_Period ON [<schema>].[DepreciationEntry]([AssetId], [PeriodStart], [PeriodEnd])
--
CREATE INDEX IX_DepreciationEntry_Type ON [<schema>].[DepreciationEntry]([EntryType])
--
