-- Vehicle Owner Tracking Migration
-- Run this script against your MotoRent database
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

-- Create VehicleOwner table for KrabiBeachRentals schema
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'VehicleOwner' AND schema_id = SCHEMA_ID('KrabiBeachRentals'))
BEGIN
    CREATE TABLE [KrabiBeachRentals].[VehicleOwner]
    (
        [VehicleOwnerId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
        [Name] AS CAST(JSON_VALUE([Json], '$.Name') AS NVARCHAR(100)),
        [Phone] AS CAST(JSON_VALUE([Json], '$.Phone') AS NVARCHAR(20)),
        [Email] AS CAST(JSON_VALUE([Json], '$.Email') AS NVARCHAR(100)),
        [IsActive] AS CAST(JSON_VALUE([Json], '$.IsActive') AS BIT),
        [Json] NVARCHAR(MAX) NOT NULL,
        [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
        [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
        [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    )

    PRINT 'Created VehicleOwner table'
END
ELSE
BEGIN
    PRINT 'VehicleOwner table already exists'
END
GO

-- Create indexes for VehicleOwner
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_VehicleOwner_Name')
BEGIN
    CREATE INDEX IX_VehicleOwner_Name ON [KrabiBeachRentals].[VehicleOwner]([Name])
    PRINT 'Created IX_VehicleOwner_Name index'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_VehicleOwner_IsActive')
BEGIN
    CREATE INDEX IX_VehicleOwner_IsActive ON [KrabiBeachRentals].[VehicleOwner]([IsActive])
    PRINT 'Created IX_VehicleOwner_IsActive index'
END
GO

-- Create OwnerPayment table for KrabiBeachRentals schema
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OwnerPayment' AND schema_id = SCHEMA_ID('KrabiBeachRentals'))
BEGIN
    CREATE TABLE [KrabiBeachRentals].[OwnerPayment]
    (
        [OwnerPaymentId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
        [VehicleOwnerId] AS CAST(JSON_VALUE([Json], '$.VehicleOwnerId') AS INT),
        [VehicleId] AS CAST(JSON_VALUE([Json], '$.VehicleId') AS INT),
        [RentalId] AS CAST(JSON_VALUE([Json], '$.RentalId') AS INT),
        [PaymentModel] AS CAST(JSON_VALUE([Json], '$.PaymentModel') AS NVARCHAR(20)),
        [Amount] AS CAST(JSON_VALUE([Json], '$.Amount') AS DECIMAL(10,2)),
        [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)),
        [RentalStartDate] AS CAST(JSON_VALUE([Json], '$.RentalStartDate') AS DATETIMEOFFSET),
        [RentalEndDate] AS CAST(JSON_VALUE([Json], '$.RentalEndDate') AS DATETIMEOFFSET),
        [Json] NVARCHAR(MAX) NOT NULL,
        [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
        [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
        [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    )

    PRINT 'Created OwnerPayment table'
END
ELSE
BEGIN
    PRINT 'OwnerPayment table already exists'
END
GO

-- Create indexes for OwnerPayment
IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OwnerPayment_VehicleOwnerId_Status')
BEGIN
    CREATE INDEX IX_OwnerPayment_VehicleOwnerId_Status ON [KrabiBeachRentals].[OwnerPayment]([VehicleOwnerId], [Status])
    PRINT 'Created IX_OwnerPayment_VehicleOwnerId_Status index'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OwnerPayment_RentalId')
BEGIN
    CREATE INDEX IX_OwnerPayment_RentalId ON [KrabiBeachRentals].[OwnerPayment]([RentalId])
    PRINT 'Created IX_OwnerPayment_RentalId index'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OwnerPayment_Status')
BEGIN
    CREATE INDEX IX_OwnerPayment_Status ON [KrabiBeachRentals].[OwnerPayment]([Status])
    PRINT 'Created IX_OwnerPayment_Status index'
END
GO

IF NOT EXISTS (SELECT * FROM sys.indexes WHERE name = 'IX_OwnerPayment_RentalStartDate')
BEGIN
    CREATE INDEX IX_OwnerPayment_RentalStartDate ON [KrabiBeachRentals].[OwnerPayment]([RentalStartDate])
    PRINT 'Created IX_OwnerPayment_RentalStartDate index'
END
GO

PRINT 'Migration complete!'
GO
