-- TillTransaction table - Individual transactions in a till session
CREATE TABLE [<schema>].[TillTransaction]
(
    [TillTransactionId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for indexing
    [TillSessionId] AS CAST(JSON_VALUE([Json], '$.TillSessionId') AS INT),
    [TransactionType] AS CAST(JSON_VALUE([Json], '$.TransactionType') AS NVARCHAR(30)),
    [Direction] AS CAST(JSON_VALUE([Json], '$.Direction') AS NVARCHAR(10)),
    [Amount] AS CAST(JSON_VALUE([Json], '$.Amount') AS DECIMAL(18,2)),
    [Category] AS CAST(JSON_VALUE([Json], '$.Category') AS NVARCHAR(50)),
    [PaymentId] AS CAST(JSON_VALUE([Json], '$.PaymentId') AS INT),
    [DepositId] AS CAST(JSON_VALUE([Json], '$.DepositId') AS INT),
    [RentalId] AS CAST(JSON_VALUE([Json], '$.RentalId') AS INT),
    [TransactionTime] AS CONVERT(DATETIMEOFFSET, JSON_VALUE([Json], '$.TransactionTime'), 127) PERSISTED,
    [IsVerified] AS CAST(JSON_VALUE([Json], '$.IsVerified') AS BIT),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE INDEX IX_TillTransaction_TillSessionId ON [<schema>].[TillTransaction]([TillSessionId])
CREATE INDEX IX_TillTransaction_TransactionType ON [<schema>].[TillTransaction]([TransactionType])
CREATE INDEX IX_TillTransaction_TransactionTime ON [<schema>].[TillTransaction]([TransactionTime])
CREATE INDEX IX_TillTransaction_PaymentId ON [<schema>].[TillTransaction]([PaymentId])
CREATE INDEX IX_TillTransaction_RentalId ON [<schema>].[TillTransaction]([RentalId])
