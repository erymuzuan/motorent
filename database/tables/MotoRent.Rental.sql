-- Rental table
CREATE TABLE [<schema>].[Rental]
(
    [RentalId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Shop and Location
    [RentedFromShopId] AS CAST(JSON_VALUE([Json], '$.RentedFromShopId') AS INT),
    [ReturnedToShopId] AS CAST(JSON_VALUE([Json], '$.ReturnedToShopId') AS INT),
    [VehiclePoolId] AS CAST(JSON_VALUE([Json], '$.VehiclePoolId') AS INT),
    -- Renter and Vehicle
    [RenterId] AS CAST(JSON_VALUE([Json], '$.RenterId') AS INT),
    [VehicleId] AS CAST(JSON_VALUE([Json], '$.VehicleId') AS INT),
    -- Duration Type
    [DurationType] AS CAST(JSON_VALUE([Json], '$.DurationType') AS NVARCHAR(20)),
    [IntervalMinutes] AS CAST(JSON_VALUE([Json], '$.IntervalMinutes') AS INT),
    -- Status and Dates
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)),
    [StartDate] AS CAST(JSON_VALUE([Json], '$.StartDate') AS DATE),
    [ExpectedEndDate] AS CAST(JSON_VALUE([Json], '$.ExpectedEndDate') AS DATE),
    -- Driver/Guide
    [IncludeDriver] AS CAST(JSON_VALUE([Json], '$.IncludeDriver') AS BIT),
    [IncludeGuide] AS CAST(JSON_VALUE([Json], '$.IncludeGuide') AS BIT),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
GO

CREATE INDEX IX_Rental_RentedFromShopId_Status ON [<schema>].[Rental]([RentedFromShopId], [Status])
GO
CREATE INDEX IX_Rental_ReturnedToShopId ON [<schema>].[Rental]([ReturnedToShopId]) WHERE [ReturnedToShopId] IS NOT NULL
GO
CREATE INDEX IX_Rental_VehiclePoolId ON [<schema>].[Rental]([VehiclePoolId]) WHERE [VehiclePoolId] IS NOT NULL
GO
CREATE INDEX IX_Rental_RenterId ON [<schema>].[Rental]([RenterId])
GO
CREATE INDEX IX_Rental_VehicleId ON [<schema>].[Rental]([VehicleId])
GO
CREATE INDEX IX_Rental_DurationType ON [<schema>].[Rental]([DurationType])
GO
