-- MaintenanceSchedule table - Per-motorbike maintenance tracking
CREATE TABLE [<schema>].[MaintenanceSchedule]
(
    [MaintenanceScheduleId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for querying
    [MotorbikeId] AS CAST(JSON_VALUE([Json], '$.MotorbikeId') AS INT),
    [ServiceTypeId] AS CAST(JSON_VALUE([Json], '$.ServiceTypeId') AS INT),
    [LastServiceDate] AS CAST(JSON_VALUE([Json], '$.LastServiceDate') AS DATE),
    [LastServiceMileage] AS CAST(JSON_VALUE([Json], '$.LastServiceMileage') AS INT),
    [NextDueDate] AS CAST(JSON_VALUE([Json], '$.NextDueDate') AS DATE),
    [NextDueMileage] AS CAST(JSON_VALUE([Json], '$.NextDueMileage') AS INT),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE INDEX IX_MaintenanceSchedule_MotorbikeId ON [<schema>].[MaintenanceSchedule]([MotorbikeId])
CREATE INDEX IX_MaintenanceSchedule_ServiceTypeId ON [<schema>].[MaintenanceSchedule]([ServiceTypeId])
CREATE INDEX IX_MaintenanceSchedule_NextDueDate ON [<schema>].[MaintenanceSchedule]([NextDueDate])
CREATE INDEX IX_MaintenanceSchedule_Composite ON [<schema>].[MaintenanceSchedule]([MotorbikeId], [ServiceTypeId])
