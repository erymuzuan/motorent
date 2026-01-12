-- Accident table
CREATE TABLE [<schema>].[Accident]
(
    [AccidentId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for indexing
    [VehicleId] AS CAST(JSON_VALUE([Json], '$.VehicleId') AS INT),
    [RentalId] AS CAST(JSON_VALUE([Json], '$.RentalId') AS INT),
    [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
    [ReferenceNo] AS CAST(JSON_VALUE([Json], '$.ReferenceNo') AS NVARCHAR(50)),
    [Severity] AS CAST(JSON_VALUE([Json], '$.Severity') AS NVARCHAR(20)),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)),
    [PoliceInvolved] AS CAST(JSON_VALUE([Json], '$.PoliceInvolved') AS BIT),
    [InsuranceClaimFiled] AS CAST(JSON_VALUE([Json], '$.InsuranceClaimFiled') AS BIT),
    [AccidentDate] AS CAST(JSON_VALUE([Json], '$.AccidentDate') AS DATE),
    [ReportedDate] AS CAST(JSON_VALUE([Json], '$.ReportedDate') AS DATE),
    [ResolvedDate] AS CAST(JSON_VALUE([Json], '$.ResolvedDate') AS DATE),
    -- Financial summary columns
    [TotalEstimatedCost] AS CAST(JSON_VALUE([Json], '$.TotalEstimatedCost') AS DECIMAL(12,2)),
    [TotalActualCost] AS CAST(JSON_VALUE([Json], '$.TotalActualCost') AS DECIMAL(12,2)),
    [ReserveAmount] AS CAST(JSON_VALUE([Json], '$.ReserveAmount') AS DECIMAL(12,2)),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
GO

-- Indexes
CREATE INDEX IX_Accident_ShopId_Status ON [<schema>].[Accident]([ShopId], [Status])
GO
CREATE INDEX IX_Accident_VehicleId ON [<schema>].[Accident]([VehicleId])
GO
CREATE INDEX IX_Accident_RentalId ON [<schema>].[Accident]([RentalId])
GO
CREATE INDEX IX_Accident_AccidentDate ON [<schema>].[Accident]([AccidentDate])
GO
CREATE INDEX IX_Accident_Severity_Status ON [<schema>].[Accident]([Severity], [Status])
GO
CREATE UNIQUE INDEX IX_Accident_ReferenceNo ON [<schema>].[Accident]([ReferenceNo])
GO
