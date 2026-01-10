-- Maintenance Tracking Schema
-- SQL Server with JSON columns and computed columns for indexing
-- Schema placeholder: <schema> (replaced with tenant's AccountNo at runtime)

SET QUOTED_IDENTIFIER ON
GO

-- ServiceType table - Configurable maintenance service types
IF OBJECT_ID('[<schema>].[ServiceType]', 'U') IS NULL
BEGIN
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

    PRINT 'Created ServiceType table'
END
GO

-- MaintenanceSchedule table - Per-motorbike maintenance tracking
IF OBJECT_ID('[<schema>].[MaintenanceSchedule]', 'U') IS NULL
BEGIN
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

    PRINT 'Created MaintenanceSchedule table'
END
GO

PRINT 'Maintenance schema created successfully'
GO
