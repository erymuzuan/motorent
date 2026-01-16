-- MaintenanceRecord table - Detailed service history with attachments
CREATE TABLE [<schema>].[MaintenanceRecord]
(
    [MaintenanceRecordId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for querying
    [VehicleId] AS CAST(JSON_VALUE([Json], '$.VehicleId') AS INT),
    [ServiceTypeId] AS CAST(JSON_VALUE([Json], '$.ServiceTypeId') AS INT),
    [ServiceTypeName] AS CAST(JSON_VALUE([Json], '$.ServiceTypeName') AS NVARCHAR(100)),
    [ServiceDate] DATE NULL,
    [ServiceMileage] AS CAST(JSON_VALUE([Json], '$.ServiceMileage') AS INT),
    [Cost] AS CAST(JSON_VALUE([Json], '$.Cost') AS MONEY),
    [WorkshopName] AS CAST(JSON_VALUE([Json], '$.Workshop.Name') AS NVARCHAR(200)),
    -- JSON storage (includes Photos[], Documents[], Workshop{})
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE INDEX IX_MaintenanceRecord_VehicleId ON [<schema>].[MaintenanceRecord]([VehicleId])
CREATE INDEX IX_MaintenanceRecord_ServiceTypeId ON [<schema>].[MaintenanceRecord]([ServiceTypeId])
CREATE INDEX IX_MaintenanceRecord_ServiceDate ON [<schema>].[MaintenanceRecord]([ServiceDate])
CREATE INDEX IX_MaintenanceRecord_Composite ON [<schema>].[MaintenanceRecord]([VehicleId], [ServiceDate])
