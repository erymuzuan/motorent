$schema = "KrabiBeachRentals"

# Drop tables if they exist
$dropSql = @"
DROP TABLE IF EXISTS [$schema].[AssetLoanPayment];
DROP TABLE IF EXISTS [$schema].[AssetLoan];
DROP TABLE IF EXISTS [$schema].[AssetExpense];
DROP TABLE IF EXISTS [$schema].[DepreciationEntry];
DROP TABLE IF EXISTS [$schema].[Asset];
"@

Write-Host "Dropping existing tables..."
try {
    Invoke-Sqlcmd -ServerInstance "localhost\DEV2022" -Database MotoRent -Query $dropSql -ErrorAction Stop
    Write-Host "  Tables dropped!" -ForegroundColor Green
} catch {
    Write-Host "  Error: $_" -ForegroundColor Red
}

# Create Asset table
$assetSql = @"
CREATE TABLE [$schema].[Asset]
(
    [AssetId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [VehicleId] AS CAST(JSON_VALUE([Json], '$.VehicleId') AS INT) PERSISTED,
    [AcquisitionDate] AS CAST(JSON_VALUE([Json], '$.AcquisitionDate') AS DATE),
    [AcquisitionCost] AS CAST(JSON_VALUE([Json], '$.AcquisitionCost') AS DECIMAL(12,2)),
    [FirstRentalDate] AS CAST(JSON_VALUE([Json], '$.FirstRentalDate') AS DATE),
    [IsPreExisting] AS CAST(JSON_VALUE([Json], '$.IsPreExisting') AS BIT),
    [DepreciationMethod] AS CAST(JSON_VALUE([Json], '$.DepreciationMethod') AS NVARCHAR(30)),
    [UsefulLifeMonths] AS CAST(JSON_VALUE([Json], '$.UsefulLifeMonths') AS INT),
    [ResidualValue] AS CAST(JSON_VALUE([Json], '$.ResidualValue') AS DECIMAL(12,2)),
    [CurrentBookValue] AS CAST(JSON_VALUE([Json], '$.CurrentBookValue') AS DECIMAL(12,2)),
    [AccumulatedDepreciation] AS CAST(JSON_VALUE([Json], '$.AccumulatedDepreciation') AS DECIMAL(12,2)),
    [TotalExpenses] AS CAST(JSON_VALUE([Json], '$.TotalExpenses') AS DECIMAL(12,2)),
    [TotalRevenue] AS CAST(JSON_VALUE([Json], '$.TotalRevenue') AS DECIMAL(12,2)),
    [LastDepreciationDate] AS CAST(JSON_VALUE([Json], '$.LastDepreciationDate') AS DATE),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)) PERSISTED,
    [DisposalDate] AS CAST(JSON_VALUE([Json], '$.DisposalDate') AS DATE),
    [AssetLoanId] AS CAST(JSON_VALUE([Json], '$.AssetLoanId') AS INT),
    [Json] NVARCHAR(MAX) NOT NULL,
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
);
CREATE UNIQUE INDEX IX_Asset_VehicleId ON [$schema].[Asset]([VehicleId]);
CREATE INDEX IX_Asset_Status ON [$schema].[Asset]([Status]);
"@

Write-Host "Creating Asset table..."
try {
    Invoke-Sqlcmd -ServerInstance "localhost\DEV2022" -Database MotoRent -Query $assetSql -ErrorAction Stop
    Write-Host "  Success!" -ForegroundColor Green
} catch {
    Write-Host "  Error: $_" -ForegroundColor Red
}

