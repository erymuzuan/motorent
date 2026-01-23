-- Receipt table - Stores receipts for transactions (check-in, check-out, booking deposits)
CREATE TABLE [<schema>].[Receipt]
(
    [ReceiptId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for indexing and querying
    [ReceiptNo] AS CAST(JSON_VALUE([Json], '$.ReceiptNo') AS NVARCHAR(20)),
    [ReceiptType] AS CAST(JSON_VALUE([Json], '$.ReceiptType') AS NVARCHAR(20)),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)),
    [BookingId] AS CAST(JSON_VALUE([Json], '$.BookingId') AS INT),
    [RentalId] AS CAST(JSON_VALUE([Json], '$.RentalId') AS INT),
    [TillSessionId] AS CAST(JSON_VALUE([Json], '$.TillSessionId') AS INT),
    [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
    [RenterId] AS CAST(JSON_VALUE([Json], '$.RenterId') AS INT),
    [CustomerName] AS CAST(JSON_VALUE([Json], '$.CustomerName') AS NVARCHAR(100)),
    [GrandTotal] AS CAST(JSON_VALUE([Json], '$.GrandTotal') AS MONEY),
    [IssuedOn] AS CONVERT(DATETIMEOFFSET, JSON_VALUE([Json], '$.IssuedOn'), 127) PERSISTED,
    [IssuedByUserName] AS CAST(JSON_VALUE([Json], '$.IssuedByUserName') AS NVARCHAR(50)),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

-- Unique index on receipt number
CREATE UNIQUE INDEX IX_Receipt_ReceiptNo ON [<schema>].[Receipt]([ReceiptNo]) WHERE [ReceiptNo] IS NOT NULL

-- Indexes for common queries
CREATE INDEX IX_Receipt_RentalId ON [<schema>].[Receipt]([RentalId])
CREATE INDEX IX_Receipt_BookingId ON [<schema>].[Receipt]([BookingId])
CREATE INDEX IX_Receipt_ShopId_IssuedOn ON [<schema>].[Receipt]([ShopId], [IssuedOn])
CREATE INDEX IX_Receipt_TillSessionId ON [<schema>].[Receipt]([TillSessionId])
CREATE INDEX IX_Receipt_RenterId ON [<schema>].[Receipt]([RenterId])
CREATE INDEX IX_Receipt_Status ON [<schema>].[Receipt]([Status])
