-- FleetModel table - shared vehicle attributes for vehicles of the same make/model/year
CREATE TABLE [<schema>].[FleetModel]
(
    [FleetModelId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Vehicle identification
    [VehicleModelId] AS CAST(JSON_VALUE([Json], '$.VehicleModelId') AS INT),
    [Brand] AS CAST(JSON_VALUE([Json], '$.Brand') AS NVARCHAR(50)),
    [Model] AS CAST(JSON_VALUE([Json], '$.Model') AS NVARCHAR(50)),
    [Year] AS CAST(JSON_VALUE([Json], '$.Year') AS INT),
    [VehicleType] AS CAST(JSON_VALUE([Json], '$.VehicleType') AS NVARCHAR(20)),
    -- Pricing
    [DailyRate] AS CAST(JSON_VALUE([Json], '$.DailyRate') AS DECIMAL(10,2)),
    [DurationType] AS CAST(JSON_VALUE([Json], '$.DurationType') AS NVARCHAR(20)),
    -- Status
    [IsActive] AS CAST(JSON_VALUE([Json], '$.IsActive') AS BIT),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
--

-- Indexes for common query patterns
CREATE INDEX IX_FleetModel_VehicleType_IsActive ON [<schema>].[FleetModel]([VehicleType], [IsActive])
--
CREATE INDEX IX_FleetModel_Brand_Model ON [<schema>].[FleetModel]([Brand], [Model])
--
