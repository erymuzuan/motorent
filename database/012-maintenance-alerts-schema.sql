-- Maintenance Alerts Schema
-- SQL Server with JSON columns and computed columns for indexing
-- Schema placeholder: <schema> (replaced with tenant's AccountNo at runtime)

SET QUOTED_IDENTIFIER ON
GO

-- MaintenanceAlert table - Automated maintenance alerts for vehicles
IF OBJECT_ID('[<schema>].[MaintenanceAlert]', 'U') IS NULL
BEGIN
    CREATE TABLE [<schema>].[MaintenanceAlert]
    (
        [MaintenanceAlertId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
        -- Computed columns for querying
        [VehicleId] AS CAST(JSON_VALUE([Json], '$.VehicleId') AS INT),
        [ServiceTypeId] AS CAST(JSON_VALUE([Json], '$.ServiceTypeId') AS INT),
        [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS INT), -- MaintenanceStatus enum
        [IsRead] AS CAST(JSON_VALUE([Json], '$.IsRead') AS BIT),
        [TriggerDate] AS CAST(JSON_VALUE([Json], '$.TriggerDate') AS DATETIMEOFFSET),
        [ResolvedDate] AS CAST(JSON_VALUE([Json], '$.ResolvedDate') AS DATETIMEOFFSET),
        -- JSON storage
        [Json] NVARCHAR(MAX) NOT NULL,
        -- Audit columns
        [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
        [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
        [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    )

    CREATE INDEX IX_MaintenanceAlert_VehicleId ON [<schema>].[MaintenanceAlert]([VehicleId])
    CREATE INDEX IX_MaintenanceAlert_ServiceTypeId ON [<schema>].[MaintenanceAlert]([ServiceTypeId])
    CREATE INDEX IX_MaintenanceAlert_Status ON [<schema>].[MaintenanceAlert]([Status])
    CREATE INDEX IX_MaintenanceAlert_IsRead ON [<schema>].[MaintenanceAlert]([IsRead])
    CREATE INDEX IX_MaintenanceAlert_TriggerDate ON [<schema>].[MaintenanceAlert]([TriggerDate])

    PRINT 'Created MaintenanceAlert table'
END
GO
