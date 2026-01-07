-- =============================================
-- MotoRent Core Schema
-- Multi-tenant core entities for user management,
-- organization (tenant) management, and authentication
-- =============================================

-- Create Core schema
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'Core')
BEGIN
    EXEC('CREATE SCHEMA [Core]');
END
GO

-- =============================================
-- Organization (Tenant) Table
-- =============================================
CREATE TABLE [Core].[Organization]
(
    [OrganizationId]    INT            NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns from JSON
    [AccountNo]         AS CAST(JSON_VALUE([Json], '$.AccountNo') AS VARCHAR(20)) PERSISTED,
    [Name]              AS CAST(JSON_VALUE([Json], '$.Name') AS NVARCHAR(200)),
    [Currency]          AS CAST(JSON_VALUE([Json], '$.Currency') AS VARCHAR(10)),
    [Timezone]          AS CAST(JSON_VALUE([Json], '$.Timezone') AS FLOAT),
    [Language]          AS CAST(JSON_VALUE([Json], '$.Language') AS VARCHAR(10)),
    [IsActive]          AS CAST(JSON_VALUE([Json], '$.IsActive') AS BIT),
    -- JSON storage
    [Json]              NVARCHAR(MAX)  NOT NULL,
    -- Audit columns
    [CreatedBy]         VARCHAR(50)    NOT NULL DEFAULT 'system',
    [ChangedBy]         VARCHAR(50)    NOT NULL DEFAULT 'system',
    [CreatedTimestamp]  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp]  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
);
GO

CREATE UNIQUE INDEX [IX_Organization_AccountNo] ON [Core].[Organization]([AccountNo]);
GO

-- =============================================
-- User Table
-- =============================================
CREATE TABLE [Core].[User]
(
    [UserId]            INT            NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns from JSON
    [UserName]          AS CAST(JSON_VALUE([Json], '$.UserName') AS VARCHAR(200)) PERSISTED,
    [Email]             AS CAST(JSON_VALUE([Json], '$.Email') AS VARCHAR(200)),
    [FullName]          AS CAST(JSON_VALUE([Json], '$.FullName') AS NVARCHAR(200)),
    [CredentialProvider] AS CAST(JSON_VALUE([Json], '$.CredentialProvider') AS VARCHAR(20)),
    [IsLockedOut]       AS CAST(JSON_VALUE([Json], '$.IsLockedOut') AS BIT),
    -- JSON storage
    [Json]              NVARCHAR(MAX)  NOT NULL,
    -- Audit columns
    [CreatedBy]         VARCHAR(50)    NOT NULL DEFAULT 'system',
    [ChangedBy]         VARCHAR(50)    NOT NULL DEFAULT 'system',
    [CreatedTimestamp]  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp]  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
);
GO

CREATE UNIQUE INDEX [IX_User_UserName] ON [Core].[User]([UserName]);
CREATE INDEX [IX_User_Email] ON [Core].[User]([Email]);
GO

-- =============================================
-- Setting Table
-- Supports both global and user-specific settings
-- =============================================
CREATE TABLE [Core].[Setting]
(
    [SettingId]         INT            NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns from JSON
    [AccountNo]         AS CAST(JSON_VALUE([Json], '$.AccountNo') AS VARCHAR(20)),
    [Key]               AS CAST(JSON_VALUE([Json], '$.Key') AS VARCHAR(100)) PERSISTED,
    [UserName]          AS CAST(JSON_VALUE([Json], '$.UserName') AS VARCHAR(200)),
    [Expire]            AS CAST(JSON_VALUE([Json], '$.Expire') AS DATETIMEOFFSET),
    -- JSON storage
    [Json]              NVARCHAR(MAX)  NOT NULL,
    -- Audit columns
    [CreatedBy]         VARCHAR(50)    NOT NULL DEFAULT 'system',
    [ChangedBy]         VARCHAR(50)    NOT NULL DEFAULT 'system',
    [CreatedTimestamp]  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp]  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
);
GO

CREATE INDEX [IX_Setting_AccountNo_Key] ON [Core].[Setting]([AccountNo], [Key]);
CREATE INDEX [IX_Setting_AccountNo_UserName_Key] ON [Core].[Setting]([AccountNo], [UserName], [Key]);
GO

