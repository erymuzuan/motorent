-- Vehicle Model Seed Data for Phuket Rentals
-- Common motorbikes, cars, and vans in Thai market

SET QUOTED_IDENTIFIER ON
SET ANSI_NULLS ON
GO

-- Clear existing data (optional - comment out if you want to preserve existing)
-- DELETE FROM [Core].[VehicleModel]

-- =============================================
-- MOTORBIKES - Popular Thai Scooters
-- =============================================

-- Honda Click 125 (Most popular tourist scooter)
INSERT INTO [Core].[VehicleModel] ([Json])
VALUES (N'{
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
}')

-- Honda Click 160
INSERT INTO [Core].[VehicleModel] ([Json])
VALUES (N'{
    "Make": "Honda",
    "Model": "Click 160",
    "VehicleType": "Motorbike",
    "EngineCC": 160,
    "YearFrom": 2022,
    "IsActive": true,
    "DisplayOrder": 2,
    "Aliases": ["Click160", "Click 160i"],
    "SuggestedDailyRate": 300,
    "SuggestedDeposit": 3500,
    "CountryOfOrigin": "Thailand"
}')

-- Honda PCX 160
INSERT INTO [Core].[VehicleModel] ([Json])
VALUES (N'{
    "Make": "Honda",
    "Model": "PCX 160",
    "VehicleType": "Motorbike",
    "EngineCC": 160,
    "YearFrom": 2021,
    "IsActive": true,
    "DisplayOrder": 3,
    "Aliases": ["PCX", "PCX160"],
    "SuggestedDailyRate": 400,
    "SuggestedDeposit": 5000,
    "CountryOfOrigin": "Thailand"
}')

-- Honda Scoopy
INSERT INTO [Core].[VehicleModel] ([Json])
VALUES (N'{
    "Make": "Honda",
    "Model": "Scoopy",
    "VehicleType": "Motorbike",
    "EngineCC": 110,
    "YearFrom": 2017,
    "IsActive": true,
    "DisplayOrder": 4,
    "Aliases": ["Scoopy i", "Scoopyi"],
    "SuggestedDailyRate": 250,
    "SuggestedDeposit": 3000,
    "CountryOfOrigin": "Thailand"
}')

-- Honda Wave 110
INSERT INTO [Core].[VehicleModel] ([Json])
VALUES (N'{
    "Make": "Honda",
    "Model": "Wave 110",
    "VehicleType": "Motorbike",
    "EngineCC": 110,
    "YearFrom": 2015,
    "IsActive": true,
    "DisplayOrder": 5,
    "Aliases": ["Wave", "Wave110", "Wave 110i"],
    "SuggestedDailyRate": 200,
    "SuggestedDeposit": 2500,
    "CountryOfOrigin": "Thailand"
}')

-- Yamaha NMAX 155
INSERT INTO [Core].[VehicleModel] ([Json])
VALUES (N'{
    "Make": "Yamaha",
    "Model": "NMAX 155",
    "VehicleType": "Motorbike",
    "EngineCC": 155,
    "YearFrom": 2020,
    "IsActive": true,
    "DisplayOrder": 6,
    "Aliases": ["NMAX", "N-MAX", "NMAX155"],
    "SuggestedDailyRate": 400,
    "SuggestedDeposit": 5000,
    "CountryOfOrigin": "Thailand"
}')

-- Yamaha Aerox 155
INSERT INTO [Core].[VehicleModel] ([Json])
VALUES (N'{
    "Make": "Yamaha",
    "Model": "Aerox 155",
    "VehicleType": "Motorbike",
    "EngineCC": 155,
    "YearFrom": 2019,
    "IsActive": true,
    "DisplayOrder": 7,
    "Aliases": ["Aerox", "Aerox155"],
    "SuggestedDailyRate": 350,
    "SuggestedDeposit": 4000,
    "CountryOfOrigin": "Thailand"
}')

-- Yamaha Grand Filano
INSERT INTO [Core].[VehicleModel] ([Json])
VALUES (N'{
    "Make": "Yamaha",
    "Model": "Grand Filano",
    "VehicleType": "Motorbike",
    "EngineCC": 125,
    "YearFrom": 2018,
    "IsActive": true,
    "DisplayOrder": 8,
    "Aliases": ["Filano", "GrandFilano"],
    "SuggestedDailyRate": 300,
    "SuggestedDeposit": 3500,
    "CountryOfOrigin": "Thailand"
}')

-- Honda Forza 350
INSERT INTO [Core].[VehicleModel] ([Json])
VALUES (N'{
    "Make": "Honda",
    "Model": "Forza 350",
    "VehicleType": "Motorbike",
    "EngineCC": 350,
    "YearFrom": 2020,
    "IsActive": true,
    "DisplayOrder": 9,
    "Aliases": ["Forza", "Forza350"],
    "SuggestedDailyRate": 800,
    "SuggestedDeposit": 10000,
    "CountryOfOrigin": "Thailand"
}')

-- Honda ADV 160
INSERT INTO [Core].[VehicleModel] ([Json])
VALUES (N'{
    "Make": "Honda",
    "Model": "ADV 160",
    "VehicleType": "Motorbike",
    "EngineCC": 160,
    "YearFrom": 2022,
    "IsActive": true,
    "DisplayOrder": 10,
    "Aliases": ["ADV", "ADV160", "ADV 160"],
    "SuggestedDailyRate": 450,
    "SuggestedDeposit": 5000,
    "CountryOfOrigin": "Thailand"
}')

