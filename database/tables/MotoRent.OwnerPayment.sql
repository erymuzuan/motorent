-- OwnerPayment table - tracks payments due/paid to third-party vehicle owners
CREATE TABLE [<schema>].[OwnerPayment]
(
    [OwnerPaymentId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for querying
    [VehicleOwnerId] AS CAST(JSON_VALUE([Json], '$.VehicleOwnerId') AS INT),
    [VehicleId] AS CAST(JSON_VALUE([Json], '$.VehicleId') AS INT),
    [RentalId] AS CAST(JSON_VALUE([Json], '$.RentalId') AS INT),
    [PaymentModel] AS CAST(JSON_VALUE([Json], '$.PaymentModel') AS NVARCHAR(20)),
    [Amount] AS CAST(JSON_VALUE([Json], '$.Amount') AS DECIMAL(10,2)),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)),
    [RentalStartDate] DATETIMEOFFSET NULL,
    [RentalEndDate] DATETIMEOFFSET NULL,
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
--

CREATE INDEX IX_OwnerPayment_VehicleOwnerId_Status ON [<schema>].[OwnerPayment]([VehicleOwnerId], [Status])
--
CREATE INDEX IX_OwnerPayment_RentalId ON [<schema>].[OwnerPayment]([RentalId])
--
CREATE INDEX IX_OwnerPayment_Status ON [<schema>].[OwnerPayment]([Status])
--
CREATE INDEX IX_OwnerPayment_RentalStartDate ON [<schema>].[OwnerPayment]([RentalStartDate])
--
