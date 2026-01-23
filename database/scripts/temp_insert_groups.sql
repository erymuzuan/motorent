SET QUOTED_IDENTIFIER ON;
GO

-- Insert EUR All Notes
INSERT INTO [KrabiBeachRentals].[DenominationGroup] ([Json], [CreatedBy], [ChangedBy], [CreatedTimestamp], [ChangedTimestamp])
VALUES ('{"DenominationGroupId":0,"Currency":"EUR","GroupName":"All Notes","Denominations":[500,200,100,50,20,10,5],"SortOrder":3,"IsActive":true}', 'system', 'system', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());

-- Insert GBP All Notes
INSERT INTO [KrabiBeachRentals].[DenominationGroup] ([Json], [CreatedBy], [ChangedBy], [CreatedTimestamp], [ChangedTimestamp])
VALUES ('{"DenominationGroupId":0,"Currency":"GBP","GroupName":"All Notes","Denominations":[50,20,10,5],"SortOrder":4,"IsActive":true}', 'system', 'system', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());

-- Insert JPY All Notes
INSERT INTO [KrabiBeachRentals].[DenominationGroup] ([Json], [CreatedBy], [ChangedBy], [CreatedTimestamp], [ChangedTimestamp])
VALUES ('{"DenominationGroupId":0,"Currency":"JPY","GroupName":"All Notes","Denominations":[10000,5000,1000],"SortOrder":5,"IsActive":true}', 'system', 'system', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());

-- Insert CNY All Notes
INSERT INTO [KrabiBeachRentals].[DenominationGroup] ([Json], [CreatedBy], [ChangedBy], [CreatedTimestamp], [ChangedTimestamp])
VALUES ('{"DenominationGroupId":0,"Currency":"CNY","GroupName":"All Notes","Denominations":[100,50,20,10,5,1],"SortOrder":6,"IsActive":true}', 'system', 'system', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());

-- Insert AUD All Notes
INSERT INTO [KrabiBeachRentals].[DenominationGroup] ([Json], [CreatedBy], [ChangedBy], [CreatedTimestamp], [ChangedTimestamp])
VALUES ('{"DenominationGroupId":0,"Currency":"AUD","GroupName":"All Notes","Denominations":[100,50,20,10,5],"SortOrder":7,"IsActive":true}', 'system', 'system', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());

-- Insert RUB All Notes
INSERT INTO [KrabiBeachRentals].[DenominationGroup] ([Json], [CreatedBy], [ChangedBy], [CreatedTimestamp], [ChangedTimestamp])
VALUES ('{"DenominationGroupId":0,"Currency":"RUB","GroupName":"All Notes","Denominations":[5000,2000,1000,500,200,100],"SortOrder":8,"IsActive":true}', 'system', 'system', SYSDATETIMEOFFSET(), SYSDATETIMEOFFSET());

SELECT COUNT(*) as TotalGroups FROM [KrabiBeachRentals].[DenominationGroup];
