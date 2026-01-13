-- Seed Vehicle Lookups with Popular Thai Market Vehicles
-- This script populates the Core.VehicleModel table with common rental vehicles

-- Clear existing data (optional - comment out if you want to append)
-- DELETE FROM [Core].[VehicleModel];

-- ============================================================
-- MOTORBIKES - Popular Thai Rental Scooters and Motorcycles
-- ============================================================

-- Honda Motorbikes
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Honda","Model":"Click 125","VehicleType":"Motorbike","EngineCC":125,"YearFrom":2020,"IsActive":true,"DisplayOrder":1,"Aliases":["Click","Click125","Click 125i"],"SuggestedDailyRate":300,"SuggestedDeposit":2000,"CountryOfOrigin":"Thailand"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Honda","Model":"PCX 160","VehicleType":"Motorbike","EngineCC":160,"YearFrom":2021,"IsActive":true,"DisplayOrder":2,"Aliases":["PCX","PCX160","PCX 160cc"],"SuggestedDailyRate":400,"SuggestedDeposit":3000,"CountryOfOrigin":"Thailand"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Honda","Model":"Wave 110","VehicleType":"Motorbike","EngineCC":110,"YearFrom":2015,"IsActive":true,"DisplayOrder":3,"Aliases":["Wave","Wave110","Wave 110i"],"SuggestedDailyRate":250,"SuggestedDeposit":1500,"CountryOfOrigin":"Thailand"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Honda","Model":"Scoopy","VehicleType":"Motorbike","EngineCC":110,"YearFrom":2018,"IsActive":true,"DisplayOrder":4,"Aliases":["Scoopy i","Scoopy Club"],"SuggestedDailyRate":280,"SuggestedDeposit":2000,"CountryOfOrigin":"Thailand"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Honda","Model":"ADV 150","VehicleType":"Motorbike","EngineCC":150,"YearFrom":2020,"IsActive":true,"DisplayOrder":5,"Aliases":["ADV","ADV150"],"SuggestedDailyRate":450,"SuggestedDeposit":3500,"CountryOfOrigin":"Thailand"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Honda","Model":"Forza 350","VehicleType":"Motorbike","EngineCC":350,"YearFrom":2020,"IsActive":true,"DisplayOrder":6,"Aliases":["Forza","Forza350"],"SuggestedDailyRate":600,"SuggestedDeposit":5000,"CountryOfOrigin":"Thailand"}');

-- Yamaha Motorbikes
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Yamaha","Model":"NMAX 155","VehicleType":"Motorbike","EngineCC":155,"YearFrom":2020,"IsActive":true,"DisplayOrder":10,"Aliases":["NMAX","NMAX155","N-Max"],"SuggestedDailyRate":400,"SuggestedDeposit":3000,"CountryOfOrigin":"Thailand"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Yamaha","Model":"Aerox 155","VehicleType":"Motorbike","EngineCC":155,"YearFrom":2020,"IsActive":true,"DisplayOrder":11,"Aliases":["Aerox","Aerox155"],"SuggestedDailyRate":380,"SuggestedDeposit":2500,"CountryOfOrigin":"Thailand"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Yamaha","Model":"Fino","VehicleType":"Motorbike","EngineCC":125,"YearFrom":2018,"IsActive":true,"DisplayOrder":12,"Aliases":["Fino125","Fino 125"],"SuggestedDailyRate":280,"SuggestedDeposit":2000,"CountryOfOrigin":"Thailand"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Yamaha","Model":"Grand Filano","VehicleType":"Motorbike","EngineCC":125,"YearFrom":2020,"IsActive":true,"DisplayOrder":13,"Aliases":["Filano","Grand Filano Hybrid"],"SuggestedDailyRate":350,"SuggestedDeposit":2500,"CountryOfOrigin":"Thailand"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Yamaha","Model":"XMAX 300","VehicleType":"Motorbike","EngineCC":300,"YearFrom":2020,"IsActive":true,"DisplayOrder":14,"Aliases":["XMAX","XMAX300","X-Max"],"SuggestedDailyRate":550,"SuggestedDeposit":5000,"CountryOfOrigin":"Thailand"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Yamaha","Model":"Mio","VehicleType":"Motorbike","EngineCC":125,"YearFrom":2015,"IsActive":true,"DisplayOrder":15,"Aliases":["Mio125","Mio 125"],"SuggestedDailyRate":250,"SuggestedDeposit":1500,"CountryOfOrigin":"Thailand"}');

