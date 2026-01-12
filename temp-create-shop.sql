SET IDENTITY_INSERT [KrabiBeachRentals].[Shop] ON;
INSERT INTO [KrabiBeachRentals].[Shop] (ShopId, Json, CreatedBy, ChangedBy, CreatedTimestamp, ChangedTimestamp)
VALUES (1, '{"ShopId":1,"Name":"Krabi Beach Shop","Address":{"Street":"123 Beach Road","City":"Krabi","Province":"Krabi","PostalCode":"81000","Country":"Thailand"},"Phone":"+66 75 123 4567","Email":"info@krabibeachrentals.com","IsActive":true}', 'system', 'system', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());
SET IDENTITY_INSERT [KrabiBeachRentals].[Shop] OFF;
PRINT 'Shop created successfully';
