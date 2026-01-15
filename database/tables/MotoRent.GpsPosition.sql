-- GpsPosition table - Position records from GPS devices (90-day retention)
CREATE TABLE [<schema>].[GpsPosition]
(
    [GpsPositionId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Device and Vehicle Links
    [GpsTrackingDeviceId] AS CAST(JSON_VALUE([Json], '$.GpsTrackingDeviceId') AS INT),
    [VehicleId] AS CAST(JSON_VALUE([Json], '$.VehicleId') AS INT),
    -- Position Data
    [Latitude] AS CAST(JSON_VALUE([Json], '$.Latitude') AS FLOAT),
    [Longitude] AS CAST(JSON_VALUE([Json], '$.Longitude') AS FLOAT),
    [Altitude] AS CAST(JSON_VALUE([Json], '$.Altitude') AS FLOAT),
    [Speed] AS CAST(JSON_VALUE([Json], '$.Speed') AS FLOAT),
    [Heading] AS CAST(JSON_VALUE([Json], '$.Heading') AS FLOAT),
    [Accuracy] AS CAST(JSON_VALUE([Json], '$.Accuracy') AS FLOAT),
    [SatelliteCount] AS CAST(JSON_VALUE([Json], '$.SatelliteCount') AS INT),
    -- Timestamps
    [DeviceTimestamp] AS CAST(JSON_VALUE([Json], '$.DeviceTimestamp') AS DATETIMEOFFSET),
    [ReceivedTimestamp] AS CAST(JSON_VALUE([Json], '$.ReceivedTimestamp') AS DATETIMEOFFSET),
    -- Vehicle State
    [IgnitionOn] AS CAST(JSON_VALUE([Json], '$.IgnitionOn') AS BIT),
    -- Denormalized
    [VehicleLicensePlate] AS CAST(JSON_VALUE([Json], '$.VehicleLicensePlate') AS NVARCHAR(20)),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
GO

-- Indexes for common queries
CREATE INDEX IX_GpsPosition_VehicleId_DeviceTimestamp ON [<schema>].[GpsPosition]([VehicleId], [DeviceTimestamp] DESC)
GO
CREATE INDEX IX_GpsPosition_DeviceTimestamp ON [<schema>].[GpsPosition]([DeviceTimestamp])
GO
CREATE INDEX IX_GpsPosition_GpsTrackingDeviceId ON [<schema>].[GpsPosition]([GpsTrackingDeviceId])
GO
CREATE INDEX IX_GpsPosition_ReceivedTimestamp ON [<schema>].[GpsPosition]([ReceivedTimestamp])
GO

-- Note: 90-day retention policy enforced via scheduled job
-- DELETE FROM [<schema>].[GpsPosition] WHERE [DeviceTimestamp] < DATEADD(DAY, -90, SYSDATETIMEOFFSET())
