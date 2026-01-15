-- GpsTrackingDevice table - GPS device registered to a vehicle
CREATE TABLE [<schema>].[GpsTrackingDevice]
(
    [GpsTrackingDeviceId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Vehicle Link
    [VehicleId] AS CAST(JSON_VALUE([Json], '$.VehicleId') AS INT),
    -- Provider Info
    [Provider] AS CAST(JSON_VALUE([Json], '$.Provider') AS NVARCHAR(50)),
    [DeviceId] AS CAST(JSON_VALUE([Json], '$.DeviceId') AS NVARCHAR(100)),
    [Imei] AS CAST(JSON_VALUE([Json], '$.Imei') AS NVARCHAR(20)),
    [SimNumber] AS CAST(JSON_VALUE([Json], '$.SimNumber') AS NVARCHAR(20)),
    -- Status
    [IsActive] AS CAST(JSON_VALUE([Json], '$.IsActive') AS BIT),
    [JammingDetected] AS CAST(JSON_VALUE([Json], '$.JammingDetected') AS BIT),
    [PowerDisconnected] AS CAST(JSON_VALUE([Json], '$.PowerDisconnected') AS BIT),
    [BatteryPercent] AS CAST(JSON_VALUE([Json], '$.BatteryPercent') AS INT),
    [LastContactTimestamp] AS CAST(JSON_VALUE([Json], '$.LastContactTimestamp') AS DATETIMEOFFSET),
    -- Kill Switch (UI placeholder)
    [SupportsKillSwitch] AS CAST(JSON_VALUE([Json], '$.SupportsKillSwitch') AS BIT),
    [KillSwitchActivated] AS CAST(JSON_VALUE([Json], '$.KillSwitchActivated') AS BIT),
    -- Denormalized display fields
    [VehicleLicensePlate] AS CAST(JSON_VALUE([Json], '$.VehicleLicensePlate') AS NVARCHAR(20)),
    [VehicleDisplayName] AS CAST(JSON_VALUE([Json], '$.VehicleDisplayName') AS NVARCHAR(100)),
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
CREATE UNIQUE INDEX IX_GpsTrackingDevice_VehicleId ON [<schema>].[GpsTrackingDevice]([VehicleId])
GO
CREATE INDEX IX_GpsTrackingDevice_Provider_DeviceId ON [<schema>].[GpsTrackingDevice]([Provider], [DeviceId])
GO
CREATE INDEX IX_GpsTrackingDevice_IsActive ON [<schema>].[GpsTrackingDevice]([IsActive])
GO
CREATE INDEX IX_GpsTrackingDevice_JammingDetected ON [<schema>].[GpsTrackingDevice]([JammingDetected]) WHERE [JammingDetected] = 1
GO
CREATE INDEX IX_GpsTrackingDevice_PowerDisconnected ON [<schema>].[GpsTrackingDevice]([PowerDisconnected]) WHERE [PowerDisconnected] = 1
GO
