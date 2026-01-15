-- Asset table - Financial tracking record for vehicles
CREATE TABLE [<schema>].[Asset]
(
    [AssetId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Vehicle reference
    [VehicleId] AS CAST(JSON_VALUE([Json], '$.VehicleId') AS INT),
    -- Acquisition
    [AcquisitionDate] DATE NULL,
    [AcquisitionCost] AS CAST(JSON_VALUE([Json], '$.AcquisitionCost') AS DECIMAL(12,2)),
    [FirstRentalDate] DATE NULL,
    [IsPreExisting] AS CAST(JSON_VALUE([Json], '$.IsPreExisting') AS BIT),
    -- Depreciation
    [DepreciationMethod] AS CAST(JSON_VALUE([Json], '$.DepreciationMethod') AS NVARCHAR(30)),
    [UsefulLifeMonths] AS CAST(JSON_VALUE([Json], '$.UsefulLifeMonths') AS INT),
    [ResidualValue] AS CAST(JSON_VALUE([Json], '$.ResidualValue') AS DECIMAL(12,2)),
    -- Current values
    [CurrentBookValue] AS CAST(JSON_VALUE([Json], '$.CurrentBookValue') AS DECIMAL(12,2)),
    [AccumulatedDepreciation] AS CAST(JSON_VALUE([Json], '$.AccumulatedDepreciation') AS DECIMAL(12,2)),
    [TotalExpenses] AS CAST(JSON_VALUE([Json], '$.TotalExpenses') AS DECIMAL(12,2)),
    [TotalRevenue] AS CAST(JSON_VALUE([Json], '$.TotalRevenue') AS DECIMAL(12,2)),
    [LastDepreciationDate] DATE NULL,
    -- Status
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)),
    [DisposalDate] DATE NULL,
    -- Loan
    [AssetLoanId] AS CAST(JSON_VALUE([Json], '$.AssetLoanId') AS INT),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
--

CREATE UNIQUE INDEX IX_Asset_VehicleId ON [<schema>].[Asset]([VehicleId])
--
CREATE INDEX IX_Asset_Status ON [<schema>].[Asset]([Status])
--
CREATE INDEX IX_Asset_AcquisitionDate ON [<schema>].[Asset]([AcquisitionDate])
--
CREATE INDEX IX_Asset_DepreciationMethod ON [<schema>].[Asset]([DepreciationMethod])
--
