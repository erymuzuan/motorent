-- Migration: Add pick-up/drop-off location columns to Rental table
-- These are computed columns reading from JSON, no data migration needed

ALTER TABLE [<schema>].[Rental] ADD
    [PickupLocationId] AS CAST(JSON_VALUE([Json], '$.PickupLocationId') AS INT),
    [DropoffLocationId] AS CAST(JSON_VALUE([Json], '$.DropoffLocationId') AS INT),
    [PickupLocationFee] AS CAST(COALESCE(JSON_VALUE([Json], '$.PickupLocationFee'), '0') AS DECIMAL(18,2)),
    [DropoffLocationFee] AS CAST(COALESCE(JSON_VALUE([Json], '$.DropoffLocationFee'), '0') AS DECIMAL(18,2)),
    [OutOfHoursPickupFee] AS CAST(COALESCE(JSON_VALUE([Json], '$.OutOfHoursPickupFee'), '0') AS DECIMAL(18,2)),
    [OutOfHoursDropoffFee] AS CAST(COALESCE(JSON_VALUE([Json], '$.OutOfHoursDropoffFee'), '0') AS DECIMAL(18,2)),
    [IsOutOfHoursPickup] AS CAST(COALESCE(JSON_VALUE([Json], '$.IsOutOfHoursPickup'), 'false') AS BIT),
    [IsOutOfHoursDropoff] AS CAST(COALESCE(JSON_VALUE([Json], '$.IsOutOfHoursDropoff'), 'false') AS BIT)
GO

-- Index for pickup location queries
CREATE INDEX IX_Rental_PickupLocation ON [<schema>].[Rental]([PickupLocationId])
    WHERE [PickupLocationId] IS NOT NULL
GO

-- Index for dropoff location queries
CREATE INDEX IX_Rental_DropoffLocation ON [<schema>].[Rental]([DropoffLocationId])
    WHERE [DropoffLocationId] IS NOT NULL
GO