-- Vespa
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Vespa","Model":"Primavera 150","VehicleType":"Motorbike","EngineCC":150,"YearFrom":2019,"IsActive":true,"DisplayOrder":20,"Aliases":["Primavera","Primavera150"],"SuggestedDailyRate":500,"SuggestedDeposit":5000,"CountryOfOrigin":"Italy"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Vespa","Model":"Sprint 150","VehicleType":"Motorbike","EngineCC":150,"YearFrom":2019,"IsActive":true,"DisplayOrder":21,"Aliases":["Sprint","Sprint150"],"SuggestedDailyRate":500,"SuggestedDeposit":5000,"CountryOfOrigin":"Italy"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Vespa","Model":"GTS 300","VehicleType":"Motorbike","EngineCC":300,"YearFrom":2020,"IsActive":true,"DisplayOrder":22,"Aliases":["GTS","GTS300"],"SuggestedDailyRate":700,"SuggestedDeposit":7000,"CountryOfOrigin":"Italy"}');

-- GPX
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"GPX","Model":"Demon 150","VehicleType":"Motorbike","EngineCC":150,"YearFrom":2020,"IsActive":true,"DisplayOrder":25,"Aliases":["Demon","Demon150"],"SuggestedDailyRate":350,"SuggestedDeposit":3000,"CountryOfOrigin":"Thailand"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"GPX","Model":"Legend 150","VehicleType":"Motorbike","EngineCC":150,"YearFrom":2021,"IsActive":true,"DisplayOrder":26,"Aliases":["Legend","Legend150"],"SuggestedDailyRate":380,"SuggestedDeposit":3000,"CountryOfOrigin":"Thailand"}');

-- ============================================================
-- CARS - Popular Thai Rental Cars
-- ============================================================

-- Toyota Cars
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Toyota","Model":"Yaris","VehicleType":"Car","Segment":"Hatchback","EngineLiters":1.2,"SeatCount":5,"YearFrom":2018,"IsActive":true,"DisplayOrder":30,"Aliases":["Yaris Ativ","Yaris Eco"],"SuggestedDailyRate":1000,"SuggestedDeposit":5000,"CountryOfOrigin":"Japan"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Toyota","Model":"Vios","VehicleType":"Car","Segment":"SmallSedan","EngineLiters":1.5,"SeatCount":5,"YearFrom":2018,"IsActive":true,"DisplayOrder":31,"Aliases":["Vios E","Vios J"],"SuggestedDailyRate":1200,"SuggestedDeposit":7000,"CountryOfOrigin":"Japan"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Toyota","Model":"Camry","VehicleType":"Car","Segment":"BigSedan","EngineLiters":2.5,"SeatCount":5,"YearFrom":2019,"IsActive":true,"DisplayOrder":32,"Aliases":["Camry Hybrid"],"SuggestedDailyRate":2500,"SuggestedDeposit":15000,"CountryOfOrigin":"Japan"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Toyota","Model":"Fortuner","VehicleType":"Car","Segment":"SUV","EngineLiters":2.8,"SeatCount":7,"YearFrom":2020,"IsActive":true,"DisplayOrder":33,"Aliases":["Fortuner Legender"],"SuggestedDailyRate":2800,"SuggestedDeposit":15000,"CountryOfOrigin":"Thailand"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Toyota","Model":"Innova","VehicleType":"Car","Segment":"Minivan","EngineLiters":2.0,"SeatCount":7,"YearFrom":2019,"IsActive":true,"DisplayOrder":34,"Aliases":["Innova Crysta"],"SuggestedDailyRate":1800,"SuggestedDeposit":10000,"CountryOfOrigin":"Thailand"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Toyota","Model":"Hilux Revo","VehicleType":"Car","Segment":"Pickup","EngineLiters":2.4,"SeatCount":5,"YearFrom":2020,"IsActive":true,"DisplayOrder":35,"Aliases":["Hilux","Revo","Hilux Revo Rocco"],"SuggestedDailyRate":1500,"SuggestedDeposit":10000,"CountryOfOrigin":"Thailand"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Toyota","Model":"CHR","VehicleType":"Car","Segment":"SUV","EngineLiters":1.8,"SeatCount":5,"YearFrom":2019,"IsActive":true,"DisplayOrder":36,"Aliases":["C-HR","CHR Hybrid"],"SuggestedDailyRate":2000,"SuggestedDeposit":12000,"CountryOfOrigin":"Japan"}');

