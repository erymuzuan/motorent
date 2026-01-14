SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO

-- Fix Honda Click 125 (VehicleModelId = 1)
UPDATE [Core].[VehicleModel]
SET [Json] = N'{
    "Make": "Honda",
    "Model": "Click 125",
    "VehicleType": "Motorbike",
    "EngineCC": 125,
    "YearFrom": 2018,
    "IsActive": true,
    "DisplayOrder": 1,
    "Aliases": ["Click", "Click125", "Click 125i"],
    "SuggestedDailyRate": 250,
    "SuggestedDeposit": 3000,
    "CountryOfOrigin": "Thailand"
}'
WHERE VehicleModelId = 1
GO

-- Fix Toyota Vios (VehicleModelId = 2)
UPDATE [Core].[VehicleModel]
SET [Json] = N'{
    "Make": "Toyota",
    "Model": "Vios",
    "VehicleType": "Car",
    "Segment": "Sedan",
    "EngineLiters": 1.5,
    "SeatCount": 5,
    "YearFrom": 2018,
    "IsActive": true,
    "DisplayOrder": 20,
    "Aliases": ["Vios 1.5"],
    "SuggestedDailyRate": 1200,
    "SuggestedDeposit": 10000,
    "CountryOfOrigin": "Thailand"
}'
WHERE VehicleModelId = 2
GO

-- Display all records
SELECT VehicleModelId, Make, Model, VehicleType, EngineCC, IsActive, DisplayOrder
FROM [Core].[VehicleModel]
ORDER BY DisplayOrder
GO
