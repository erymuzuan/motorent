-- ExchangeRate table - Exchange rates for multi-currency cash payments
-- Organization-scoped rates with effective dates for history/audit
CREATE TABLE [<schema>].[ExchangeRate]
(
    [ExchangeRateId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for indexing
    [Currency] AS CAST(JSON_VALUE([Json], '$.Currency') AS CHAR(3)),
    [BuyRate] AS CAST(JSON_VALUE([Json], '$.BuyRate') AS DECIMAL(18,4)),
    [Source] AS CAST(JSON_VALUE([Json], '$.Source') AS NVARCHAR(20)),
    [IsActive] AS CAST(JSON_VALUE([Json], '$.IsActive') AS BIT),
    [EffectiveDate] AS CONVERT(DATETIMEOFFSET, JSON_VALUE([Json], '$.EffectiveDate'), 127) PERSISTED,
    [ExpiresOn] AS CONVERT(DATETIMEOFFSET, JSON_VALUE([Json], '$.ExpiresOn'), 127) PERSISTED,
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE INDEX IX_ExchangeRate_Currency_Active ON [<schema>].[ExchangeRate]([Currency], [IsActive])
CREATE INDEX IX_ExchangeRate_EffectiveDate ON [<schema>].[ExchangeRate]([EffectiveDate])
