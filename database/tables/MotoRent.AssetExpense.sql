-- AssetExpense table - Expense tracking for assets
CREATE TABLE [<schema>].[AssetExpense]
(
    [AssetExpenseId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Asset reference
    [AssetId] AS CAST(JSON_VALUE([Json], '$.AssetId') AS INT),
    -- Expense details
    [Cate--ry] AS CAST(JSON_VALUE([Json], '$.Cate--ry') AS NVARCHAR(30)),
    [Amount] AS CAST(JSON_VALUE([Json], '$.Amount') AS DECIMAL(12,2)),
    [ExpenseDate] DATE NULL,
    [IsPaid] AS CAST(JSON_VALUE([Json], '$.IsPaid') AS BIT),
    -- Related entities
    [RentalId] AS CAST(JSON_VALUE([Json], '$.RentalId') AS INT),
    [AccidentId] AS CAST(JSON_VALUE([Json], '$.AccidentId') AS INT),
    [MaintenanceScheduleId] AS CAST(JSON_VALUE([Json], '$.MaintenanceScheduleId') AS INT),
    [AssetLoanPaymentId] AS CAST(JSON_VALUE([Json], '$.AssetLoanPaymentId') AS INT),
    -- Accounting
    [AccountingPeriod] AS CAST(JSON_VALUE([Json], '$.AccountingPeriod') AS CHAR(7)),
    [IsTaxDeductible] AS CAST(JSON_VALUE([Json], '$.IsTaxDeductible') AS BIT),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
--

CREATE INDEX IX_AssetExpense_AssetId ON [<schema>].[AssetExpense]([AssetId])
--
CREATE INDEX IX_AssetExpense_Cate--ry ON [<schema>].[AssetExpense]([AssetId], [Cate--ry])
--
CREATE INDEX IX_AssetExpense_ExpenseDate ON [<schema>].[AssetExpense]([ExpenseDate])
--
CREATE INDEX IX_AssetExpense_AccountingPeriod ON [<schema>].[AssetExpense]([AccountingPeriod])
--
