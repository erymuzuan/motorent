-- AccidentNote table
CREATE TABLE [<schema>].[AccidentNote]
(
    [AccidentNoteId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns
    [AccidentId] AS CAST(JSON_VALUE([Json], '$.AccidentId') AS INT),
    [NoteType] AS CAST(JSON_VALUE([Json], '$.NoteType') AS NVARCHAR(30)),
    [IsPinned] AS CAST(JSON_VALUE([Json], '$.IsPinned') AS BIT),
    [IsInternal] AS CAST(JSON_VALUE([Json], '$.IsInternal') AS BIT),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
GO

CREATE INDEX IX_AccidentNote_AccidentId ON [<schema>].[AccidentNote]([AccidentId])
GO
