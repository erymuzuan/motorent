-- Remove Segment from motorbikes (only applicable for Cars)
-- For motorbikes, Segment should be null/not present
UPDATE [KrabiBeachRentals].[Vehicle]
SET [Json] = JSON_MODIFY([Json], '$.Segment', NULL)
WHERE [VehicleType] = 'Motorbike'

-- Verify the fix
SELECT [VehicleId], [VehicleType], JSON_VALUE([Json], '$.Segment') AS Segment, [Brand], [Model]
FROM [KrabiBeachRentals].[Vehicle]
