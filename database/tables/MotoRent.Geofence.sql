-- Geofence table - Zone definitions for breach detection
CREATE TABLE [<schema>].[Geofence]
(
    [GeofenceId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Ownership
    [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
    -- Identification
    [Name] AS CAST(JSON_VALUE([Json], '$.Name') AS NVARCHAR(100)),
    [Description] AS CAST(JSON_VALUE([Json], '$.Description') AS NVARCHAR(500)),
    -- Classification
    [GeofenceType] AS CAST(JSON_VALUE([Json], '$.GeofenceType') AS NVARCHAR(20)),
    [Shape] AS CAST(JSON_VALUE([Json], '$.Shape') AS NVARCHAR(20)),
    -- Circle Definition (when Shape = 'Circle')
    [CenterLatitude] AS CAST(JSON_VALUE([Json], '$.CenterLatitude') AS FLOAT),
    [CenterLongitude] AS CAST(JSON_VALUE([Json], '$.CenterLongitude') AS FLOAT),
    [RadiusMeters] AS CAST(JSON_VALUE([Json], '$.RadiusMeters') AS FLOAT),
    -- Alert Configuration
    [AlertPriority] AS CAST(JSON_VALUE([Json], '$.AlertPriority') AS NVARCHAR(20)),
    [AlertOnEnter] AS CAST(JSON_VALUE([Json], '$.AlertOnEnter') AS BIT),
    [AlertOnExit] AS CAST(JSON_VALUE([Json], '$.AlertOnExit') AS BIT),
    [SendLineNotification] AS CAST(JSON_VALUE([Json], '$.SendLineNotification') AS BIT),
    [SendInAppNotification] AS CAST(JSON_VALUE([Json], '$.SendInAppNotification') AS BIT),
    -- Status
    [IsActive] AS CAST(JSON_VALUE([Json], '$.IsActive') AS BIT),
    [IsTemplate] AS CAST(JSON_VALUE([Json], '$.IsTemplate') AS BIT),
    [ProvinceCode] AS CAST(JSON_VALUE([Json], '$.ProvinceCode') AS NVARCHAR(10)),
    -- Display
    [MapColor] AS CAST(JSON_VALUE([Json], '$.MapColor') AS NVARCHAR(10)),
    -- JSON storage (includes Coordinates array for polygons)
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
GO

-- Indexes
CREATE INDEX IX_Geofence_ShopId_IsActive ON [<schema>].[Geofence]([ShopId], [IsActive])
GO
CREATE INDEX IX_Geofence_IsTemplate ON [<schema>].[Geofence]([IsTemplate]) WHERE [IsTemplate] = 1
GO
CREATE INDEX IX_Geofence_ProvinceCode ON [<schema>].[Geofence]([ProvinceCode]) WHERE [ProvinceCode] IS NOT NULL
GO
CREATE INDEX IX_Geofence_GeofenceType_IsActive ON [<schema>].[Geofence]([GeofenceType], [IsActive])
GO
