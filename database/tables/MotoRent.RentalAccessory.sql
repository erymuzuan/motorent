-- RentalAccessory table
CREATE TABLE [<schema>].[RentalAccessory]
(
    [RentalAccessoryId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns
    [RentalId] AS CAST(JSON_VALUE([Json], '$.RentalId') AS INT),
    [AccessoryId] AS CAST(JSON_VALUE([Json], '$.AccessoryId') AS INT),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE INDEX IX_RentalAccessory_RentalId ON [<schema>].[RentalAccessory]([RentalId])
