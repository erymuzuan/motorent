-- PricingRule table - Dynamic pricing rules for seasonal and event-based pricing
CREATE TABLE [<schema>].[PricingRule]
(
    [PricingRuleId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for querying
    [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
    [Name] AS CAST(JSON_VALUE([Json], '$.Name') AS NVARCHAR(100)),
    [RuleType] AS CAST(JSON_VALUE([Json], '$.RuleType') AS NVARCHAR(20)),
    [StartDate] AS CAST(JSON_VALUE([Json], '$.StartDate') AS DATE),
    [EndDate] AS CAST(JSON_VALUE([Json], '$.EndDate') AS DATE),
    [IsRecurring] AS CAST(JSON_VALUE([Json], '$.IsRecurring') AS BIT),
    [Multiplier] AS CAST(JSON_VALUE([Json], '$.Multiplier') AS DECIMAL(5,2)),
    [Priority] AS CAST(JSON_VALUE([Json], '$.Priority') AS INT),
    [IsActive] AS CAST(JSON_VALUE([Json], '$.IsActive') AS BIT),
    [VehicleType] AS CAST(JSON_VALUE([Json], '$.VehicleType') AS NVARCHAR(20)),
    [VehicleId] AS CAST(JSON_VALUE([Json], '$.VehicleId') AS INT),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

-- Index for querying active rules by shop and date range
CREATE INDEX IX_PricingRule_ShopDate ON [<schema>].[PricingRule]([ShopId], [StartDate], [EndDate], [IsActive])
