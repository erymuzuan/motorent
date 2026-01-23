-- RateDelta table - Persisted delta adjustments for rate refresh reapplication
-- ShopId null = org default, ShopId set = shop-specific override
-- DenominationGroupId null = applies to all groups for currency
CREATE TABLE [<schema>].[RateDelta]
(
    [RateDeltaId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for indexing
    [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
    [Currency] AS CAST(JSON_VALUE([Json], '$.Currency') AS CHAR(3)),
    [DenominationGroupId] AS CAST(JSON_VALUE([Json], '$.DenominationGroupId') AS INT),
    [IsActive] AS CAST(JSON_VALUE([Json], '$.IsActive') AS BIT),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE INDEX IX_RateDelta_Shop ON [<schema>].[RateDelta]([ShopId], [Currency], [IsActive])
CREATE INDEX IX_RateDelta_Group ON [<schema>].[RateDelta]([DenominationGroupId], [IsActive])
