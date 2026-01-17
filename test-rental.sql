-- Create test rental for offline download testing
INSERT INTO [MotoRent].[Rental] ([Json], [ChangedBy], [CreatedBy], [ChangedTimestamp], [CreatedTimestamp])
VALUES (
    N'{"WebId":"550e8400-e29b-41d4-a716-446655440001","RenterId":1,"RenterName":"John Test Smith","VehicleId":1,"VehicleName":"Honda Click 125","RentedFromShopId":1,"Status":"Active","DurationType":"Daily","RentalRate":350.00,"TotalAmount":700.00,"StartDate":"2026-01-17T09:00:00+07:00","ExpectedEndDate":"2026-01-19T09:00:00+07:00","Notes":"Test rental for offline download testing"}',
    'system',
    'system',
    SYSDATETIMEOFFSET(),
    SYSDATETIMEOFFSET()
);

SELECT SCOPE_IDENTITY() as NewRentalId;
