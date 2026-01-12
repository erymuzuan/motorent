-- AccidentDocument table
CREATE TABLE [<schema>].[AccidentDocument]
(
    [AccidentDocumentId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns
    [AccidentId] AS CAST(JSON_VALUE([Json], '$.AccidentId') AS INT),
    [DocumentType] AS CAST(JSON_VALUE([Json], '$.DocumentType') AS NVARCHAR(30)),
    [FileName] AS CAST(JSON_VALUE([Json], '$.FileName') AS NVARCHAR(255)),
    [UploadedDate] AS CAST(JSON_VALUE([Json], '$.UploadedDate') AS DATE),
    [AccidentPartyId] AS CAST(JSON_VALUE([Json], '$.AccidentPartyId') AS INT),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
GO

CREATE INDEX IX_AccidentDocument_AccidentId ON [<schema>].[AccidentDocument]([AccidentId])
GO
CREATE INDEX IX_AccidentDocument_DocumentType ON [<schema>].[AccidentDocument]([AccidentId], [DocumentType])
GO
