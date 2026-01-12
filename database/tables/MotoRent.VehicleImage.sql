-- VehicleImage table
-- Stores images associated with vehicles (up to 5 per vehicle)
CREATE TABLE [<schema>].[VehicleImage]
(
    [VehicleImageId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns
    [VehicleId] AS CAST(JSON_VALUE([Json], '$.VehicleId') AS INT),
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

CREATE INDEX IX_VehicleImage_VehicleId ON [<schema>].[VehicleImage]([VehicleId])
CREATE INDEX IX_VehicleImage_VehicleId_IsPrimary ON [<schema>].[VehicleImage]([VehicleId], [IsPrimary])
