-- Migration 003: Make Vehicle.FleetModelId required
-- Auto-creates FleetModel records for vehicles that don't have one,
-- grouping by Brand|Model|Year|VehicleType|Engine, then links them.
--
-- Run this BEFORE deploying the code that changes FleetModelId from int? to int.
-- Replace <schema> with the tenant schema name.

-- Step 1: Create FleetModel records for orphan vehicle groups
INSERT INTO [<schema>].[FleetModel] ([Json], [CreatedBy], [ChangedBy], [CreatedTimestamp], [ChangedTimestamp])
SELECT
    (
        SELECT
            v.Brand AS Brand,
            v.Model AS Model,
            v.Year AS [Year],
            v.VehicleType AS VehicleType,
            v.EngineCC AS EngineCC,
            v.EngineLiters AS EngineLiters,
            v.Segment AS Segment,
            v.Transmission AS Transmission,
            v.SeatCount AS SeatCount,
            v.PassengerCapacity AS PassengerCapacity,
            v.MaxRiderWeight AS MaxRiderWeight,
            MAX(v.DailyRate) AS DailyRate,
            MAX(v.HourlyRate) AS HourlyRate,
            MAX(v.Rate15Min) AS Rate15Min,
            MAX(v.Rate30Min) AS Rate30Min,
            MAX(v.Rate1Hour) AS Rate1Hour,
            MAX(v.DepositAmount) AS DepositAmount,
            v.DurationType AS DurationType,
            MAX(v.DriverDailyFee) AS DriverDailyFee,
            MAX(v.GuideDailyFee) AS GuideDailyFee,
            CAST(1 AS BIT) AS IsActive
        FOR JSON PATH, WITHOUT_ARRAY_WRAPPER
    ) AS [Json],
    'migration' AS [CreatedBy],
    'migration' AS [ChangedBy],
    SYSDATETIMEOFFSET() AS [CreatedTimestamp],
    SYSDATETIMEOFFSET() AS [ChangedTimestamp]
FROM [<schema>].[Vehicle] v
WHERE v.FleetModelId IS NULL OR v.FleetModelId = 0
GROUP BY v.Brand, v.Model, v.[Year], v.VehicleType, v.EngineCC, v.EngineLiters,
         v.Segment, v.Transmission, v.SeatCount, v.PassengerCapacity, v.MaxRiderWeight, v.DurationType;

-- Step 2: Link orphan vehicles to their newly created FleetModel
UPDATE v
SET v.[Json] = JSON_MODIFY(v.[Json], '$.FleetModelId', fm.FleetModelId)
FROM [<schema>].[Vehicle] v
INNER JOIN [<schema>].[FleetModel] fm
    ON fm.Brand = v.Brand
    AND fm.Model = v.Model
    AND fm.[Year] = v.[Year]
    AND fm.VehicleType = v.VehicleType
WHERE v.FleetModelId IS NULL OR v.FleetModelId = 0;

-- Step 3: Verify no orphans remain
SELECT COUNT(*) AS OrphanCount
FROM [<schema>].[Vehicle]
WHERE FleetModelId IS NULL OR FleetModelId = 0;
-- Expected: 0