-- Honda Cars
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Honda","Model":"City","VehicleType":"Car","Segment":"SmallSedan","EngineLiters":1.5,"SeatCount":5,"YearFrom":2020,"IsActive":true,"DisplayOrder":40,"Aliases":["City RS","City e:HEV"],"SuggestedDailyRate":1300,"SuggestedDeposit":8000,"CountryOfOrigin":"Thailand"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Honda","Model":"Civic","VehicleType":"Car","Segment":"BigSedan","EngineLiters":1.5,"SeatCount":5,"YearFrom":2021,"IsActive":true,"DisplayOrder":41,"Aliases":["Civic RS","Civic Turbo"],"SuggestedDailyRate":2000,"SuggestedDeposit":12000,"CountryOfOrigin":"Thailand"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Honda","Model":"HR-V","VehicleType":"Car","Segment":"SUV","EngineLiters":1.5,"SeatCount":5,"YearFrom":2021,"IsActive":true,"DisplayOrder":42,"Aliases":["HRV","HR-V e:HEV"],"SuggestedDailyRate":1800,"SuggestedDeposit":10000,"CountryOfOrigin":"Thailand"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Honda","Model":"CR-V","VehicleType":"Car","Segment":"SUV","EngineLiters":2.4,"SeatCount":7,"YearFrom":2020,"IsActive":true,"DisplayOrder":43,"Aliases":["CRV","CR-V e:HEV"],"SuggestedDailyRate":2500,"SuggestedDeposit":15000,"CountryOfOrigin":"Thailand"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Honda","Model":"Accord","VehicleType":"Car","Segment":"BigSedan","EngineLiters":1.5,"SeatCount":5,"YearFrom":2020,"IsActive":true,"DisplayOrder":44,"Aliases":["Accord Turbo","Accord e:HEV"],"SuggestedDailyRate":2800,"SuggestedDeposit":15000,"CountryOfOrigin":"Thailand"}');

-- Mazda Cars
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Mazda","Model":"Mazda 2","VehicleType":"Car","Segment":"Hatchback","EngineLiters":1.3,"SeatCount":5,"YearFrom":2019,"IsActive":true,"DisplayOrder":50,"Aliases":["Mazda2","2 Hatchback"],"SuggestedDailyRate":1100,"SuggestedDeposit":6000,"CountryOfOrigin":"Japan"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Mazda","Model":"Mazda 3","VehicleType":"Car","Segment":"BigSedan","EngineLiters":2.0,"SeatCount":5,"YearFrom":2020,"IsActive":true,"DisplayOrder":51,"Aliases":["Mazda3","3 Fastback"],"SuggestedDailyRate":1800,"SuggestedDeposit":10000,"CountryOfOrigin":"Japan"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Mazda","Model":"CX-5","VehicleType":"Car","Segment":"SUV","EngineLiters":2.5,"SeatCount":5,"YearFrom":2020,"IsActive":true,"DisplayOrder":52,"Aliases":["CX5","CX-5 Turbo"],"SuggestedDailyRate":2200,"SuggestedDeposit":12000,"CountryOfOrigin":"Japan"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Mazda","Model":"CX-30","VehicleType":"Car","Segment":"SUV","EngineLiters":2.0,"SeatCount":5,"YearFrom":2020,"IsActive":true,"DisplayOrder":53,"Aliases":["CX30"],"SuggestedDailyRate":1800,"SuggestedDeposit":10000,"CountryOfOrigin":"Japan"}');

-- Nissan Cars
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Nissan","Model":"March","VehicleType":"Car","Segment":"Hatchback","EngineLiters":1.2,"SeatCount":5,"YearFrom":2017,"IsActive":true,"DisplayOrder":55,"Aliases":["March E","Micra"],"SuggestedDailyRate":900,"SuggestedDeposit":5000,"CountryOfOrigin":"Thailand"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Nissan","Model":"Almera","VehicleType":"Car","Segment":"SmallSedan","EngineLiters":1.0,"SeatCount":5,"YearFrom":2020,"IsActive":true,"DisplayOrder":56,"Aliases":["Almera Turbo"],"SuggestedDailyRate":1000,"SuggestedDeposit":6000,"CountryOfOrigin":"Thailand"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Nissan","Model":"Kicks","VehicleType":"Car","Segment":"SUV","EngineLiters":1.2,"SeatCount":5,"YearFrom":2021,"IsActive":true,"DisplayOrder":57,"Aliases":["Kicks e-Power"],"SuggestedDailyRate":1500,"SuggestedDeposit":8000,"CountryOfOrigin":"Thailand"}');

