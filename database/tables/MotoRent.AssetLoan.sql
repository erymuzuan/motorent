-- AssetLoan table - Loan/financing tracking
CREATE TABLE [<schema>].[AssetLoan]
(
    [AssetLoanId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Asset reference
    [AssetId] AS CAST(JSON_VALUE([Json], '$.AssetId') AS INT),
    -- Loan details
    [LenderName] AS CAST(JSON_VALUE([Json], '$.LenderName') AS NVARCHAR(100)),
    [PrincipalAmount] AS CAST(JSON_VALUE([Json], '$.PrincipalAmount') AS DECIMAL(12,2)),
    [AnnualInterestRate] AS CAST(JSON_VALUE([Json], '$.AnnualInterestRate') AS DECIMAL(6,4)),
    [TermMonths] AS CAST(JSON_VALUE([Json], '$.TermMonths') AS INT),
    [MonthlyPayment] AS CAST(JSON_VALUE([Json], '$.MonthlyPayment') AS DECIMAL(12,2)),
    [StartDate] DATE NULL,
    [EndDate] DATE NULL,
    -- Current balances
    [RemainingPrincipal] AS CAST(JSON_VALUE([Json], '$.RemainingPrincipal') AS DECIMAL(12,2)),
    [PaymentsMade] AS CAST(JSON_VALUE([Json], '$.PaymentsMade') AS INT),
    [NextPaymentDue] DATE NULL,
    -- Status
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
--

CREATE INDEX IX_AssetLoan_AssetId ON [<schema>].[AssetLoan]([AssetId])
--
CREATE INDEX IX_AssetLoan_Status ON [<schema>].[AssetLoan]([Status])
--
CREATE INDEX IX_AssetLoan_NextPaymentDue ON [<schema>].[AssetLoan]([NextPaymentDue]) WHERE [Status] = 'Active'
--
