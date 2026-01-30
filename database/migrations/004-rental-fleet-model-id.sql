-- Migration 004: Backfill Rental.FleetModelId from Vehicle.FleetModelId
-- Adds FleetModelId to existing rentals for direct fee lookups and reporting.
--
-- Replace <schema> with the tenant schema name.

-- Step 1: Backfill FleetModelId on rentals that have a VehicleId linked to a Vehicle
UPDATE r
SET r.[Json] = JSON_MODIFY(r.[Json], '$.FleetModelId', v.FleetModelId)
FROM [<schema>].[Rental] r
INNER JOIN [<schema>].[Vehicle] v
    ON v.VehicleId = r.VehicleId
WHERE r.FleetModelId IS NULL
    AND v.FleetModelId > 0;

-- Step 2: Verify backfill results
SELECT
    COUNT(*) AS TotalRentals,
    SUM(CASE WHEN FleetModelId IS NOT NULL AND FleetModelId > 0 THEN 1 ELSE 0 END) AS WithFleetModelId,
    SUM(CASE WHEN FleetModelId IS NULL OR FleetModelId = 0 THEN 1 ELSE 0 END) AS WithoutFleetModelId
FROM [<schema>].[Rental];
