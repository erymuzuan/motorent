-- GeofenceAlert table - Geofence breach notifications
CREATE TABLE [<schema>].[GeofenceAlert]
(
    [GeofenceAlertId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- References
    [GeofenceId] AS CAST(JSON_VALUE([Json], '$.GeofenceId') AS INT),
    [VehicleId] AS CAST(JSON_VALUE([Json], '$.VehicleId') AS INT),
    [RentalId] AS CAST(JSON_VALUE([Json], '$.RentalId') AS INT),
    [GpsPositionId] AS CAST(JSON_VALUE([Json], '$.GpsPositionId') AS INT),
    -- Alert Info
    [AlertType] AS CAST(JSON_VALUE([Json], '$.AlertType') AS NVARCHAR(20)),
    [Priority] AS CAST(JSON_VALUE([Json], '$.Priority') AS NVARCHAR(20)),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)),
    [AlertTimestamp] AS CAST(JSON_VALUE([Json], '$.AlertTimestamp') AS DATETIMEOFFSET),
    -- Location at time of breach
    [Latitude] AS CAST(JSON_VALUE([Json], '$.Latitude') AS FLOAT),
    [Longitude] AS CAST(JSON_VALUE([Json], '$.Longitude') AS FLOAT),
    [LocationDescription] AS CAST(JSON_VALUE([Json], '$.LocationDescription') AS NVARCHAR(200)),
    -- Notification Status
    [LineNotificationSent] AS CAST(JSON_VALUE([Json], '$.LineNotificationSent') AS BIT),
    [LineMessageId] AS CAST(JSON_VALUE([Json], '$.LineMessageId') AS NVARCHAR(100)),
    -- Resolution
    [ResolvedBy] AS CAST(JSON_VALUE([Json], '$.ResolvedBy') AS NVARCHAR(100)),
    [ResolvedTimestamp] AS CAST(JSON_VALUE([Json], '$.ResolvedTimestamp') AS DATETIMEOFFSET),
    -- Denormalized fields
    [GeofenceName] AS CAST(JSON_VALUE([Json], '$.GeofenceName') AS NVARCHAR(100)),
    [VehicleLicensePlate] AS CAST(JSON_VALUE([Json], '$.VehicleLicensePlate') AS NVARCHAR(20)),
    [VehicleDisplayName] AS CAST(JSON_VALUE([Json], '$.VehicleDisplayName') AS NVARCHAR(100)),
    [RenterName] AS CAST(JSON_VALUE([Json], '$.RenterName') AS NVARCHAR(100)),
    [RenterPhone] AS CAST(JSON_VALUE([Json], '$.RenterPhone') AS NVARCHAR(20)),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
GO

-- Indexes
CREATE INDEX IX_GeofenceAlert_VehicleId_Status ON [<schema>].[GeofenceAlert]([VehicleId], [Status])
GO
CREATE INDEX IX_GeofenceAlert_Status_AlertTimestamp ON [<schema>].[GeofenceAlert]([Status], [AlertTimestamp] DESC)
GO
CREATE INDEX IX_GeofenceAlert_Status_Priority ON [<schema>].[GeofenceAlert]([Status], [Priority])
GO
CREATE INDEX IX_GeofenceAlert_GeofenceId ON [<schema>].[GeofenceAlert]([GeofenceId])
GO
CREATE INDEX IX_GeofenceAlert_RentalId ON [<schema>].[GeofenceAlert]([RentalId]) WHERE [RentalId] IS NOT NULL
GO
CREATE INDEX IX_GeofenceAlert_AlertTimestamp ON [<schema>].[GeofenceAlert]([AlertTimestamp] DESC)
GO
