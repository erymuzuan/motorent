-- DamagePhoto table
CREATE TABLE [<schema>].[DamagePhoto]
(
    [DamagePhotoId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns
    [DamageReportId] AS CAST(JSON_VALUE([Json], '$.DamageReportId') AS INT),
    [PhotoType] AS CAST(JSON_VALUE([Json], '$.PhotoType') AS NVARCHAR(20)),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE INDEX IX_DamagePhoto_DamageReportId ON [<schema>].[DamagePhoto]([DamageReportId])
