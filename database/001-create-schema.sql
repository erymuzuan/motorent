-- MotoRent Database Schema
-- SQL Server with JSON columns and computed columns for indexing

-- Create schema
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'MotoRent')
    EXEC('CREATE SCHEMA [MotoRent]')
GO

-- Shop table
CREATE TABLE [MotoRent].[Shop]
(
    [ShopId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for querying
    [Name] AS CAST(JSON_VALUE([Json], '$.Name') AS NVARCHAR(200)),
    [Location] AS CAST(JSON_VALUE([Json], '$.Location') AS NVARCHAR(100)),
    [IsActive] AS CAST(JSON_VALUE([Json], '$.IsActive') AS BIT),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
GO

-- Renter table
CREATE TABLE [MotoRent].[Renter]
(
    [RenterId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for querying
    [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
    [FullName] AS CAST(JSON_VALUE([Json], '$.FullName') AS NVARCHAR(200)),
    [Phone] AS CAST(JSON_VALUE([Json], '$.Phone') AS NVARCHAR(50)),
    [PassportNo] AS CAST(JSON_VALUE([Json], '$.PassportNo') AS NVARCHAR(50)),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
GO

CREATE INDEX IX_Renter_ShopId ON [MotoRent].[Renter]([ShopId])
GO

-- Document table
CREATE TABLE [MotoRent].[Document]
(
    [DocumentId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns
    [RenterId] AS CAST(JSON_VALUE([Json], '$.RenterId') AS INT),
    [DocumentType] AS CAST(JSON_VALUE([Json], '$.DocumentType') AS NVARCHAR(50)),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
GO

CREATE INDEX IX_Document_RenterId ON [MotoRent].[Document]([RenterId])
GO

-- Motorbike table
CREATE TABLE [MotoRent].[Motorbike]
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
GO

CREATE INDEX IX_Motorbike_ShopId_Status ON [MotoRent].[Motorbike]([ShopId], [Status])
GO

-- Insurance table
CREATE TABLE [MotoRent].[Insurance]
(
    [InsuranceId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns
    [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
    [Name] AS CAST(JSON_VALUE([Json], '$.Name') AS NVARCHAR(100)),
    [IsActive] AS CAST(JSON_VALUE([Json], '$.IsActive') AS BIT),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
GO

CREATE INDEX IX_Insurance_ShopId ON [MotoRent].[Insurance]([ShopId])
GO

-- Accessory table
CREATE TABLE [MotoRent].[Accessory]
(
    [AccessoryId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns
    [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
    [Name] AS CAST(JSON_VALUE([Json], '$.Name') AS NVARCHAR(100)),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
GO

CREATE INDEX IX_Accessory_ShopId ON [MotoRent].[Accessory]([ShopId])
GO

-- Rental table
CREATE TABLE [MotoRent].[Rental]
(
    [RentalId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns for querying
    [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
    [RenterId] AS CAST(JSON_VALUE([Json], '$.RenterId') AS INT),
    [MotorbikeId] AS CAST(JSON_VALUE([Json], '$.MotorbikeId') AS INT),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)),
    [StartDate] AS CAST(JSON_VALUE([Json], '$.StartDate') AS DATE),
    [ExpectedEndDate] AS CAST(JSON_VALUE([Json], '$.ExpectedEndDate') AS DATE),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
GO

CREATE INDEX IX_Rental_ShopId_Status ON [MotoRent].[Rental]([ShopId], [Status])
CREATE INDEX IX_Rental_RenterId ON [MotoRent].[Rental]([RenterId])
CREATE INDEX IX_Rental_MotorbikeId ON [MotoRent].[Rental]([MotorbikeId])
GO

-- RentalAccessory table
CREATE TABLE [MotoRent].[RentalAccessory]
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
GO

CREATE INDEX IX_RentalAccessory_RentalId ON [MotoRent].[RentalAccessory]([RentalId])
GO

-- Deposit table
CREATE TABLE [MotoRent].[Deposit]
(
    [DepositId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns
    [RentalId] AS CAST(JSON_VALUE([Json], '$.RentalId') AS INT),
    [DepositType] AS CAST(JSON_VALUE([Json], '$.DepositType') AS NVARCHAR(20)),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
GO

CREATE INDEX IX_Deposit_RentalId ON [MotoRent].[Deposit]([RentalId])
GO

-- Payment table
CREATE TABLE [MotoRent].[Payment]
(
    [PaymentId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns
    [RentalId] AS CAST(JSON_VALUE([Json], '$.RentalId') AS INT),
    [PaymentType] AS CAST(JSON_VALUE([Json], '$.PaymentType') AS NVARCHAR(20)),
    [PaymentMethod] AS CAST(JSON_VALUE([Json], '$.PaymentMethod') AS NVARCHAR(20)),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
GO

CREATE INDEX IX_Payment_RentalId ON [MotoRent].[Payment]([RentalId])
GO

-- DamageReport table
CREATE TABLE [MotoRent].[DamageReport]
(
    [DamageReportId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns
    [RentalId] AS CAST(JSON_VALUE([Json], '$.RentalId') AS INT),
    [MotorbikeId] AS CAST(JSON_VALUE([Json], '$.MotorbikeId') AS INT),
    [Severity] AS CAST(JSON_VALUE([Json], '$.Severity') AS NVARCHAR(20)),
    [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS NVARCHAR(20)),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
GO

CREATE INDEX IX_DamageReport_RentalId ON [MotoRent].[DamageReport]([RentalId])
CREATE INDEX IX_DamageReport_MotorbikeId ON [MotoRent].[DamageReport]([MotorbikeId])
GO

-- DamagePhoto table
CREATE TABLE [MotoRent].[DamagePhoto]
(
    [DamagePhotoId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns
    [DamageReportId] AS CAST(JSON_VALUE([Json], '$.DamageReportId') AS INT),
    [PhotoType] AS CAST(JSON_VALUE([Json], '$.PhotoType') AS NVARCHAR(20)),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
GO

CREATE INDEX IX_DamagePhoto_DamageReportId ON [MotoRent].[DamagePhoto]([DamageReportId])
GO

-- RentalAgreement table
CREATE TABLE [MotoRent].[RentalAgreement]
(
    [RentalAgreementId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns
    [RentalId] AS CAST(JSON_VALUE([Json], '$.RentalId') AS INT),
    -- JSON storage
    [Json] NVARCHAR(MAX) NOT NULL,
    -- Audit columns
    [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
    [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
)
GO

CREATE INDEX IX_RentalAgreement_RentalId ON [MotoRent].[RentalAgreement]([RentalId])
GO

PRINT 'MotoRent schema created successfully'
GO
