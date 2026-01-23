-- ============================================================
-- Schema Fix Script for KrabiBeachRentals
-- Generated: 2026-01-23
-- ============================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
GO

-- ============================================================
-- 1. ADD MISSING COLUMNS TO Vehicle TABLE
-- ============================================================

-- Add OwnerPaymentModel column
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('KrabiBeachRentals.Vehicle') AND name = 'OwnerPaymentModel')
BEGIN
    ALTER TABLE [KrabiBeachRentals].[Vehicle]
    ADD [OwnerPaymentModel] AS CAST(JSON_VALUE([Json], '$.OwnerPaymentModel') AS NVARCHAR(20));
END
GO

-- Add OwnerDailyRate column
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('KrabiBeachRentals.Vehicle') AND name = 'OwnerDailyRate')
BEGIN
    ALTER TABLE [KrabiBeachRentals].[Vehicle]
    ADD [OwnerDailyRate] AS CAST(JSON_VALUE([Json], '$.OwnerDailyRate') AS DECIMAL(10,2));
END
GO

-- Add OwnerRevenueSharePercent column
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('KrabiBeachRentals.Vehicle') AND name = 'OwnerRevenueSharePercent')
BEGIN
    ALTER TABLE [KrabiBeachRentals].[Vehicle]
    ADD [OwnerRevenueSharePercent] AS CAST(JSON_VALUE([Json], '$.OwnerRevenueSharePercent') AS DECIMAL(5,4));
END
GO

-- ============================================================
-- 2. CREATE MISSING TABLES
-- ============================================================

