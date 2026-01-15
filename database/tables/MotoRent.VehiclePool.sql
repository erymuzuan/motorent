-- VehiclePool table - groups shops that share vehicle inventory
CREATE TABLE [<schema>].[VehiclePool]
(
    [VehiclePoolId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for querying
    [Name] AS CAST(JSON_VALUE([Json], '$.Name') AS NVARCHAR(200)),
    [IsActive] AS CAST(JSON_VALUE([Json], '$.IsActive') AS BIT),
    [PrimaryShopId] AS CAST(JSON_VALUE([Json], '$.PrimaryShopId') AS INT),
    -- JSON storage (includes ShopIds array)
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
--

CREATE INDEX IX_VehiclePool_IsActive ON [<schema>].[VehiclePool]([IsActive])
--
