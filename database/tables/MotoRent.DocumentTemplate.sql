CREATE TABLE [<schema>].[DocumentTemplate]
(
    [DocumentTemplateId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [WebId] VARCHAR(50) NULL,
    [Name] AS CAST(JSON_VALUE([Json], '$.Name') AS NVARCHAR(100)),
    [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
    [Type] AS CAST(JSON_VALUE([Json], '$.Type') AS VARCHAR(50)),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS VARCHAR(50)),
    [IsDefault] AS CAST(JSON_VALUE([Json], '$.IsDefault') AS BIT),
    [Version] AS CAST(JSON_VALUE([Json], '$.Version') AS INT),
    [Json] NVARCHAR(MAX) NOT NULL,
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
CREATE INDEX [IX_DocumentTemplate_ShopId] ON [<schema>].[DocumentTemplate] ([ShopId])

CREATE INDEX [IX_DocumentTemplate_Type] ON [<schema>].[DocumentTemplate] ([Type])

CREATE INDEX [IX_DocumentTemplate_Status] ON [<schema>].[DocumentTemplate] ([Status])
