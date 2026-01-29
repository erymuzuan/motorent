-- Migration: Create FleetModel table and populate from existing Vehicle data
-- This script is idempotent and safe to run multiple times.

-- Step 1: Create FleetModel table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'FleetModel' AND schema_id = SCHEMA_ID('<schema>'))
BEGIN
    CREATE TABLE [<schema>].[FleetModel]
    (
        [FleetModelId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
        [VehicleModelId] AS CAST(JSON_VALUE([Json], '$.VehicleModelId') AS INT),
        [Brand] AS CAST(JSON_VALUE([Json], '$.Brand') AS NVARCHAR(50)),
        [Model] AS CAST(JSON_VALUE([Json], '$.Model') AS NVARCHAR(50)),
        [Year] AS CAST(JSON_VALUE([Json], '$.Year') AS INT),
        [VehicleType] AS CAST(JSON_VALUE([Json], '$.VehicleType') AS NVARCHAR(20)),
        [DailyRate] AS CAST(JSON_VALUE([Json], '$.DailyRate') AS DECIMAL(10,2)),
        [DurationType] AS CAST(JSON_VALUE([Json], '$.DurationType') AS NVARCHAR(20)),
        [IsActive] AS CAST(JSON_VALUE([Json], '$.IsActive') AS BIT),
        [Json] NVARCHAR(MAX) NOT NULL,
        [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
        [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
        [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );

    CREATE INDEX IX_FleetModel_VehicleType_IsActive ON [<schema>].[FleetModel]([VehicleType], [IsActive]);
    CREATE INDEX IX_FleetModel_Brand_Model ON [<schema>].[FleetModel]([Brand], [Model]);

    PRINT 'Created FleetModel table';
END
ELSE
    PRINT 'FleetModel table already exists';
GO

-- Step 2: Add FleetModelId computed column to Vehicle table if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('<schema>.Vehicle') AND name = 'FleetModelId')
BEGIN
    ALTER TABLE [<schema>].[Vehicle]
    ADD [FleetModelId] AS CAST(JSON_VALUE([Json], '$.FleetModelId') AS INT);

    CREATE INDEX IX_Vehicle_FleetModelId ON [<schema>].[Vehicle]([FleetModelId]);

    PRINT 'Added FleetModelId column to Vehicle table';
END
ELSE
    PRINT 'FleetModelId column already exists on Vehicle table';
GO

-- Step 3: Create FleetModel records from existing vehicles (grouped by Brand|Model|Year|VehicleType|EngineCC|EngineLiters)
-- Only creates records for groups that don't already have a FleetModel
;WITH VehicleGroups AS (
    SELECT
        JSON_VALUE([Json], '$.Brand') AS Brand,
        JSON_VALUE([Json], '$.Model') AS Model,
        CAST(JSON_VALUE([Json], '$.Year') AS INT) AS [Year],
        JSON_VALUE([Json], '$.VehicleType') AS VehicleType,
        -- Use first vehicle in group for shared attributes
        MIN([VehicleId]) AS FirstVehicleId
    FROM [<schema>].[Vehicle]
    WHERE JSON_VALUE([Json], '$.Brand') IS NOT NULL
      AND JSON_VALUE([Json], '$.Model') IS NOT NULL
    GROUP BY
        JSON_VALUE([Json], '$.Brand'),
        JSON_VALUE([Json], '$.Model'),
        CAST(JSON_VALUE([Json], '$.Year') AS INT),
        JSON_VALUE([Json], '$.VehicleType')
)
INSERT INTO [<schema>].[FleetModel] ([Json], [CreatedBy], [ChangedBy])
SELECT
    JSON_MODIFY(
    JSON_MODIFY(
    JSON_MODIFY(
    JSON_MODIFY(
    JSON_MODIFY(
    JSON_MODIFY(
    JSON_MODIFY(
    JSON_MODIFY(
    JSON_MODIFY(
    JSON_MODIFY(
    JSON_MODIFY(
    JSON_MODIFY(
    JSON_MODIFY(
    JSON_MODIFY(
    JSON_MODIFY(
    JSON_MODIFY(
    JSON_MODIFY(
    JSON_MODIFY(
    JSON_MODIFY(
    JSON_MODIFY(
    JSON_MODIFY(
    JSON_MODIFY('{}',
        '$.Brand', JSON_VALUE(v.[Json], '$.Brand')),
        '$.Model', JSON_VALUE(v.[Json], '$.Model')),
        '$.Year', CAST(JSON_VALUE(v.[Json], '$.Year') AS INT)),
        '$.VehicleType', JSON_VALUE(v.[Json], '$.VehicleType')),
        '$.EngineCC', CAST(JSON_VALUE(v.[Json], '$.EngineCC') AS INT)),
        '$.EngineLiters', CAST(JSON_VALUE(v.[Json], '$.EngineLiters') AS FLOAT)),
        '$.Segment', JSON_VALUE(v.[Json], '$.Segment')),
        '$.Transmission', JSON_VALUE(v.[Json], '$.Transmission')),
        '$.SeatCount', CAST(JSON_VALUE(v.[Json], '$.SeatCount') AS INT)),
        '$.PassengerCapacity', CAST(JSON_VALUE(v.[Json], '$.PassengerCapacity') AS INT)),
        '$.MaxRiderWeight', CAST(JSON_VALUE(v.[Json], '$.MaxRiderWeight') AS INT)),
        '$.DailyRate', CAST(JSON_VALUE(v.[Json], '$.DailyRate') AS FLOAT)),
        '$.HourlyRate', CAST(JSON_VALUE(v.[Json], '$.HourlyRate') AS FLOAT)),
        '$.Rate15Min', CAST(JSON_VALUE(v.[Json], '$.Rate15Min') AS FLOAT)),
        '$.Rate30Min', CAST(JSON_VALUE(v.[Json], '$.Rate30Min') AS FLOAT)),
        '$.Rate1Hour', CAST(JSON_VALUE(v.[Json], '$.Rate1Hour') AS FLOAT)),
        '$.DepositAmount', CAST(JSON_VALUE(v.[Json], '$.DepositAmount') AS FLOAT)),
        '$.DurationType', JSON_VALUE(v.[Json], '$.DurationType')),
        '$.DriverDailyFee', CAST(JSON_VALUE(v.[Json], '$.DriverDailyFee') AS FLOAT)),
        '$.GuideDailyFee', CAST(JSON_VALUE(v.[Json], '$.GuideDailyFee') AS FLOAT)),
        '$.IsActive', CAST(1 AS BIT)),
        '$.$type', 'FleetModel')
    AS [Json],
    'migration' AS [CreatedBy],
    'migration' AS [ChangedBy]
FROM VehicleGroups vg
INNER JOIN [<schema>].[Vehicle] v ON v.[VehicleId] = vg.FirstVehicleId
WHERE NOT EXISTS (
    SELECT 1 FROM [<schema>].[FleetModel] fm
    WHERE CAST(JSON_VALUE(fm.[Json], '$.Brand') AS NVARCHAR(50)) = vg.Brand
      AND CAST(JSON_VALUE(fm.[Json], '$.Model') AS NVARCHAR(50)) = vg.Model
      AND CAST(JSON_VALUE(fm.[Json], '$.Year') AS INT) = vg.[Year]
      AND CAST(JSON_VALUE(fm.[Json], '$.VehicleType') AS NVARCHAR(20)) = vg.VehicleType
);

PRINT 'Created FleetModel records from existing vehicle groups';
GO

-- Step 4: Link vehicles to their FleetModel records
UPDATE v
SET v.[Json] = JSON_MODIFY(v.[Json], '$.FleetModelId', fm.[FleetModelId])
FROM [<schema>].[Vehicle] v
INNER JOIN [<schema>].[FleetModel] fm
    ON CAST(JSON_VALUE(fm.[Json], '$.Brand') AS NVARCHAR(50)) = CAST(JSON_VALUE(v.[Json], '$.Brand') AS NVARCHAR(50))
    AND CAST(JSON_VALUE(fm.[Json], '$.Model') AS NVARCHAR(50)) = CAST(JSON_VALUE(v.[Json], '$.Model') AS NVARCHAR(50))
    AND CAST(JSON_VALUE(fm.[Json], '$.Year') AS INT) = CAST(JSON_VALUE(v.[Json], '$.Year') AS INT)
    AND CAST(JSON_VALUE(fm.[Json], '$.VehicleType') AS NVARCHAR(20)) = CAST(JSON_VALUE(v.[Json], '$.VehicleType') AS NVARCHAR(20))
WHERE CAST(JSON_VALUE(v.[Json], '$.FleetModelId') AS INT) IS NULL;

PRINT 'Linked vehicles to FleetModel records';
GO
