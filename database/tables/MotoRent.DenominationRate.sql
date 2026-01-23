-- DenominationRate table - Exchange rates by denomination group
-- ShopId null = org default, ShopId set = shop-specific override
CREATE TABLE [<schema>].[DenominationRate]
(
    [DenominationRateId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for indexing
    [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
    [Currency] AS CAST(JSON_VALUE([Json], '$.Currency') AS CHAR(3)),
    [DenominationGroupId] AS CAST(JSON_VALUE([Json], '$.DenominationGroupId') AS INT),
    [ProviderCode] AS CAST(JSON_VALUE([Json], '$.ProviderCode') AS NVARCHAR(20)),
    [IsActive] AS CAST(JSON_VALUE([Json], '$.IsActive') AS BIT),
    -- Date columns for temporal queries (persisted for indexing)
    [EffectiveDate] AS CONVERT(DATETIMEOFFSET, JSON_VALUE([Json], '$.EffectiveDate'), 127) PERSISTED,
    [ExpiresOn] AS CONVERT(DATETIMEOFFSET, JSON_VALUE([Json], '$.ExpiresOn'), 127) PERSISTED,
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE INDEX IX_DenominationRate_Currency ON [<schema>].[DenominationRate]([Currency], [IsActive])
CREATE INDEX IX_DenominationRate_Shop ON [<schema>].[DenominationRate]([ShopId], [Currency], [IsActive])
CREATE INDEX IX_DenominationRate_Group ON [<schema>].[DenominationRate]([DenominationGroupId], [IsActive])
CREATE INDEX IX_DenominationRate_EffectiveDate ON [<schema>].[DenominationRate]([EffectiveDate])
