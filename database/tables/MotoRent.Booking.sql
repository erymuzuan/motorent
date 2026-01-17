-- Booking table
-- Stores reservations/bookings made by customers (tourist portal or staff-assisted)
CREATE TABLE [<schema>].[Booking]
(
    [BookingId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Booking Reference (unique 6-char alphanumeric)
    [BookingRef] AS CAST(JSON_VALUE([Json], '$.BookingRef') AS NVARCHAR(10)),
    -- Status
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)),
    -- Shop (optional - customer can check in at any shop)
    [PreferredShopId] AS CAST(JSON_VALUE([Json], '$.PreferredShopId') AS INT),
    [CheckedInAtShopId] AS CAST(JSON_VALUE([Json], '$.CheckedInAtShopId') AS INT),
    -- Customer
    [RenterId] AS CAST(JSON_VALUE([Json], '$.RenterId') AS INT),
    [CustomerName] AS CAST(JSON_VALUE([Json], '$.CustomerName') AS NVARCHAR(100)),
    [CustomerPhone] AS CAST(JSON_VALUE([Json], '$.CustomerPhone') AS NVARCHAR(50)),
    [CustomerEmail] AS CAST(JSON_VALUE([Json], '$.CustomerEmail') AS NVARCHAR(100)),
    -- Dates
    [StartDate] DATE NULL,
    [EndDate] DATE NULL,
    -- Payment
    [PaymentStatus] AS CAST(JSON_VALUE([Json], '$.PaymentStatus') AS NVARCHAR(20)),
    [TotalAmount] AS CAST(JSON_VALUE([Json], '$.TotalAmount') AS MONEY),
    [AmountPaid] AS CAST(JSON_VALUE([Json], '$.AmountPaid') AS MONEY),
    -- Source
    [BookingSource] AS CAST(JSON_VALUE([Json], '$.BookingSource') AS NVARCHAR(20)),
    -- Agent (for agent-referred bookings)
    [AgentId] AS CAST(JSON_VALUE([Json], '$.AgentId') AS INT),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

-- Unique index on BookingRef (no filter - computed columns don't support filtered indexes)
CREATE UNIQUE INDEX IX_Booking_BookingRef ON [<schema>].[Booking]([BookingRef])

-- Index for querying by status
CREATE INDEX IX_Booking_Status ON [<schema>].[Booking]([Status])

-- Index for querying by preferred shop (no filter - computed column)
CREATE INDEX IX_Booking_PreferredShopId ON [<schema>].[Booking]([PreferredShopId])

-- Index for querying by start date and status (upcoming bookings)
CREATE INDEX IX_Booking_StartDate_Status ON [<schema>].[Booking]([StartDate], [Status])

-- Index for customer lookup (no filter - computed columns)
CREATE INDEX IX_Booking_CustomerPhone ON [<schema>].[Booking]([CustomerPhone])
CREATE INDEX IX_Booking_CustomerEmail ON [<schema>].[Booking]([CustomerEmail])

-- Index for linked renter (no filter - computed column)
CREATE INDEX IX_Booking_RenterId ON [<schema>].[Booking]([RenterId])

-- Index for agent-referred bookings
CREATE INDEX IX_Booking_AgentId ON [<schema>].[Booking]([AgentId])

