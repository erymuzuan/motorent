-- Setup test data in PhuketRentals schema for offline download testing
-- Run with: sqlcmd -S localhost -d MotoRent -E -i setup-test-data.sql -C

SET QUOTED_IDENTIFIER ON;
GO

SET IDENTITY_INSERT [PhuketRentals].[Shop] ON;
INSERT INTO [PhuketRentals].[Shop] ([ShopId], [Json], [ChangedBy], [CreatedBy], [ChangedTimestamp], [CreatedTimestamp])
VALUES (
    1,
    N'{"WebId":"shop-001-phuket","Name":"Phuket Bike Rentals","Phone":"076-123-4567","WhatsApp":"66761234567","LineId":"phuketbikes","Email":"info@phuketbikerentals.com","Address":"123 Patong Beach Road, Patong, Phuket 83150","GpsLocation":{"Lat":7.8919,"Lng":98.3004},"OperatingHours":"08:00-20:00","IsOpen":true}',
    'system',
    'system',
    SYSDATETIMEOFFSET(),
    SYSDATETIMEOFFSET()
);
SET IDENTITY_INSERT [PhuketRentals].[Shop] OFF;

SET IDENTITY_INSERT [PhuketRentals].[Renter] ON;
INSERT INTO [PhuketRentals].[Renter] ([RenterId], [Json], [ChangedBy], [CreatedBy], [ChangedTimestamp], [CreatedTimestamp])
VALUES (
    1,
    N'{"WebId":"renter-001","FullName":"Sarah Johnson","Email":"sarah@example.com","Phone":"081-234-5678","Nationality":"Australian","PassportNo":"PA12345678"}',
    'system',
    'system',
    SYSDATETIMEOFFSET(),
    SYSDATETIMEOFFSET()
);
SET IDENTITY_INSERT [PhuketRentals].[Renter] OFF;

SET IDENTITY_INSERT [PhuketRentals].[Vehicle] ON;
INSERT INTO [PhuketRentals].[Vehicle] ([VehicleId], [Json], [ChangedBy], [CreatedBy], [ChangedTimestamp], [CreatedTimestamp])
VALUES (
    1,
    N'{"WebId":"vehicle-001","Brand":"Honda","Model":"Click 125","LicensePlate":"กก 1234","Status":"Rented","DailyRate":350.00,"Color":"Blue","EngineCC":125}',
    'system',
    'system',
    SYSDATETIMEOFFSET(),
    SYSDATETIMEOFFSET()
);
SET IDENTITY_INSERT [PhuketRentals].[Vehicle] OFF;

SET IDENTITY_INSERT [PhuketRentals].[Rental] ON;
INSERT INTO [PhuketRentals].[Rental] ([RentalId], [Json], [ChangedBy], [CreatedBy], [ChangedTimestamp], [CreatedTimestamp])
VALUES (
    1,
    N'{"WebId":"test-rental-offline-001","RenterId":1,"RenterName":"Sarah Johnson","VehicleId":1,"VehicleName":"Honda Click 125","RentedFromShopId":1,"Status":"Active","DurationType":"Daily","RentalRate":350.00,"TotalAmount":1050.00,"StartDate":"2026-01-17T09:00:00+07:00","ExpectedEndDate":"2026-01-20T09:00:00+07:00","Notes":"Test rental for offline download testing","MileageStart":15234}',
    'system',
    'system',
    SYSDATETIMEOFFSET(),
    SYSDATETIMEOFFSET()
);
SET IDENTITY_INSERT [PhuketRentals].[Rental] OFF;

SELECT 'Test data created successfully!' as Result;
SELECT 'Test Rental WebId: test-rental-offline-001' as [Info];
SELECT 'Full URL: /tourist/PhuketRentals/my-rental/test-rental-offline-001' as [TestUrl];
