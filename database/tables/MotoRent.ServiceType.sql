-- ServiceType table - Configurable maintenance service types
CREATE TABLE [<schema>].[ServiceType]
(
    [ServiceTypeId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for querying
    [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
    [Name] AS CAST(JSON_VALUE([Json], '$.Name') AS NVARCHAR(100)),
    [DaysInterval] AS CAST(JSON_VALUE([Json], '$.DaysInterval') AS INT),
    [KmInterval] AS CAST(JSON_VALUE([Json], '$.KmInterval') AS INT),
    [IsActive] AS CAST(JSON_VALUE([Json], '$.IsActive') AS BIT),
    [SortOrder] AS CAST(JSON_VALUE([Json], '$.SortOrder') AS INT),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE INDEX IX_ServiceType_ShopId ON [<schema>].[ServiceType]([ShopId])
CREATE INDEX IX_ServiceType_IsActive ON [<schema>].[ServiceType]([ShopId], [IsActive])
