-- Agent table
-- Stores agents (tour guides, hotels, travel agencies) who make bookings on behalf of customers
CREATE TABLE [<schema>].[Agent]
(
    [AgentId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Agent Code (unique identifier like "TG-001", "HTL-PATONG")
    [AgentCode] AS CAST(JSON_VALUE([Json], '$.AgentCode') AS NVARCHAR(50)) PERSISTED,
    -- Basic Info
    [Name] AS CAST(JSON_VALUE([Json], '$.Name') AS NVARCHAR(200)),
    [AgentType] AS CAST(JSON_VALUE([Json], '$.AgentType') AS NVARCHAR(50)),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)),
    -- Contact
    [Phone] AS CAST(JSON_VALUE([Json], '$.Phone') AS NVARCHAR(50)),
    [Email] AS CAST(JSON_VALUE([Json], '$.Email') AS NVARCHAR(100)),
    -- Commission
    [CommissionType] AS CAST(JSON_VALUE([Json], '$.CommissionType') AS NVARCHAR(50)),
    [CommissionRate] AS CAST(JSON_VALUE([Json], '$.CommissionRate') AS DECIMAL(18,2)),
    [CommissionBalance] AS CAST(JSON_VALUE([Json], '$.CommissionBalance') AS DECIMAL(18,2)),
    -- Statistics
    [TotalBookings] AS CAST(JSON_VALUE([Json], '$.TotalBookings') AS INT),
    [TotalCommissionEarned] AS CAST(JSON_VALUE([Json], '$.TotalCommissionEarned') AS DECIMAL(18,2)),
    [TotalCommissionPaid] AS CAST(JSON_VALUE([Json], '$.TotalCommissionPaid') AS DECIMAL(18,2)),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

-- Unique index on AgentCode
CREATE UNIQUE INDEX IX_Agent_AgentCode ON [<schema>].[Agent]([AgentCode])

-- Index for querying by status
CREATE INDEX IX_Agent_Status ON [<schema>].[Agent]([Status])

-- Index for querying by type
CREATE INDEX IX_Agent_AgentType ON [<schema>].[Agent]([AgentType])

-- Index for querying by type and status
CREATE INDEX IX_Agent_AgentType_Status ON [<schema>].[Agent]([AgentType], [Status])

-- Index for outstanding commission balance
CREATE INDEX IX_Agent_CommissionBalance ON [<schema>].[Agent]([CommissionBalance]) WHERE [Status] = 'Active'
