-- MotoRent Seed Data
-- Sample data for development and testing

-- Insert sample shop
INSERT INTO [MotoRent].[Shop] ([Json])
VALUES (N'{
    "Name": "Phuket Bike Rentals",
    "Location": "Phuket",
    "Address": "123 Beach Road, Patong, Phuket 83150",
    "Phone": "076-123-456",
    "Email": "info@phuketbikerentals.com",
    "TermsAndConditions": "Standard rental terms apply. Helmet required. Valid license required.",
    "IsActive": true
}')
GO

DECLARE @ShopId INT = SCOPE_IDENTITY()

-- Insert sample insurance packages
INSERT INTO [MotoRent].[Insurance] ([Json])
VALUES
(N'{"ShopId": ' + CAST(@ShopId AS VARCHAR) + N', "Name": "Basic", "Description": "Basic coverage for minor damages", "DailyRate": 50, "MaxCoverage": 5000, "Deductible": 1000, "IsActive": true}'),
(N'{"ShopId": ' + CAST(@ShopId AS VARCHAR) + N', "Name": "Premium", "Description": "Full coverage including theft", "DailyRate": 150, "MaxCoverage": 20000, "Deductible": 500, "IsActive": true}'),
(N'{"ShopId": ' + CAST(@ShopId AS VARCHAR) + N', "Name": "Full Coverage", "Description": "Complete protection with zero deductible", "DailyRate": 250, "MaxCoverage": 50000, "Deductible": 0, "IsActive": true}')
GO

-- Insert sample accessories
INSERT INTO [MotoRent].[Accessory] ([Json])
SELECT N'{"ShopId": ' + CAST(ShopId AS VARCHAR) + N', "Name": "Helmet (Full Face)", "DailyRate": 0, "QuantityAvailable": 20, "IsIncluded": true}'
FROM [MotoRent].[Shop] WHERE [Name] = 'Phuket Bike Rentals'
UNION ALL
SELECT N'{"ShopId": ' + CAST(ShopId AS VARCHAR) + N', "Name": "Helmet (Half Face)", "DailyRate": 0, "QuantityAvailable": 30, "IsIncluded": true}'
FROM [MotoRent].[Shop] WHERE [Name] = 'Phuket Bike Rentals'
UNION ALL
SELECT N'{"ShopId": ' + CAST(ShopId AS VARCHAR) + N', "Name": "Phone Holder", "DailyRate": 20, "QuantityAvailable": 15, "IsIncluded": false}'
FROM [MotoRent].[Shop] WHERE [Name] = 'Phuket Bike Rentals'
UNION ALL
SELECT N'{"ShopId": ' + CAST(ShopId AS VARCHAR) + N', "Name": "Rain Poncho", "DailyRate": 30, "QuantityAvailable": 25, "IsIncluded": false}'
FROM [MotoRent].[Shop] WHERE [Name] = 'Phuket Bike Rentals'
UNION ALL
SELECT N'{"ShopId": ' + CAST(ShopId AS VARCHAR) + N', "Name": "Luggage Box", "DailyRate": 50, "QuantityAvailable": 10, "IsIncluded": false}'
FROM [MotoRent].[Shop] WHERE [Name] = 'Phuket Bike Rentals'
GO

