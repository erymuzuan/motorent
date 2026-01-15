-- AssetLoanPayment table - Individual loan payment records
CREATE TABLE [<schema>].[AssetLoanPayment]
(
    [AssetLoanPaymentId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Loan reference
    [AssetLoanId] AS CAST(JSON_VALUE([Json], '$.AssetLoanId') AS INT),
    -- Payment details
    [PaymentNumber] AS CAST(JSON_VALUE([Json], '$.PaymentNumber') AS INT),
    [DueDate] DATE NULL,
    [PaidDate] DATE NULL,
    [TotalAmount] AS CAST(JSON_VALUE([Json], '$.TotalAmount') AS DECIMAL(12,2)),
    [PrincipalAmount] AS CAST(JSON_VALUE([Json], '$.PrincipalAmount') AS DECIMAL(12,2)),
    [InterestAmount] AS CAST(JSON_VALUE([Json], '$.InterestAmount') AS DECIMAL(12,2)),
    [BalanceAfter] AS CAST(JSON_VALUE([Json], '$.BalanceAfter') AS DECIMAL(12,2)),
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

CREATE INDEX IX_AssetLoanPayment_LoanId ON [<schema>].[AssetLoanPayment]([AssetLoanId])
--
CREATE INDEX IX_AssetLoanPayment_Status ON [<schema>].[AssetLoanPayment]([AssetLoanId], [Status])
--
CREATE INDEX IX_AssetLoanPayment_DueDate ON [<schema>].[AssetLoanPayment]([DueDate])
--
