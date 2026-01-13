-- VehicleModel Table
-- Global vehicle make/model lookup data (shared across all tenants)
CREATE TABLE [Core].[VehicleModel]
(
    [VehicleModelId]    INT            NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns from JSON
    [Make]              AS CAST(JSON_VALUE([Json], '$.Make') AS NVARCHAR(100)) PERSISTED,
    [Model]             AS CAST(JSON_VALUE([Json], '$.Model') AS NVARCHAR(100)) PERSISTED,
    [VehicleType]       AS CAST(JSON_VALUE([Json], '$.VehicleType') AS NVARCHAR(20)),
    [Segment]           AS CAST(JSON_VALUE([Json], '$.Segment') AS NVARCHAR(20)),
    [EngineCC]          AS CAST(JSON_VALUE([Json], '$.EngineCC') AS INT),
    [IsActive]          AS CAST(JSON_VALUE([Json], '$.IsActive') AS BIT),
    [DisplayOrder]      AS CAST(JSON_VALUE([Json], '$.DisplayOrder') AS INT),
    -- JSON storage
    [Json]              NVARCHAR(MAX)  NOT NULL,
    -- Audit columns
    [CreatedBy]         VARCHAR(50)    NOT NULL DEFAULT 'system',
    [ChangedBy]         VARCHAR(50)    NOT NULL DEFAULT 'system',
    [CreatedTimestamp]  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp]  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
GO

-- Index for filtering by make
CREATE INDEX [IX_VehicleModel_Make] ON [Core].[VehicleModel]([Make], [IsActive])
GO

-- Index for filtering by vehicle type
CREATE INDEX [IX_VehicleModel_VehicleType] ON [Core].[VehicleModel]([VehicleType], [IsActive])
GO

-- Unique constraint on make + model combination
CREATE UNIQUE INDEX [IX_VehicleModel_Make_Model] ON [Core].[VehicleModel]([Make], [Model])
GO

-- Index for display ordering
CREATE INDEX [IX_VehicleModel_DisplayOrder] ON [Core].[VehicleModel]([DisplayOrder], [IsActive])
GO
