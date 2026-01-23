-- VehicleInspection table for 3D damage marking and inspection records
CREATE TABLE [<schema>].[VehicleInspection]
(
    [VehicleInspectionId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for efficient querying
    [VehicleId] AS CAST(JSON_VALUE([Json], '$.VehicleId') AS INT),
    [RentalId] AS CAST(JSON_VALUE([Json], '$.RentalId') AS INT),
    [MaintenanceRecordId] AS CAST(JSON_VALUE([Json], '$.MaintenanceRecordId') AS INT),
    [AccidentId] AS CAST(JSON_VALUE([Json], '$.AccidentId') AS INT),
    [InspectionType] AS CAST(JSON_VALUE([Json], '$.InspectionType') AS NVARCHAR(20)),
    [OverallCondition] AS CAST(JSON_VALUE([Json], '$.OverallCondition') AS NVARCHAR(20)),
    [InspectedAt] AS CAST(JSON_VALUE([Json], '$.InspectedAt') AS DATETIMEOFFSET),
    [OdometerReading] AS CAST(JSON_VALUE([Json], '$.OdometerReading') AS INT),
    [FuelLevel] AS CAST(JSON_VALUE([Json], '$.FuelLevel') AS INT),
    [PreviousInspectionId] AS CAST(JSON_VALUE([Json], '$.PreviousInspectionId') AS INT),
    -- JSON storage for markers and full entity data
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

-- Index for vehicle lookup
CREATE INDEX IX_VehicleInspection_VehicleId ON [<schema>].[VehicleInspection]([VehicleId])

-- Index for rental-based inspection lookup
CREATE INDEX IX_VehicleInspection_RentalId ON [<schema>].[VehicleInspection]([RentalId])

-- Index for maintenance-based inspection lookup
CREATE INDEX IX_VehicleInspection_MaintenanceRecordId ON [<schema>].[VehicleInspection]([MaintenanceRecordId])

-- Index for accident-based inspection lookup
CREATE INDEX IX_VehicleInspection_AccidentId ON [<schema>].[VehicleInspection]([AccidentId])

-- Composite index for inspection type and date
CREATE INDEX IX_VehicleInspection_Type_Date ON [<schema>].[VehicleInspection]([InspectionType], [InspectedAt])
