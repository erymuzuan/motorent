-- Motorbike table
CREATE TABLE [<schema>].[Motorbike]
(
    [MotorbikeId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for querying
    [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
    [LicensePlate] AS CAST(JSON_VALUE([Json], '$.LicensePlate') AS NVARCHAR(20)),
    [Brand] AS CAST(JSON_VALUE([Json], '$.Brand') AS NVARCHAR(50)),
    [Model] AS CAST(JSON_VALUE([Json], '$.Model') AS NVARCHAR(50)),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)),
    [DailyRate] AS CAST(JSON_VALUE([Json], '$.DailyRate') AS DECIMAL(10,2)),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)

CREATE INDEX IX_Motorbike_ShopId_Status ON [<schema>].[Motorbike]([ShopId], [Status])
