-- Fix Shop Address from object to string
UPDATE [KrabiBeachRentals].[Shop]
SET [Json] = N'{"ShopId":9,"Name":"Krabi Beach Shop","Location":"Krabi","Address":"123 Beach Road, Ao Nang, Krabi 81000, Thailand","Phone":"+66 75 123 4567","Email":"info@krabibeachrentals.com","IsActive":true,"GpsLocation":{"Lat":8.0308,"Lng":98.8263}}'
WHERE [Name] = 'Krabi Beach Shop'

SELECT [ShopId], [Name], [Json] FROM [KrabiBeachRentals].[Shop] WHERE [Name] = 'Krabi Beach Shop'
