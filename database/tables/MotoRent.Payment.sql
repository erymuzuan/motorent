-- Payment table
CREATE TABLE [<schema>].[Payment]
(
    [PaymentId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns
    [RentalId] AS CAST(JSON_VALUE([Json], '$.RentalId') AS INT),
    [PaymentType] AS CAST(JSON_VALUE([Json], '$.PaymentType') AS NVARCHAR(20)),
    [PaymentMethod] AS CAST(JSON_VALUE([Json], '$.PaymentMethod') AS NVARCHAR(20)),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [PaidOn] DATETIMEOFFSET NULL,
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE INDEX IX_Payment_RentalId ON [<schema>].[Payment]([RentalId])
