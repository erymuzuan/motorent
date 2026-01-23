-- Rental table
CREATE TABLE [<schema>].[Rental]
(
    [RentalId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Shop and Location (COALESCE for backward compatibility with old ShopId)
    [RentedFromShopId] AS CAST(COALESCE(JSON_VALUE([Json], '$.RentedFromShopId'), JSON_VALUE([Json], '$.ShopId')) AS INT),
    [ReturnedToShopId] AS CAST(JSON_VALUE([Json], '$.ReturnedToShopId') AS INT),
    [VehiclePoolId] AS CAST(JSON_VALUE([Json], '$.VehiclePoolId') AS INT),
    -- Renter and Vehicle (COALESCE for backward compatibility with old MotorbikeId)
    [RenterId] AS CAST(JSON_VALUE([Json], '$.RenterId') AS INT),
    [VehicleId] AS CAST(COALESCE(JSON_VALUE([Json], '$.VehicleId'), JSON_VALUE([Json], '$.MotorbikeId')) AS INT),
    -- Duration Type (defaults to Daily for backward compatibility)
    [DurationType] AS CAST(COALESCE(JSON_VALUE([Json], '$.DurationType'), 'Daily') AS NVARCHAR(20)),
    [IntervalMinutes] AS CAST(JSON_VALUE([Json], '$.IntervalMinutes') AS INT),
    -- Booking Reference (for rentals created from bookings)
    [BookingId] AS CAST(JSON_VALUE([Json], '$.BookingId') AS INT),
    -- Status and Dates
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)),
    [StartDate] DATE NULL,
    [ExpectedEndDate] DATE NULL,
    -- Driver/Guide
    [IncludeDriver] AS CAST(COALESCE(JSON_VALUE([Json], '$.IncludeDriver'), 'false') AS BIT),
    [IncludeGuide] AS CAST(COALESCE(JSON_VALUE([Json], '$.IncludeGuide'), 'false') AS BIT),
    -- Till Session
    [TillSessionId] AS CAST(JSON_VALUE([Json], '$.TillSessionId') AS INT),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)


CREATE INDEX IX_Rental_RentedFromShopId_Status ON [<schema>].[Rental]([RentedFromShopId], [Status])

CREATE INDEX IX_Rental_ReturnedToShopId ON [<schema>].[Rental]([ReturnedToShopId]) WHERE [ReturnedToShopId] IS NOT NULL

CREATE INDEX IX_Rental_VehiclePoolId ON [<schema>].[Rental]([VehiclePoolId]) WHERE [VehiclePoolId] IS NOT NULL

CREATE INDEX IX_Rental_RenterId ON [<schema>].[Rental]([RenterId])

CREATE INDEX IX_Rental_VehicleId ON [<schema>].[Rental]([VehicleId])

CREATE INDEX IX_Rental_DurationType ON [<schema>].[Rental]([DurationType])

CREATE INDEX IX_Rental_BookingId ON [<schema>].[Rental]([BookingId]) WHERE [BookingId] IS NOT NULL

CREATE INDEX IX_Rental_TillSessionId ON [<schema>].[Rental]([TillSessionId]) WHERE [TillSessionId] IS NOT NULL