-- ============================================================
-- VANS - Popular Thai Rental Vans
-- ============================================================

INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Toyota","Model":"HiAce Commuter","VehicleType":"Van","SeatCount":12,"YearFrom":2019,"IsActive":true,"DisplayOrder":70,"Aliases":["HiAce","Commuter","Hiace 12 seats"],"SuggestedDailyRate":2500,"SuggestedDeposit":15000,"CountryOfOrigin":"Japan"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Toyota","Model":"HiAce VIP","VehicleType":"Van","SeatCount":10,"YearFrom":2020,"IsActive":true,"DisplayOrder":71,"Aliases":["Hiace VIP","VIP Van"],"SuggestedDailyRate":3500,"SuggestedDeposit":20000,"CountryOfOrigin":"Japan"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Hyundai","Model":"H1","VehicleType":"Van","SeatCount":11,"YearFrom":2018,"IsActive":true,"DisplayOrder":72,"Aliases":["H-1","Grand Starex","Starex"],"SuggestedDailyRate":2200,"SuggestedDeposit":12000,"CountryOfOrigin":"South Korea"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Hyundai","Model":"Staria","VehicleType":"Van","SeatCount":11,"YearFrom":2022,"IsActive":true,"DisplayOrder":73,"Aliases":["Staria Premium","Staria Tourer"],"SuggestedDailyRate":3000,"SuggestedDeposit":15000,"CountryOfOrigin":"South Korea"}');

-- ============================================================
-- JET SKI - Popular Rental Jet Skis
-- ============================================================

INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Yamaha","Model":"VX Cruiser","VehicleType":"JetSki","YearFrom":2020,"IsActive":true,"DisplayOrder":80,"Aliases":["VX","VX Cruiser HO"],"SuggestedDailyRate":5000,"SuggestedDeposit":10000,"CountryOfOrigin":"Japan"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Yamaha","Model":"GP1800R","VehicleType":"JetSki","YearFrom":2021,"IsActive":true,"DisplayOrder":81,"Aliases":["GP1800","GP1800R SVHO"],"SuggestedDailyRate":7000,"SuggestedDeposit":15000,"CountryOfOrigin":"Japan"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Sea-Doo","Model":"GTI 130","VehicleType":"JetSki","YearFrom":2020,"IsActive":true,"DisplayOrder":82,"Aliases":["GTI","GTI SE"],"SuggestedDailyRate":5500,"SuggestedDeposit":12000,"CountryOfOrigin":"Canada"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Sea-Doo","Model":"RXP-X 300","VehicleType":"JetSki","YearFrom":2021,"IsActive":true,"DisplayOrder":83,"Aliases":["RXP-X","RXP"],"SuggestedDailyRate":8000,"SuggestedDeposit":20000,"CountryOfOrigin":"Canada"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Kawasaki","Model":"Ultra 310LX","VehicleType":"JetSki","YearFrom":2020,"IsActive":true,"DisplayOrder":84,"Aliases":["Ultra","Ultra LX"],"SuggestedDailyRate":6500,"SuggestedDeposit":15000,"CountryOfOrigin":"Japan"}');

-- ============================================================
-- BOATS - Common Rental Boats (Less standardized)
-- ============================================================

INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Yamaha","Model":"Center Console 23ft","VehicleType":"Boat","SeatCount":8,"YearFrom":2019,"IsActive":true,"DisplayOrder":90,"Aliases":["Center Console","CC 23"],"SuggestedDailyRate":8000,"SuggestedDeposit":20000,"CountryOfOrigin":"Japan"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Local","Model":"Longtail Boat","VehicleType":"Boat","SeatCount":10,"YearFrom":2015,"IsActive":true,"DisplayOrder":91,"Aliases":["Long Tail","Thai Boat"],"SuggestedDailyRate":2000,"SuggestedDeposit":5000,"CountryOfOrigin":"Thailand"}');
INSERT INTO [Core].[VehicleModel] ([Json]) VALUES (N'{"$type":"VehicleModel","Make":"Local","Model":"Speedboat 10 Pax","VehicleType":"Boat","SeatCount":10,"YearFrom":2018,"IsActive":true,"DisplayOrder":92,"Aliases":["Speedboat","Speed Boat 10"],"SuggestedDailyRate":6000,"SuggestedDeposit":15000,"CountryOfOrigin":"Thailand"}');

PRINT 'Vehicle lookups seed data inserted successfully';
GO
