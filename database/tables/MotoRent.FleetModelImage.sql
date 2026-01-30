-- FleetModelImage table
-- Stores marketing/catalog images for fleet models (up to 5 per fleet model)
CREATE TABLE [<schema>].[FleetModelImage]
(
    [FleetModelImageId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns
    [FleetModelId] AS CAST(JSON_VALUE([Json], '$.FleetModelId') AS INT),
    [IsPrimary] AS CAST(JSON_VALUE([Json], '$.IsPrimary') AS BIT),
    [DisplayOrder] AS CAST(JSON_VALUE([Json], '$.DisplayOrder') AS INT),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE INDEX IX_FleetModelImage_FleetModelId ON [<schema>].[FleetModelImage]([FleetModelId])
CREATE INDEX IX_FleetModelImage_FleetModelId_IsPrimary ON [<schema>].[FleetModelImage]([FleetModelId], [IsPrimary])
