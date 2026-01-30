-- Traffic/parking fines issued against company vehicles
CREATE TABLE [<schema>].[TrafficFine]
(
    [TrafficFineId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for indexing
    [VehicleId] AS CAST(JSON_VALUE([Json], '$.VehicleId') AS INT),
    [RentalId] AS CAST(JSON_VALUE([Json], '$.RentalId') AS INT),
    [FineType] AS CAST(JSON_VALUE([Json], '$.FineType') AS NVARCHAR(30)),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(30)),
    [Amount] AS CAST(JSON_VALUE([Json], '$.Amount') AS DECIMAL(12,2)),
    [ReferenceNo] AS CAST(JSON_VALUE([Json], '$.ReferenceNo') AS NVARCHAR(50)),
    -- Persisted date columns
    [FineDate] DATE NULL,
    [ResolvedDate] DATE NULL,
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL,
    [ChangedBy] VARCHAR(50) NOT NULL,
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL,
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL
)
--

-- Indexes
CREATE INDEX IX_TrafficFine_VehicleId ON [<schema>].[TrafficFine]([VehicleId])
--
CREATE INDEX IX_TrafficFine_RentalId ON [<schema>].[TrafficFine]([RentalId])
--
CREATE INDEX IX_TrafficFine_Status_FineDate ON [<schema>].[TrafficFine]([Status], [FineDate])
--
CREATE INDEX IX_TrafficFine_FineDate ON [<schema>].[TrafficFine]([FineDate])
--