-- Insert sample motorbikes
INSERT INTO [MotoRent].[Motorbike] ([Json])
SELECT N'{"ShopId": ' + CAST(ShopId AS VARCHAR) + N', "LicensePlate": "1กข 1234", "Brand": "Honda", "Model": "Click 125", "EngineCC": 125, "Color": "White", "Year": 2023, "Status": "Available", "DailyRate": 250, "DepositAmount": 3000, "Mileage": 5420}'
FROM [MotoRent].[Shop] WHERE [Name] = 'Phuket Bike Rentals'
UNION ALL
SELECT N'{"ShopId": ' + CAST(ShopId AS VARCHAR) + N', "LicensePlate": "1กข 2345", "Brand": "Honda", "Model": "Click 125", "EngineCC": 125, "Color": "Red", "Year": 2023, "Status": "Available", "DailyRate": 250, "DepositAmount": 3000, "Mileage": 3210}'
FROM [MotoRent].[Shop] WHERE [Name] = 'Phuket Bike Rentals'
UNION ALL
SELECT N'{"ShopId": ' + CAST(ShopId AS VARCHAR) + N', "LicensePlate": "1กข 3456", "Brand": "Honda", "Model": "PCX 160", "EngineCC": 160, "Color": "Black", "Year": 2024, "Status": "Available", "DailyRate": 400, "DepositAmount": 5000, "Mileage": 1520}'
FROM [MotoRent].[Shop] WHERE [Name] = 'Phuket Bike Rentals'
UNION ALL
SELECT N'{"ShopId": ' + CAST(ShopId AS VARCHAR) + N', "LicensePlate": "1กข 4567", "Brand": "Yamaha", "Model": "NMAX 155", "EngineCC": 155, "Color": "Blue", "Year": 2023, "Status": "Available", "DailyRate": 450, "DepositAmount": 5000, "Mileage": 8750}'
FROM [MotoRent].[Shop] WHERE [Name] = 'Phuket Bike Rentals'
UNION ALL
SELECT N'{"ShopId": ' + CAST(ShopId AS VARCHAR) + N', "LicensePlate": "1กข 5678", "Brand": "Yamaha", "Model": "Aerox 155", "EngineCC": 155, "Color": "Yellow", "Year": 2024, "Status": "Available", "DailyRate": 500, "DepositAmount": 5000, "Mileage": 2100}'
FROM [MotoRent].[Shop] WHERE [Name] = 'Phuket Bike Rentals'
UNION ALL
SELECT N'{"ShopId": ' + CAST(ShopId AS VARCHAR) + N', "LicensePlate": "1กข 6789", "Brand": "Honda", "Model": "Wave 110", "EngineCC": 110, "Color": "Black", "Year": 2022, "Status": "Available", "DailyRate": 200, "DepositAmount": 2000, "Mileage": 15200}'
FROM [MotoRent].[Shop] WHERE [Name] = 'Phuket Bike Rentals'
UNION ALL
SELECT N'{"ShopId": ' + CAST(ShopId AS VARCHAR) + N', "LicensePlate": "1กข 7890", "Brand": "Honda", "Model": "Scoopy", "EngineCC": 110, "Color": "Pink", "Year": 2023, "Status": "Maintenance", "DailyRate": 220, "DepositAmount": 2500, "Mileage": 9800, "Notes": "Brake pads replacement"}'
FROM [MotoRent].[Shop] WHERE [Name] = 'Phuket Bike Rentals'
GO

-- Insert sample renters
INSERT INTO [MotoRent].[Renter] ([Json])
SELECT N'{"ShopId": ' + CAST(ShopId AS VARCHAR) + N', "FullName": "John Smith", "Nationality": "USA", "PassportNo": "US12345678", "DrivingLicenseNo": "DL-123456", "DrivingLicenseCountry": "USA", "Phone": "+1-555-123-4567", "Email": "john.smith@email.com", "HotelName": "Patong Beach Hotel", "HotelAddress": "456 Beach Road, Patong"}'
FROM [MotoRent].[Shop] WHERE [Name] = 'Phuket Bike Rentals'
UNION ALL
SELECT N'{"ShopId": ' + CAST(ShopId AS VARCHAR) + N', "FullName": "Maria Garcia", "Nationality": "Spain", "PassportNo": "ESP98765432", "DrivingLicenseNo": "ES-654321", "DrivingLicenseCountry": "Spain", "Phone": "+34-600-123-456", "Email": "maria.garcia@email.com", "HotelName": "Kata Beach Resort"}'
FROM [MotoRent].[Shop] WHERE [Name] = 'Phuket Bike Rentals'
UNION ALL
SELECT N'{"ShopId": ' + CAST(ShopId AS VARCHAR) + N', "FullName": "James Wilson", "Nationality": "UK", "PassportNo": "GB11223344", "DrivingLicenseNo": "UK-112233", "DrivingLicenseCountry": "UK", "Phone": "+44-7700-900123", "Email": "james.wilson@email.com"}'
FROM [MotoRent].[Shop] WHERE [Name] = 'Phuket Bike Rentals'
GO

PRINT 'Seed data inserted successfully'
GO
