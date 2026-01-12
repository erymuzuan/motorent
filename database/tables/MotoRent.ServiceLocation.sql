-- ServiceLocation table
-- Predefined pick-up/drop-off locations with associated fees (Airport, Hotels, Ferry terminals)
CREATE TABLE [<schema>].[ServiceLocation]
(
    [ServiceLocationId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for querying
    [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
    [Name] AS CAST(JSON_VALUE([Json], '$.Name') AS NVARCHAR(200)),
    [LocationType] AS CAST(JSON_VALUE([Json], '$.LocationType') AS NVARCHAR(50)),
    [PickupFee] AS CAST(COALESCE(JSON_VALUE([Json], '$.PickupFee'), '0') AS DECIMAL(18,2)),
    [DropoffFee] AS CAST(COALESCE(JSON_VALUE([Json], '$.DropoffFee'), '0') AS DECIMAL(18,2)),
    [PickupAvailable] AS CAST(COALESCE(JSON_VALUE([Json], '$.PickupAvailable'), 'true') AS BIT),
    [DropoffAvailable] AS CAST(COALESCE(JSON_VALUE([Json], '$.DropoffAvailable'), 'true') AS BIT),
    [IsActive] AS CAST(COALESCE(JSON_VALUE([Json], '$.IsActive'), 'true') AS BIT),
    [DisplayOrder] AS CAST(COALESCE(JSON_VALUE([Json], '$.DisplayOrder'), '0') AS INT),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
GO

-- Index for shop lookup
CREATE INDEX IX_ServiceLocation_ShopId ON [<schema>].[ServiceLocation]([ShopId])
GO

-- Index for active locations by type
CREATE INDEX IX_ServiceLocation_ActiveType ON [<schema>].[ServiceLocation]([ShopId], [LocationType], [IsActive])
    WHERE [IsActive] = 1
GO
