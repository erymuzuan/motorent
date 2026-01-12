-- AccidentParty table
CREATE TABLE [<schema>].[AccidentParty]
(
    [AccidentPartyId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns
    [AccidentId] AS CAST(JSON_VALUE([Json], '$.AccidentId') AS INT),
    [PartyType] AS CAST(JSON_VALUE([Json], '$.PartyType') AS NVARCHAR(30)),
    [Name] AS CAST(JSON_VALUE([Json], '$.Name') AS NVARCHAR(100)),
    [IsInjured] AS CAST(JSON_VALUE([Json], '$.IsInjured') AS BIT),
    [IsAtFault] AS CAST(JSON_VALUE([Json], '$.IsAtFault') AS BIT),
    [RenterId] AS CAST(JSON_VALUE([Json], '$.RenterId') AS INT),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
GO

CREATE INDEX IX_AccidentParty_AccidentId ON [<schema>].[AccidentParty]([AccidentId])
GO
CREATE INDEX IX_AccidentParty_PartyType ON [<schema>].[AccidentParty]([AccidentId], [PartyType])
GO
