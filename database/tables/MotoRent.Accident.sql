-- Accident table (organization-wide)
CREATE TABLE [<schema>].[Accident]
(
    [AccidentId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for indexing
    [VehicleId] AS CAST(JSON_VALUE([Json], '$.VehicleId') AS INT),
    [RentalId] AS CAST(JSON_VALUE([Json], '$.RentalId') AS INT),
    [ReferenceNo] AS CAST(JSON_VALUE([Json], '$.ReferenceNo') AS NVARCHAR(50)),
    [Severity] AS CAST(JSON_VALUE([Json], '$.Severity') AS NVARCHAR(20)),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)),
    [PoliceInvolved] AS CAST(JSON_VALUE([Json], '$.PoliceInvolved') AS BIT),
    [InsuranceClaimFiled] AS CAST(JSON_VALUE([Json], '$.InsuranceClaimFiled') AS BIT),
    [AccidentDate] DATE NULL,
    [ReportedDate] DATE NULL,
    [ResolvedDate] DATE NULL,
    -- Financial summary columns
    [TotalEstimatedCost] AS CAST(JSON_VALUE([Json], '$.TotalEstimatedCost') AS DECIMAL(12,2)),
    [TotalActualCost] AS CAST(JSON_VALUE([Json], '$.TotalActualCost') AS DECIMAL(12,2)),
    [ReserveAmount] AS CAST(JSON_VALUE([Json], '$.ReserveAmount') AS DECIMAL(12,2)),
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
CREATE INDEX IX_Accident_Status ON [<schema>].[Accident]([Status])
--
CREATE INDEX IX_Accident_VehicleId ON [<schema>].[Accident]([VehicleId])
--
CREATE INDEX IX_Accident_RentalId ON [<schema>].[Accident]([RentalId])
--
CREATE INDEX IX_Accident_AccidentDate ON [<schema>].[Accident]([AccidentDate])
--
CREATE INDEX IX_Accident_Severity_Status ON [<schema>].[Accident]([Severity], [Status])
--
CREATE UNIQUE INDEX IX_Accident_ReferenceNo ON [<schema>].[Accident]([ReferenceNo])
--
