-- Vehicle table - replaces Motorbike with support for multiple vehicle types
CREATE TABLE [<schema>].[Vehicle]
(
    [VehicleId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Location and Pool
    [HomeShopId] AS CAST(JSON_VALUE([Json], '$.HomeShopId') AS INT),
    [VehiclePoolId] AS CAST(JSON_VALUE([Json], '$.VehiclePoolId') AS INT),
    [CurrentShopId] AS CAST(JSON_VALUE([Json], '$.CurrentShopId') AS INT),
    -- Vehicle Type and Classification
    [VehicleType] AS CAST(JSON_VALUE([Json], '$.VehicleType') AS NVARCHAR(20)),
    [Segment] AS CAST(JSON_VALUE([Json], '$.Segment') AS NVARCHAR(20)),
    [DurationType] AS CAST(JSON_VALUE([Json], '$.DurationType') AS NVARCHAR(20)),
    -- Common Properties
    [LicensePlate] AS CAST(JSON_VALUE([Json], '$.LicensePlate') AS NVARCHAR(20)),
    [Brand] AS CAST(JSON_VALUE([Json], '$.Brand') AS NVARCHAR(50)),
    [Model] AS CAST(JSON_VALUE([Json], '$.Model') AS NVARCHAR(50)),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)),
    -- Pricing
    [DailyRate] AS CAST(JSON_VALUE([Json], '$.DailyRate') AS DECIMAL(10,2)),
    [Rate15Min] AS CAST(JSON_VALUE([Json], '$.Rate15Min') AS DECIMAL(10,2)),
    [Rate30Min] AS CAST(JSON_VALUE([Json], '$.Rate30Min') AS DECIMAL(10,2)),
    [Rate1Hour] AS CAST(JSON_VALUE([Json], '$.Rate1Hour') AS DECIMAL(10,2)),
    -- Driver/Guide Fees
    [DriverDailyFee] AS CAST(JSON_VALUE([Json], '$.DriverDailyFee') AS DECIMAL(10,2)),
    [GuideDailyFee] AS CAST(JSON_VALUE([Json], '$.GuideDailyFee') AS DECIMAL(10,2)),
    -- Third-Party Owner
    [VehicleOwnerId] AS CAST(JSON_VALUE([Json], '$.VehicleOwnerId') AS INT),
    [OwnerPaymentModel] AS CAST(JSON_VALUE([Json], '$.OwnerPaymentModel') AS NVARCHAR(20)),
    [OwnerDailyRate] AS CAST(JSON_VALUE([Json], '$.OwnerDailyRate') AS DECIMAL(10,2)),
    [OwnerRevenueSharePercent] AS CAST(JSON_VALUE([Json], '$.OwnerRevenueSharePercent') AS DECIMAL(5,4)),
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
CREATE INDEX IX_Vehicle_HomeShopId_Status ON [<schema>].[Vehicle]([HomeShopId], [Status])
--
CREATE INDEX IX_Vehicle_CurrentShopId_Status ON [<schema>].[Vehicle]([CurrentShopId], [Status])
--
CREATE INDEX IX_Vehicle_VehiclePoolId_Status ON [<schema>].[Vehicle]([VehiclePoolId], [Status]) WHERE [VehiclePoolId] IS NOT NULL
--
CREATE INDEX IX_Vehicle_VehicleType_Status ON [<schema>].[Vehicle]([VehicleType], [Status])
--
CREATE UNIQUE INDEX IX_Vehicle_LicensePlate ON [<schema>].[Vehicle]([LicensePlate])
--
CREATE INDEX IX_Vehicle_VehicleOwnerId ON [<schema>].[Vehicle]([VehicleOwnerId]) WHERE [VehicleOwnerId] IS NOT NULL
--