-- =============================================
-- CARS - Popular Thai Market Cars
-- =============================================

-- Toyota Vios
INSERT INTO [Core].[VehicleModel] ([Json])
VALUES (N'{
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
}')

-- Honda City
INSERT INTO [Core].[VehicleModel] ([Json])
VALUES (N'{
    "Make": "Honda",
    "Model": "City",
    "VehicleType": "Car",
    "Segment": "Sedan",
    "EngineLiters": 1.5,
    "SeatCount": 5,
    "YearFrom": 2019,
    "IsActive": true,
    "DisplayOrder": 21,
    "Aliases": ["City 1.5"],
    "SuggestedDailyRate": 1300,
    "SuggestedDeposit": 10000,
    "CountryOfOrigin": "Thailand"
}')

-- Toyota Yaris Ativ
INSERT INTO [Core].[VehicleModel] ([Json])
VALUES (N'{
    "Make": "Toyota",
    "Model": "Yaris Ativ",
    "VehicleType": "Car",
    "Segment": "Sedan",
    "EngineLiters": 1.2,
    "SeatCount": 5,
    "YearFrom": 2017,
    "IsActive": true,
    "DisplayOrder": 22,
    "Aliases": ["Yaris", "Ativ"],
    "SuggestedDailyRate": 1100,
    "SuggestedDeposit": 8000,
    "CountryOfOrigin": "Thailand"
}')

-- Toyota Fortuner
INSERT INTO [Core].[VehicleModel] ([Json])
VALUES (N'{
    "Make": "Toyota",
    "Model": "Fortuner",
    "VehicleType": "Car",
    "Segment": "SUV",
    "EngineLiters": 2.4,
    "SeatCount": 7,
    "YearFrom": 2015,
    "IsActive": true,
    "DisplayOrder": 23,
    "Aliases": ["Fortuner 2.4"],
    "SuggestedDailyRate": 2500,
    "SuggestedDeposit": 20000,
    "CountryOfOrigin": "Thailand"
}')

-- Honda CR-V
INSERT INTO [Core].[VehicleModel] ([Json])
VALUES (N'{
    "Make": "Honda",
    "Model": "CR-V",
    "VehicleType": "Car",
    "Segment": "SUV",
    "EngineLiters": 2.4,
    "SeatCount": 5,
    "YearFrom": 2017,
    "IsActive": true,
    "DisplayOrder": 24,
    "Aliases": ["CRV"],
    "SuggestedDailyRate": 2200,
    "SuggestedDeposit": 18000,
    "CountryOfOrigin": "Thailand"
}')

-- Toyota Camry
INSERT INTO [Core].[VehicleModel] ([Json])
VALUES (N'{
    "Make": "Toyota",
    "Model": "Camry",
    "VehicleType": "Car",
    "Segment": "Sedan",
    "EngineLiters": 2.5,
    "SeatCount": 5,
    "YearFrom": 2018,
    "IsActive": true,
    "DisplayOrder": 25,
    "Aliases": ["Camry 2.5"],
    "SuggestedDailyRate": 2800,
    "SuggestedDeposit": 25000,
    "CountryOfOrigin": "Thailand"
}')

-- =============================================
-- VANS - Popular Thai Market Vans
-- =============================================

-- Toyota Hiace
INSERT INTO [Core].[VehicleModel] ([Json])
VALUES (N'{
    "Make": "Toyota",
    "Model": "Hiace",
    "VehicleType": "Van",
    "SeatCount": 14,
    "YearFrom": 2015,
    "IsActive": true,
    "DisplayOrder": 30,
    "Aliases": ["Hiace Commuter"],
    "SuggestedDailyRate": 3500,
    "SuggestedDeposit": 30000,
    "CountryOfOrigin": "Japan"
}')

-- Toyota Commuter
INSERT INTO [Core].[VehicleModel] ([Json])
VALUES (N'{
    "Make": "Toyota",
    "Model": "Commuter",
    "VehicleType": "Van",
    "SeatCount": 12,
    "YearFrom": 2017,
    "IsActive": true,
    "DisplayOrder": 31,
    "Aliases": ["Commuter Van"],
    "SuggestedDailyRate": 3200,
    "SuggestedDeposit": 25000,
    "CountryOfOrigin": "Thailand"
}')

-- Hyundai H1
INSERT INTO [Core].[VehicleModel] ([Json])
VALUES (N'{
    "Make": "Hyundai",
    "Model": "H1",
    "VehicleType": "Van",
    "SeatCount": 11,
    "YearFrom": 2016,
    "IsActive": true,
    "DisplayOrder": 32,
    "Aliases": ["H-1", "Hyundai H-1", "Grand Starex"],
    "SuggestedDailyRate": 3000,
    "SuggestedDeposit": 25000,
    "CountryOfOrigin": "South Korea"
}')

-- Ford Transit
INSERT INTO [Core].[VehicleModel] ([Json])
VALUES (N'{
    "Make": "Ford",
    "Model": "Transit",
    "VehicleType": "Van",
    "SeatCount": 15,
    "YearFrom": 2018,
    "IsActive": true,
    "DisplayOrder": 33,
    "Aliases": ["Ford Transit Van"],
    "SuggestedDailyRate": 3500,
    "SuggestedDeposit": 30000,
    "CountryOfOrigin": "USA"
}')

-- Display results
SELECT VehicleModelId, Make, Model, VehicleType, EngineCC, IsActive, DisplayOrder
FROM [Core].[VehicleModel]
ORDER BY DisplayOrder
GO
