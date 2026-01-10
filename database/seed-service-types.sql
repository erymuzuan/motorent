-- Insert default service types for shop 1
INSERT INTO [MotoRent].[ServiceType] ([Json], [CreatedBy], [ChangedBy], [CreatedTimestamp], [ChangedTimestamp])
VALUES
('{"ShopId":1,"Name":"Oil Change","Description":"Regular engine oil change","DaysInterval":30,"KmInterval":3000,"SortOrder":1,"IsActive":true}', 'system', 'system', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
('{"ShopId":1,"Name":"Brake Check","Description":"Brake pad and fluid inspection","DaysInterval":60,"KmInterval":5000,"SortOrder":2,"IsActive":true}', 'system', 'system', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
('{"ShopId":1,"Name":"Tire Inspection","Description":"Tire wear and pressure check","DaysInterval":90,"KmInterval":8000,"SortOrder":3,"IsActive":true}', 'system', 'system', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET()),
('{"ShopId":1,"Name":"General Service","Description":"Full maintenance service","DaysInterval":180,"KmInterval":15000,"SortOrder":4,"IsActive":true}', 'system', 'system', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());

-- Verify insertion
SELECT ServiceTypeId, Name, DaysInterval, KmInterval FROM [MotoRent].[ServiceType];
