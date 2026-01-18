-- AgentCommission table
-- Tracks commission for agent bookings
-- Commission becomes eligible only after rental is completed
CREATE TABLE [<schema>].[AgentCommission]
(
    [AgentCommissionId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Foreign Keys
    [AgentId] AS CAST(JSON_VALUE([Json], '$.AgentId') AS INT),
    [BookingId] AS CAST(JSON_VALUE([Json], '$.BookingId') AS INT),
    [RentalId] AS CAST(JSON_VALUE([Json], '$.RentalId') AS INT),
    -- Status: Pending, Approved, Paid, Voided
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)),
    -- Amounts
    [CommissionAmount] AS CAST(JSON_VALUE([Json], '$.CommissionAmount') AS DECIMAL(18,2)),
    [BookingTotal] AS CAST(JSON_VALUE([Json], '$.BookingTotal') AS DECIMAL(18,2)),
    -- Dates
    [EligibleDate] AS CAST(JSON_VALUE([Json], '$.EligibleDate') AS DATETIMEOFFSET),
    [ApprovedDate] AS CAST(JSON_VALUE([Json], '$.ApprovedDate') AS DATETIMEOFFSET),
    [PaidDate] AS CAST(JSON_VALUE([Json], '$.PaidDate') AS DATETIMEOFFSET),
    -- Denormalized
    [AgentCode] AS CAST(JSON_VALUE([Json], '$.AgentCode') AS NVARCHAR(50)),
    [BookingRef] AS CAST(JSON_VALUE([Json], '$.BookingRef') AS NVARCHAR(10)),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

-- Index for querying by agent
CREATE INDEX IX_AgentCommission_AgentId ON [<schema>].[AgentCommission]([AgentId])

-- Index for querying by booking
CREATE INDEX IX_AgentCommission_BookingId ON [<schema>].[AgentCommission]([BookingId])

-- Index for querying by rental
CREATE INDEX IX_AgentCommission_RentalId ON [<schema>].[AgentCommission]([RentalId])

-- Index for querying by status
CREATE INDEX IX_AgentCommission_Status ON [<schema>].[AgentCommission]([Status])

-- Index for querying agent commissions by status (for approval/payment workflows)
CREATE INDEX IX_AgentCommission_AgentId_Status ON [<schema>].[AgentCommission]([AgentId], [Status])

-- Note: EligibleDate and PaidDate cannot be indexed because DATETIMEOFFSET
-- computed columns from JSON_VALUE are not deterministic
