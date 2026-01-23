-- Default denomination groups seed data
-- Run after creating DenominationGroup table

-- USD Large Bills (100, 50) - typically get better rates
INSERT INTO [<schema>].[DenominationGroup] ([Json])
VALUES (N'{"$type":"DenominationGroup","Currency":"USD","GroupName":"Large Bills","Denominations":[100,50],"SortOrder":1,"IsActive":true}')

-- USD Small Bills (20, 10, 5, 1) - standard rates
INSERT INTO [<schema>].[DenominationGroup] ([Json])
VALUES (N'{"$type":"DenominationGroup","Currency":"USD","GroupName":"Small Bills","Denominations":[20,10,5,1],"SortOrder":2,"IsActive":true}')

-- EUR Large Notes (500, 200, 100)
INSERT INTO [<schema>].[DenominationGroup] ([Json])
VALUES (N'{"$type":"DenominationGroup","Currency":"EUR","GroupName":"Large Notes","Denominations":[500,200,100],"SortOrder":1,"IsActive":true}')

-- EUR Standard Notes (50, 20, 10, 5)
INSERT INTO [<schema>].[DenominationGroup] ([Json])
VALUES (N'{"$type":"DenominationGroup","Currency":"EUR","GroupName":"Standard Notes","Denominations":[50,20,10,5],"SortOrder":2,"IsActive":true}')

-- GBP Large Notes (50, 20)
INSERT INTO [<schema>].[DenominationGroup] ([Json])
VALUES (N'{"$type":"DenominationGroup","Currency":"GBP","GroupName":"Large Notes","Denominations":[50,20],"SortOrder":1,"IsActive":true}')

-- GBP Small Notes (10, 5)
INSERT INTO [<schema>].[DenominationGroup] ([Json])
VALUES (N'{"$type":"DenominationGroup","Currency":"GBP","GroupName":"Small Notes","Denominations":[10,5],"SortOrder":2,"IsActive":true}')

-- CNY Standard Notes (100, 50, 20, 10, 5, 1)
INSERT INTO [<schema>].[DenominationGroup] ([Json])
VALUES (N'{"$type":"DenominationGroup","Currency":"CNY","GroupName":"All Notes","Denominations":[100,50,20,10,5,1],"SortOrder":1,"IsActive":true}')

-- JPY Large Notes (10000, 5000)
INSERT INTO [<schema>].[DenominationGroup] ([Json])
VALUES (N'{"$type":"DenominationGroup","Currency":"JPY","GroupName":"Large Notes","Denominations":[10000,5000],"SortOrder":1,"IsActive":true}')

-- JPY Small Notes (1000)
INSERT INTO [<schema>].[DenominationGroup] ([Json])
VALUES (N'{"$type":"DenominationGroup","Currency":"JPY","GroupName":"Small Notes","Denominations":[1000],"SortOrder":2,"IsActive":true}')

-- AUD Standard Notes (100, 50, 20, 10, 5)
INSERT INTO [<schema>].[DenominationGroup] ([Json])
VALUES (N'{"$type":"DenominationGroup","Currency":"AUD","GroupName":"All Notes","Denominations":[100,50,20,10,5],"SortOrder":1,"IsActive":true}')

-- RUB Large Notes (5000, 2000, 1000)
INSERT INTO [<schema>].[DenominationGroup] ([Json])
VALUES (N'{"$type":"DenominationGroup","Currency":"RUB","GroupName":"Large Notes","Denominations":[5000,2000,1000],"SortOrder":1,"IsActive":true}')

-- RUB Small Notes (500, 200, 100)
INSERT INTO [<schema>].[DenominationGroup] ([Json])
VALUES (N'{"$type":"DenominationGroup","Currency":"RUB","GroupName":"Small Notes","Denominations":[500,200,100],"SortOrder":2,"IsActive":true}')
