-- DenominationGroup table - Groups of denominations that share the same exchange rate
-- Admin-configurable through settings UI
CREATE TABLE [<schema>].[DenominationGroup]
(
    [DenominationGroupId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for indexing
    [Currency] AS CAST(JSON_VALUE([Json], '$.Currency') AS CHAR(3)),
    [GroupName] AS CAST(JSON_VALUE([Json], '$.GroupName') AS NVARCHAR(50)),
    [SortOrder] AS CAST(JSON_VALUE([Json], '$.SortOrder') AS INT),
    [IsActive] AS CAST(JSON_VALUE([Json], '$.IsActive') AS BIT),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE INDEX IX_DenominationGroup_Currency ON [<schema>].[DenominationGroup]([Currency], [IsActive], [SortOrder])
