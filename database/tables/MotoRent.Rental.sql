-- Rental table
CREATE TABLE [<schema>].[Rental]
(
    [RentalId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for querying
    [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
    [RenterId] AS CAST(JSON_VALUE([Json], '$.RenterId') AS INT),
    [MotorbikeId] AS CAST(JSON_VALUE([Json], '$.MotorbikeId') AS INT),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)),
    [StartDate] AS CAST(JSON_VALUE([Json], '$.StartDate') AS DATE),
    [ExpectedEndDate] AS CAST(JSON_VALUE([Json], '$.ExpectedEndDate') AS DATE),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE INDEX IX_Rental_ShopId_Status ON [<schema>].[Rental]([ShopId], [Status])
CREATE INDEX IX_Rental_RenterId ON [<schema>].[Rental]([RenterId])
CREATE INDEX IX_Rental_MotorbikeId ON [<schema>].[Rental]([MotorbikeId])
