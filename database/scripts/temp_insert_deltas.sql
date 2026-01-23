SET QUOTED_IDENTIFIER ON;
GO

-- Insert deltas for EUR (Group 3) - delta -0.08
INSERT INTO [KrabiBeachRentals].[RateDelta] ([Json], [CreatedBy], [ChangedBy], [CreatedTimestamp], [ChangedTimestamp])
VALUES ('{"RateDeltaId":0,"ShopId":null,"Currency":"EUR","DenominationGroupId":3,"BuyDelta":-0.08,"SellDelta":0,"IsActive":true}', 'system', 'system', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());

-- Insert deltas for GBP (Group 4) - delta -0.12
INSERT INTO [KrabiBeachRentals].[RateDelta] ([Json], [CreatedBy], [ChangedBy], [CreatedTimestamp], [ChangedTimestamp])
VALUES ('{"RateDeltaId":0,"ShopId":null,"Currency":"GBP","DenominationGroupId":4,"BuyDelta":-0.12,"SellDelta":0,"IsActive":true}', 'system', 'system', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());

-- Insert deltas for JPY (Group 5) - delta -0.002
INSERT INTO [KrabiBeachRentals].[RateDelta] ([Json], [CreatedBy], [ChangedBy], [CreatedTimestamp], [ChangedTimestamp])
VALUES ('{"RateDeltaId":0,"ShopId":null,"Currency":"JPY","DenominationGroupId":5,"BuyDelta":-0.002,"SellDelta":0,"IsActive":true}', 'system', 'system', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());

-- Insert deltas for CNY (Group 6) - delta -0.04
INSERT INTO [KrabiBeachRentals].[RateDelta] ([Json], [CreatedBy], [ChangedBy], [CreatedTimestamp], [ChangedTimestamp])
VALUES ('{"RateDeltaId":0,"ShopId":null,"Currency":"CNY","DenominationGroupId":6,"BuyDelta":-0.04,"SellDelta":0,"IsActive":true}', 'system', 'system', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());

-- Insert deltas for AUD (Group 7) - delta -0.06
INSERT INTO [KrabiBeachRentals].[RateDelta] ([Json], [CreatedBy], [ChangedBy], [CreatedTimestamp], [ChangedTimestamp])
VALUES ('{"RateDeltaId":0,"ShopId":null,"Currency":"AUD","DenominationGroupId":7,"BuyDelta":-0.06,"SellDelta":0,"IsActive":true}', 'system', 'system', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());

-- Insert deltas for RUB (Group 8) - delta -0.01
INSERT INTO [KrabiBeachRentals].[RateDelta] ([Json], [CreatedBy], [ChangedBy], [CreatedTimestamp], [ChangedTimestamp])
VALUES ('{"RateDeltaId":0,"ShopId":null,"Currency":"RUB","DenominationGroupId":8,"BuyDelta":-0.01,"SellDelta":0,"IsActive":true}', 'system', 'system', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());

SELECT RateDeltaId, Currency, DenominationGroupId, JSON_VALUE(Json, '$.BuyDelta') as BuyDelta FROM [KrabiBeachRentals].[RateDelta] ORDER BY RateDeltaId;
