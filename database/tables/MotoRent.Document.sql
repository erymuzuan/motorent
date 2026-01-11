-- Document table
CREATE TABLE [<schema>].[Document]
(
    [DocumentId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns
    [RenterId] AS CAST(JSON_VALUE([Json], '$.RenterId') AS INT),
    [DocumentType] AS CAST(JSON_VALUE([Json], '$.DocumentType') AS NVARCHAR(50)),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE INDEX IX_Document_RenterId ON [<schema>].[Document]([RenterId])
