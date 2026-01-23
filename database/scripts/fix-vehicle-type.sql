-- Fix VehicleType from 'Scooter' to 'Motorbike' (valid enum value)
UPDATE [KrabiBeachRentals].[Vehicle]
SET [Json] = JSON_MODIFY([Json], '$.VehicleType', 'Motorbike')
WHERE [VehicleType] = 'Scooter'

SELECT [VehicleId], [VehicleType], [Brand], [Model], [Status] FROM [KrabiBeachRentals].[Vehicle]