# Create DepreciationEntry table
$depreciationSql = @"
CREATE TABLE [$schema].[DepreciationEntry]
(
    [DepreciationEntryId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [AssetId] AS CAST(JSON_VALUE([Json], '$.AssetId') AS INT) PERSISTED,
    [PeriodStart] AS CAST(JSON_VALUE([Json], '$.PeriodStart') AS DATE),
    [PeriodEnd] AS CAST(JSON_VALUE([Json], '$.PeriodEnd') AS DATE),
    [Amount] AS CAST(JSON_VALUE([Json], '$.Amount') AS DECIMAL(12,2)),
    [BookValueStart] AS CAST(JSON_VALUE([Json], '$.BookValueStart') AS DECIMAL(12,2)),
    [BookValueEnd] AS CAST(JSON_VALUE([Json], '$.BookValueEnd') AS DECIMAL(12,2)),
    [Method] AS CAST(JSON_VALUE([Json], '$.Method') AS NVARCHAR(30)),
    [EntryType] AS CAST(JSON_VALUE([Json], '$.EntryType') AS NVARCHAR(20)),
    [IsManualOverride] AS CAST(JSON_VALUE([Json], '$.IsManualOverride') AS BIT),
    [Json] NVARCHAR(MAX) NOT NULL,
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
);
CREATE INDEX IX_DepreciationEntry_AssetId ON [$schema].[DepreciationEntry]([AssetId]);
"@

Write-Host "Creating DepreciationEntry table..."
try {
    Invoke-Sqlcmd -ServerInstance "localhost\DEV2022" -Database MotoRent -Query $depreciationSql -ErrorAction Stop
    Write-Host "  Success!" -ForegroundColor Green
} catch {
    Write-Host "  Error: $_" -ForegroundColor Red
}

# Create AssetExpense table
$expenseSql = @"
CREATE TABLE [$schema].[AssetExpense]
(
    [AssetExpenseId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [AssetId] AS CAST(JSON_VALUE([Json], '$.AssetId') AS INT) PERSISTED,
    [Category] AS CAST(JSON_VALUE([Json], '$.Category') AS NVARCHAR(30)),
    [Amount] AS CAST(JSON_VALUE([Json], '$.Amount') AS DECIMAL(12,2)),
    [ExpenseDate] AS CAST(JSON_VALUE([Json], '$.ExpenseDate') AS DATE),
    [IsPaid] AS CAST(JSON_VALUE([Json], '$.IsPaid') AS BIT),
    [RentalId] AS CAST(JSON_VALUE([Json], '$.RentalId') AS INT),
    [AccidentId] AS CAST(JSON_VALUE([Json], '$.AccidentId') AS INT),
    [MaintenanceScheduleId] AS CAST(JSON_VALUE([Json], '$.MaintenanceScheduleId') AS INT),
    [AssetLoanPaymentId] AS CAST(JSON_VALUE([Json], '$.AssetLoanPaymentId') AS INT),
    [AccountingPeriod] AS CAST(JSON_VALUE([Json], '$.AccountingPeriod') AS CHAR(7)),
    [IsTaxDeductible] AS CAST(JSON_VALUE([Json], '$.IsTaxDeductible') AS BIT),
    [Json] NVARCHAR(MAX) NOT NULL,
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
);
CREATE INDEX IX_AssetExpense_AssetId ON [$schema].[AssetExpense]([AssetId]);
"@

Write-Host "Creating AssetExpense table..."
try {
    Invoke-Sqlcmd -ServerInstance "localhost\DEV2022" -Database MotoRent -Query $expenseSql -ErrorAction Stop
    Write-Host "  Success!" -ForegroundColor Green
} catch {
    Write-Host "  Error: $_" -ForegroundColor Red
}

# Create AssetLoan table
$loanSql = @"
CREATE TABLE [$schema].[AssetLoan]
(
    [AssetLoanId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [AssetId] AS CAST(JSON_VALUE([Json], '$.AssetId') AS INT) PERSISTED,
    [LenderName] AS CAST(JSON_VALUE([Json], '$.LenderName') AS NVARCHAR(100)),
    [PrincipalAmount] AS CAST(JSON_VALUE([Json], '$.PrincipalAmount') AS DECIMAL(12,2)),
    [AnnualInterestRate] AS CAST(JSON_VALUE([Json], '$.AnnualInterestRate') AS DECIMAL(6,4)),
    [TermMonths] AS CAST(JSON_VALUE([Json], '$.TermMonths') AS INT),
    [MonthlyPayment] AS CAST(JSON_VALUE([Json], '$.MonthlyPayment') AS DECIMAL(12,2)),
    [StartDate] AS CAST(JSON_VALUE([Json], '$.StartDate') AS DATE),
    [EndDate] AS CAST(JSON_VALUE([Json], '$.EndDate') AS DATE),
    [RemainingPrincipal] AS CAST(JSON_VALUE([Json], '$.RemainingPrincipal') AS DECIMAL(12,2)),
    [PaymentsMade] AS CAST(JSON_VALUE([Json], '$.PaymentsMade') AS INT),
    [NextPaymentDue] AS CAST(JSON_VALUE([Json], '$.NextPaymentDue') AS DATE),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)) PERSISTED,
    [Json] NVARCHAR(MAX) NOT NULL,
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
);
CREATE INDEX IX_AssetLoan_AssetId ON [$schema].[AssetLoan]([AssetId]);
CREATE INDEX IX_AssetLoan_Status ON [$schema].[AssetLoan]([Status]);
"@

Write-Host "Creating AssetLoan table..."
try {
    Invoke-Sqlcmd -ServerInstance "localhost\DEV2022" -Database MotoRent -Query $loanSql -ErrorAction Stop
    Write-Host "  Success!" -ForegroundColor Green
} catch {
    Write-Host "  Error: $_" -ForegroundColor Red
}

# Create AssetLoanPayment table
$paymentSql = @"
CREATE TABLE [$schema].[AssetLoanPayment]
(
    [AssetLoanPaymentId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [AssetLoanId] AS CAST(JSON_VALUE([Json], '$.AssetLoanId') AS INT) PERSISTED,
    [PaymentNumber] AS CAST(JSON_VALUE([Json], '$.PaymentNumber') AS INT),
    [DueDate] AS CAST(JSON_VALUE([Json], '$.DueDate') AS DATE),
    [PaidDate] AS CAST(JSON_VALUE([Json], '$.PaidDate') AS DATE),
    [TotalAmount] AS CAST(JSON_VALUE([Json], '$.TotalAmount') AS DECIMAL(12,2)),
    [PrincipalAmount] AS CAST(JSON_VALUE([Json], '$.PrincipalAmount') AS DECIMAL(12,2)),
    [InterestAmount] AS CAST(JSON_VALUE([Json], '$.InterestAmount') AS DECIMAL(12,2)),
    [BalanceAfter] AS CAST(JSON_VALUE([Json], '$.BalanceAfter') AS DECIMAL(12,2)),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)) PERSISTED,
    [Json] NVARCHAR(MAX) NOT NULL,
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
);
CREATE INDEX IX_AssetLoanPayment_LoanId ON [$schema].[AssetLoanPayment]([AssetLoanId]);
CREATE INDEX IX_AssetLoanPayment_Status ON [$schema].[AssetLoanPayment]([AssetLoanId], [Status]);
"@

Write-Host "Creating AssetLoanPayment table..."
try {
    Invoke-Sqlcmd -ServerInstance "localhost\DEV2022" -Database MotoRent -Query $paymentSql -ErrorAction Stop
    Write-Host "  Success!" -ForegroundColor Green
} catch {
    Write-Host "  Error: $_" -ForegroundColor Red
}

Write-Host "`nAll tables created!" -ForegroundColor Cyan