-- ------------------------------------------------------------
-- AgentInvoice table
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID('KrabiBeachRentals') AND name = 'AgentInvoice')
BEGIN
    CREATE TABLE [KrabiBeachRentals].[AgentInvoice]
    (
        [AgentInvoiceId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
        -- Foreign Keys
        [AgentId] AS CAST(JSON_VALUE([Json], '$.AgentId') AS INT),
        [BookingId] AS CAST(JSON_VALUE([Json], '$.BookingId') AS INT),
        -- Invoice Details
        [InvoiceNo] AS CAST(JSON_VALUE([Json], '$.InvoiceNo') AS NVARCHAR(50)),
        [InvoiceDate] DATETIMEOFFSET NULL,
        -- Amounts
        [SubTotal] AS CAST(JSON_VALUE([Json], '$.SubTotal') AS DECIMAL(18,2)),
        [SurchargeAmount] AS CAST(JSON_VALUE([Json], '$.SurchargeAmount') AS DECIMAL(18,2)),
        [TotalAmount] AS CAST(JSON_VALUE([Json], '$.TotalAmount') AS DECIMAL(18,2)),
        [AmountPaid] AS CAST(JSON_VALUE([Json], '$.AmountPaid') AS DECIMAL(18,2)),
        -- Status
        [PaymentStatus] AS CAST(JSON_VALUE([Json], '$.PaymentStatus') AS NVARCHAR(20)),
        -- Customer
        [CustomerName] AS CAST(JSON_VALUE([Json], '$.CustomerName') AS NVARCHAR(100)),
        -- Denormalized
        [AgentName] AS CAST(JSON_VALUE([Json], '$.AgentName') AS NVARCHAR(200)),
        [BookingRef] AS CAST(JSON_VALUE([Json], '$.BookingRef') AS NVARCHAR(10)),
        -- JSON storage
        [Json] NVARCHAR(MAX) NOT NULL,
        -- Audit columns
        [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
        [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
        [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );

    CREATE INDEX IX_AgentInvoice_AgentId ON [KrabiBeachRentals].[AgentInvoice]([AgentId]);
    CREATE INDEX IX_AgentInvoice_BookingId ON [KrabiBeachRentals].[AgentInvoice]([BookingId]);
    CREATE INDEX IX_AgentInvoice_InvoiceNo ON [KrabiBeachRentals].[AgentInvoice]([InvoiceNo]);
    CREATE INDEX IX_AgentInvoice_PaymentStatus ON [KrabiBeachRentals].[AgentInvoice]([PaymentStatus]);
    CREATE INDEX IX_AgentInvoice_InvoiceDate ON [KrabiBeachRentals].[AgentInvoice]([InvoiceDate]);
END
GO

-- ------------------------------------------------------------
-- Follow table
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID('KrabiBeachRentals') AND name = 'Follow')
BEGIN
    CREATE TABLE [KrabiBeachRentals].[Follow]
    (
        [FollowId]          INT            NOT NULL PRIMARY KEY IDENTITY(1,1),
        -- Computed columns from JSON
        [Type]              AS CAST(JSON_VALUE([Json], '$.Type') AS VARCHAR(50)),
        [User]              AS CAST(JSON_VALUE([Json], '$.User') AS VARCHAR(100)),
        [IsActive]          AS CAST(JSON_VALUE([Json], '$.IsActive') AS BIT),
        [EntityId]          AS CAST(JSON_VALUE([Json], '$.EntityId') AS INT),
        -- JSON storage
        [Json]              NVARCHAR(MAX)  NOT NULL,
        -- Audit columns
        [CreatedBy]         VARCHAR(50)    NOT NULL DEFAULT 'system',
        [ChangedBy]         VARCHAR(50)    NOT NULL DEFAULT 'system',
        [CreatedTimestamp]  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [ChangedTimestamp]  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );

    CREATE INDEX [IX_Follow_EntityId_User_Type] ON [KrabiBeachRentals].[Follow]([EntityId], [User], [Type]);
END
GO

-- ------------------------------------------------------------
-- MaintenanceRecord table
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID('KrabiBeachRentals') AND name = 'MaintenanceRecord')
BEGIN
    CREATE TABLE [KrabiBeachRentals].[MaintenanceRecord]
    (
        [MaintenanceRecordId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
        -- Computed columns for querying
        [VehicleId] AS CAST(JSON_VALUE([Json], '$.VehicleId') AS INT),
        [ServiceTypeId] AS CAST(JSON_VALUE([Json], '$.ServiceTypeId') AS INT),
        [ServiceTypeName] AS CAST(JSON_VALUE([Json], '$.ServiceTypeName') AS NVARCHAR(100)),
        [ServiceDate] DATE NULL,
        [ServiceMileage] AS CAST(JSON_VALUE([Json], '$.ServiceMileage') AS INT),
        [Cost] AS CAST(JSON_VALUE([Json], '$.Cost') AS MONEY),
        [WorkshopName] AS CAST(JSON_VALUE([Json], '$.Workshop.Name') AS NVARCHAR(200)),
        -- JSON storage
        [Json] NVARCHAR(MAX) NOT NULL,
        -- Audit columns
        [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
        [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
        [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );

    CREATE INDEX IX_MaintenanceRecord_VehicleId ON [KrabiBeachRentals].[MaintenanceRecord]([VehicleId]);
    CREATE INDEX IX_MaintenanceRecord_ServiceTypeId ON [KrabiBeachRentals].[MaintenanceRecord]([ServiceTypeId]);
    CREATE INDEX IX_MaintenanceRecord_ServiceDate ON [KrabiBeachRentals].[MaintenanceRecord]([ServiceDate]);
    CREATE INDEX IX_MaintenanceRecord_Composite ON [KrabiBeachRentals].[MaintenanceRecord]([VehicleId], [ServiceDate]);
END
GO

-- ------------------------------------------------------------
-- ShortageLog table
-- ------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE schema_id = SCHEMA_ID('KrabiBeachRentals') AND name = 'ShortageLog')
BEGIN
    CREATE TABLE [KrabiBeachRentals].[ShortageLog]
    (
        [ShortageLogId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
        -- Computed columns for indexing
        [ShopId] AS CAST(JSON_VALUE([Json], '$.ShopId') AS INT),
        [TillSessionId] AS CAST(JSON_VALUE([Json], '$.TillSessionId') AS INT),
        [DailyCloseId] AS CAST(JSON_VALUE([Json], '$.DailyCloseId') AS INT),
        [StaffUserName] AS CAST(JSON_VALUE([Json], '$.StaffUserName') AS NVARCHAR(100)),
        [Currency] AS CAST(JSON_VALUE([Json], '$.Currency') AS CHAR(3)),
        [Amount] AS CAST(JSON_VALUE([Json], '$.Amount') AS DECIMAL(18,2)),
        [AmountInThb] AS CAST(JSON_VALUE([Json], '$.AmountInThb') AS DECIMAL(18,2)),
        [LoggedAt] AS CONVERT(DATETIMEOFFSET, JSON_VALUE([Json], '$.LoggedAt'), 127) PERSISTED,
        -- JSON storage
        [Json] NVARCHAR(MAX) NOT NULL,
        -- Audit columns
        [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
        [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
        [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );

    CREATE INDEX IX_ShortageLog_ShopId_TillSessionId ON [KrabiBeachRentals].[ShortageLog]([ShopId], [TillSessionId]);
    CREATE INDEX IX_ShortageLog_StaffUserName ON [KrabiBeachRentals].[ShortageLog]([StaffUserName]);
    CREATE INDEX IX_ShortageLog_LoggedAt ON [KrabiBeachRentals].[ShortageLog]([LoggedAt]);
END
GO

-- ============================================================
-- 3. OPTIONAL: DROP VehicleInspection (if not needed)
-- Uncomment the following lines if VehicleInspection table is obsolete
-- ============================================================
-- DROP TABLE [KrabiBeachRentals].[VehicleInspection];
-- GO

PRINT 'Schema fix completed for KrabiBeachRentals';
GO
