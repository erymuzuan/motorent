-- Accessory table
CREATE TABLE [<schema>].[Accessory]
(
    [AccessoryId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns
    [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
    [Name] AS CAST(JSON_VALUE([Json], '$.Name') AS NVARCHAR(100)),
    [QuantityAvailable] AS CAST(JSON_VALUE([Json], '$.QuantityAvailable') AS INT),
    [DailyRate] AS CAST(JSON_VALUE([Json], '$.DailyRate') AS DECIMAL(10,2)),
    [IsActive] AS CAST(JSON_VALUE([Json], '$.IsActive') AS BIT),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE INDEX IX_Accessory_ShopId ON [<schema>].[Accessory]([ShopId])
