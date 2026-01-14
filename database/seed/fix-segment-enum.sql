SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO

-- Fix Toyota Vios (VehicleModelId = 2) - SmallSedan = 0
UPDATE [Core].[VehicleModel]
SET [Json] = N'{
    "Make": "Toyota",
    "Model": "Vios",
    "VehicleType": "Car",
    "Segment": 0,
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

-- Fix Honda City (VehicleModelId = 14) - SmallSedan = 0
UPDATE [Core].[VehicleModel]
SET [Json] = N'{
    "Make": "Honda",
    "Model": "City",
    "VehicleType": "Car",
    "Segment": 0,
    "EngineLiters": 1.5,
    "SeatCount": 5,
    "YearFrom": 2019,
    "IsActive": true,
    "DisplayOrder": 21,
    "Aliases": ["City 1.5"],
    "SuggestedDailyRate": 1300,
    "SuggestedDeposit": 10000,
    "CountryOfOrigin": "Thailand"
}'
WHERE VehicleModelId = 14
GO

-- Fix Toyota Yaris Ativ (VehicleModelId = 15) - SmallSedan = 0
UPDATE [Core].[VehicleModel]
SET [Json] = N'{
    "Make": "Toyota",
    "Model": "Yaris Ativ",
    "VehicleType": "Car",
    "Segment": 0,
    "EngineLiters": 1.2,
    "SeatCount": 5,
    "YearFrom": 2017,
    "IsActive": true,
    "DisplayOrder": 22,
    "Aliases": ["Yaris", "Ativ"],
    "SuggestedDailyRate": 1100,
    "SuggestedDeposit": 8000,
    "CountryOfOrigin": "Thailand"
}'
WHERE VehicleModelId = 15
GO

-- Fix Toyota Fortuner (VehicleModelId = 16) - SUV = 2
UPDATE [Core].[VehicleModel]
SET [Json] = N'{
    "Make": "Toyota",
    "Model": "Fortuner",
    "VehicleType": "Car",
    "Segment": 2,
    "EngineLiters": 2.4,
    "SeatCount": 7,
    "YearFrom": 2015,
    "IsActive": true,
    "DisplayOrder": 23,
    "Aliases": ["Fortuner 2.4"],
    "SuggestedDailyRate": 2500,
    "SuggestedDeposit": 20000,
    "CountryOfOrigin": "Thailand"
}'
WHERE VehicleModelId = 16
GO

-- Fix Honda CR-V (VehicleModelId = 17) - SUV = 2
UPDATE [Core].[VehicleModel]
SET [Json] = N'{
    "Make": "Honda",
    "Model": "CR-V",
    "VehicleType": "Car",
    "Segment": 2,
    "EngineLiters": 2.4,
    "SeatCount": 5,
    "YearFrom": 2017,
    "IsActive": true,
    "DisplayOrder": 24,
    "Aliases": ["CRV"],
    "SuggestedDailyRate": 2200,
    "SuggestedDeposit": 18000,
    "CountryOfOrigin": "Thailand"
}'
WHERE VehicleModelId = 17
GO

-- Fix Toyota Camry (VehicleModelId = 18) - BigSedan = 1
UPDATE [Core].[VehicleModel]
SET [Json] = N'{
    "Make": "Toyota",
    "Model": "Camry",
    "VehicleType": "Car",
    "Segment": 1,
    "EngineLiters": 2.5,
    "SeatCount": 5,
    "YearFrom": 2018,
    "IsActive": true,
    "DisplayOrder": 25,
    "Aliases": ["Camry 2.5"],
    "SuggestedDailyRate": 2800,
    "SuggestedDeposit": 25000,
    "CountryOfOrigin": "Thailand"
}'
WHERE VehicleModelId = 18
GO

-- Display all records to verify
SELECT VehicleModelId, Make, Model, VehicleType, Segment, EngineCC, IsActive, DisplayOrder
FROM [Core].[VehicleModel]
ORDER BY DisplayOrder
GO
