-- TripHistory table - Aggregated trip analytics from GPS positions
CREATE TABLE [<schema>].[TripHistory]
(
    [TripHistoryId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- References
    [VehicleId] AS CAST(JSON_VALUE([Json], '$.VehicleId') AS INT),
    [RentalId] AS CAST(JSON_VALUE([Json], '$.RentalId') AS INT),
    -- Time Range
    [StartTime] AS CAST(JSON_VALUE([Json], '$.StartTime') AS DATETIMEOFFSET),
    [EndTime] AS CAST(JSON_VALUE([Json], '$.EndTime') AS DATETIMEOFFSET),
    -- Distance and Time Metrics
    [TotalDistanceKm] AS CAST(JSON_VALUE([Json], '$.TotalDistanceKm') AS FLOAT),
    [MovingTimeMinutes] AS CAST(JSON_VALUE([Json], '$.MovingTimeMinutes') AS INT),
    [IdleTimeMinutes] AS CAST(JSON_VALUE([Json], '$.IdleTimeMinutes') AS INT),
    -- Speed Metrics
    [MaxSpeedKmh] AS CAST(JSON_VALUE([Json], '$.MaxSpeedKmh') AS FLOAT),
    [AverageSpeedKmh] AS CAST(JSON_VALUE([Json], '$.AverageSpeedKmh') AS FLOAT),
    -- Fuel Estimate
    [EstimatedFuelLiters] AS CAST(JSON_VALUE([Json], '$.EstimatedFuelLiters') AS FLOAT),
    -- Behavior Metrics
    [HarshBrakingCount] AS CAST(JSON_VALUE([Json], '$.HarshBrakingCount') AS INT),
    [HarshAccelerationCount] AS CAST(JSON_VALUE([Json], '$.HarshAccelerationCount') AS INT),
    [OverspeedCount] AS CAST(JSON_VALUE([Json], '$.OverspeedCount') AS INT),
    [GeofenceViolationCount] AS CAST(JSON_VALUE([Json], '$.GeofenceViolationCount') AS INT),
    -- Denormalized
    [VehicleLicensePlate] AS CAST(JSON_VALUE([Json], '$.VehicleLicensePlate') AS NVARCHAR(20)),
    [RenterName] AS CAST(JSON_VALUE([Json], '$.RenterName') AS NVARCHAR(100)),
    -- JSON storage (includes StartLocation, EndLocation, RoutePolyline, BehaviorFlagsJson)
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
GO

-- Indexes
CREATE INDEX IX_TripHistory_VehicleId_StartTime ON [<schema>].[TripHistory]([VehicleId], [StartTime] DESC)
GO
CREATE INDEX IX_TripHistory_RentalId ON [<schema>].[TripHistory]([RentalId]) WHERE [RentalId] IS NOT NULL
GO
CREATE INDEX IX_TripHistory_StartTime ON [<schema>].[TripHistory]([StartTime] DESC)
GO
