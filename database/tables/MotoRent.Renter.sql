-- Renter table
CREATE TABLE [<schema>].[Renter]
(
    [RenterId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    [FullName] AS CAST(JSON_VALUE([Json], '$.FullName') AS NVARCHAR(200)),
    [Phone] AS CAST(JSON_VALUE([Json], '$.Phone') AS NVARCHAR(50)),
    [PassportNo] AS CAST(JSON_VALUE([Json], '$.PassportNo') AS NVARCHAR(50)),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE INDEX IX_<schema>Renter_PassportNo ON [<schema>].[Renter]([PassportNo], [FullName])