-- =============================================
-- AccessToken Table
-- For API authentication
-- =============================================
CREATE TABLE [Core].[AccessToken]
(
    [AccessTokenId]     INT            NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns from JSON
    [AccountNo]         AS CAST(JSON_VALUE([Json], '$.AccountNo') AS VARCHAR(20)),
    [UserName]          AS CAST(JSON_VALUE([Json], '$.UserName') AS VARCHAR(200)),
    [Token]             AS CAST(JSON_VALUE([Json], '$.Token') AS VARCHAR(100)) PERSISTED,
    [Salt]              AS CAST(JSON_VALUE([Json], '$.Salt') AS VARCHAR(20)),
    [Issued]            AS CAST(JSON_VALUE([Json], '$.Issued') AS DATE),
    [Expires]           AS CAST(JSON_VALUE([Json], '$.Expires') AS DATE),
    [IsRevoked]         AS CAST(JSON_VALUE([Json], '$.IsRevoked') AS BIT),
    -- JSON storage (includes JWT Payload)
    [Json]              NVARCHAR(MAX)  NOT NULL,
    -- Audit columns
    [CreatedBy]         VARCHAR(50)    NOT NULL DEFAULT 'system',
    [ChangedBy]         VARCHAR(50)    NOT NULL DEFAULT 'system',
    [CreatedTimestamp]  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp]  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
);
GO

CREATE INDEX [IX_AccessToken_Token] ON [Core].[AccessToken]([Token]);
CREATE INDEX [IX_AccessToken_UserName] ON [Core].[AccessToken]([UserName]);
GO

-- =============================================
-- RegistrationInvite Table
-- For controlled tenant onboarding
-- =============================================
CREATE TABLE [Core].[RegistrationInvite]
(
    [RegistrationInviteId] INT         NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns from JSON
    [Code]              AS CAST(JSON_VALUE([Json], '$.Code') AS VARCHAR(50)) PERSISTED,
    [IsActive]          AS CAST(JSON_VALUE([Json], '$.IsActive') AS BIT),
    [ValidFrom]         AS CAST(JSON_VALUE([Json], '$.ValidFrom') AS DATE),
    [ValidTo]           AS CAST(JSON_VALUE([Json], '$.ValidTo') AS DATE),
    [MaxAccount]        AS CAST(JSON_VALUE([Json], '$.MaxAccount') AS INT),
    -- JSON storage
    [Json]              NVARCHAR(MAX)  NOT NULL,
    -- Audit columns
    [CreatedBy]         VARCHAR(50)    NOT NULL DEFAULT 'system',
    [ChangedBy]         VARCHAR(50)    NOT NULL DEFAULT 'system',
    [CreatedTimestamp]  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp]  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
);
GO

CREATE UNIQUE INDEX [IX_RegistrationInvite_Code] ON [Core].[RegistrationInvite]([Code]);
GO

-- =============================================
-- LogEntry Table
-- For audit logging
-- =============================================
CREATE TABLE [Core].[LogEntry]
(
    [LogEntryId]        INT            NOT NULL PRIMARY KEY IDENTITY(1,1),
    -- Computed columns from JSON
    [AccountNo]         AS CAST(JSON_VALUE([Json], '$.AccountNo') AS VARCHAR(20)),
    [UserName]          AS CAST(JSON_VALUE([Json], '$.UserName') AS VARCHAR(200)),
    [Severity]          AS CAST(JSON_VALUE([Json], '$.Severity') AS VARCHAR(20)),
    [Application]       AS CAST(JSON_VALUE([Json], '$.Application') AS VARCHAR(50)),
    [DateTime]          AS CAST(JSON_VALUE([Json], '$.DateTime') AS DATETIMEOFFSET),
    -- JSON storage
    [Json]              NVARCHAR(MAX)  NOT NULL,
    -- Audit columns
    [CreatedBy]         VARCHAR(50)    NOT NULL DEFAULT 'system',
    [ChangedBy]         VARCHAR(50)    NOT NULL DEFAULT 'system',
    [CreatedTimestamp]  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
    [ChangedTimestamp]  DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
);
GO

CREATE INDEX [IX_LogEntry_AccountNo_DateTime] ON [Core].[LogEntry]([AccountNo], [DateTime]);
CREATE INDEX [IX_LogEntry_Severity] ON [Core].[LogEntry]([Severity]);
GO

-- NOTE: Shop table does NOT need AccountNo column
-- Shop data is stored in tenant-specific schemas (e.g., [AccountNo].[Shop])
-- The schema itself provides tenant isolation
-- Organization (tenant) -> has many Shops (outlets)
