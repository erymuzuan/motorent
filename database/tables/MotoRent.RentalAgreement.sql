-- RentalAgreement table
CREATE TABLE [<schema>].[RentalAgreement]
(
    [RentalAgreementId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns
    [RentalId] AS CAST(JSON_VALUE([Json], '$.RentalId') AS INT),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE INDEX IX_RentalAgreement_RentalId ON [<schema>].[RentalAgreement]([RentalId])
