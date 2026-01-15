-- AccidentCost table
CREATE TABLE [<schema>].[AccidentCost]
(
    [AccidentCostId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns
    [AccidentId] AS CAST(JSON_VALUE([Json], '$.AccidentId') AS INT),
    [CostType] AS CAST(JSON_VALUE([Json], '$.CostType') AS NVARCHAR(30)),
    [EstimatedAmount] AS CAST(JSON_VALUE([Json], '$.EstimatedAmount') AS DECIMAL(12,2)),
    [ActualAmount] AS CAST(JSON_VALUE([Json], '$.ActualAmount') AS DECIMAL(12,2)),
    [IsCredit] AS CAST(JSON_VALUE([Json], '$.IsCredit') AS BIT),
    [IsApproved] AS CAST(JSON_VALUE([Json], '$.IsApproved') AS BIT),
    [PaidDate] DATE NULL,
    [AccidentPartyId] AS CAST(JSON_VALUE([Json], '$.AccidentPartyId') AS INT),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
--

CREATE INDEX IX_AccidentCost_AccidentId ON [<schema>].[AccidentCost]([AccidentId])
--
CREATE INDEX IX_AccidentCost_CostType ON [<schema>].[AccidentCost]([AccidentId], [CostType])
--
