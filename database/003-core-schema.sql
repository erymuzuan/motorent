-- =============================================
-- MotoRent Core Schema (PostgreSQL)
-- Multi-tenant core entities for user management,
-- organization (tenant) management, and authentication
-- =============================================

-- =============================================
-- Organization (Tenant) Table
-- =============================================
CREATE TABLE "Organization"
(
    "OrganizationId"    INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AccountNo"         VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'AccountNo')::VARCHAR(50)) STORED,
    "Name"              VARCHAR(200) GENERATED ALWAYS AS (("Json"->>'Name')::VARCHAR(200)) STORED,
    "Currency"          VARCHAR(10) GENERATED ALWAYS AS (("Json"->>'Currency')::VARCHAR(10)) STORED,
    "Timezone"          DOUBLE PRECISION GENERATED ALWAYS AS (("Json"->>'Timezone')::DOUBLE PRECISION) STORED,
    "Language"          VARCHAR(10) GENERATED ALWAYS AS (("Json"->>'Language')::VARCHAR(10)) STORED,
    "IsActive"          BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsActive')::BOOLEAN) STORED,
    "Json"              JSONB NOT NULL,
    "CreatedBy"         VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy"         VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IX_Organization_AccountNo ON "Organization"("AccountNo");

-- =============================================
-- User Table
-- =============================================
CREATE TABLE "User"
(
    "UserId"            INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "UserName"          VARCHAR(200) GENERATED ALWAYS AS (("Json"->>'UserName')::VARCHAR(200)) STORED,
    "Email"             VARCHAR(200) GENERATED ALWAYS AS (("Json"->>'Email')::VARCHAR(200)) STORED,
    "FullName"          VARCHAR(200) GENERATED ALWAYS AS (("Json"->>'FullName')::VARCHAR(200)) STORED,
    "CredentialProvider" VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'CredentialProvider')::VARCHAR(20)) STORED,
    "IsLockedOut"       BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsLockedOut')::BOOLEAN) STORED,
    "Json"              JSONB NOT NULL,
    "CreatedBy"         VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy"         VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IX_User_UserName ON "User"("UserName");
CREATE INDEX IX_User_Email ON "User"("Email");

-- =============================================
-- Setting Table
-- Supports both global and user-specific settings
-- =============================================
CREATE TABLE "Setting"
(
    "SettingId"         INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AccountNo"         VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'AccountNo')::VARCHAR(20)) STORED,
    "Key"               VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'Key')::VARCHAR(100)) STORED,
    "UserName"          VARCHAR(200) GENERATED ALWAYS AS (("Json"->>'UserName')::VARCHAR(200)) STORED,
    "Expire"            TIMESTAMPTZ GENERATED ALWAYS AS (immutable_text_to_timestamptz("Json"->>'Expire')) STORED,
    "Json"              JSONB NOT NULL,
    "CreatedBy"         VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy"         VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IX_Setting_AccountNo_Key ON "Setting"("AccountNo", "Key");
CREATE INDEX IX_Setting_AccountNo_UserName_Key ON "Setting"("AccountNo", "UserName", "Key");

-- =============================================
-- AccessToken Table
-- For API authentication
-- =============================================
CREATE TABLE "AccessToken"
(
    "AccessTokenId"     INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AccountNo"         VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'AccountNo')::VARCHAR(20)) STORED,
    "UserName"          VARCHAR(200) GENERATED ALWAYS AS (("Json"->>'UserName')::VARCHAR(200)) STORED,
    "Token"             VARCHAR(100) GENERATED ALWAYS AS (("Json"->>'Token')::VARCHAR(100)) STORED,
    "Salt"              VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Salt')::VARCHAR(20)) STORED,
    "Issued"            DATE GENERATED ALWAYS AS (immutable_text_to_date("Json"->>'Issued')) STORED,
    "Expires"           DATE GENERATED ALWAYS AS (immutable_text_to_date("Json"->>'Expires')) STORED,
    "IsRevoked"         BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsRevoked')::BOOLEAN) STORED,
    "Json"              JSONB NOT NULL,
    "CreatedBy"         VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy"         VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IX_AccessToken_Token ON "AccessToken"("Token");
CREATE INDEX IX_AccessToken_UserName ON "AccessToken"("UserName");

-- =============================================
-- RegistrationInvite Table
-- For controlled tenant onboarding
-- =============================================
CREATE TABLE "RegistrationInvite"
(
    "RegistrationInviteId" INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "Code"              VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'Code')::VARCHAR(50)) STORED,
    "IsActive"          BOOLEAN GENERATED ALWAYS AS (("Json"->>'IsActive')::BOOLEAN) STORED,
    "ValidFrom"         DATE GENERATED ALWAYS AS (immutable_text_to_date("Json"->>'ValidFrom')) STORED,
    "ValidTo"           DATE GENERATED ALWAYS AS (immutable_text_to_date("Json"->>'ValidTo')) STORED,
    "MaxAccount"        INT GENERATED ALWAYS AS (("Json"->>'MaxAccount')::INT) STORED,
    "Json"              JSONB NOT NULL,
    "CreatedBy"         VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy"         VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE UNIQUE INDEX IX_RegistrationInvite_Code ON "RegistrationInvite"("Code");

-- =============================================
-- LogEntry Table
-- For audit logging
-- =============================================
CREATE TABLE "LogEntry"
(
    "LogEntryId"        INT GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
    "AccountNo"         VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'AccountNo')::VARCHAR(20)) STORED,
    "UserName"          VARCHAR(200) GENERATED ALWAYS AS (("Json"->>'UserName')::VARCHAR(200)) STORED,
    "Severity"          VARCHAR(20) GENERATED ALWAYS AS (("Json"->>'Severity')::VARCHAR(20)) STORED,
    "Application"       VARCHAR(50) GENERATED ALWAYS AS (("Json"->>'Application')::VARCHAR(50)) STORED,
    "DateTime"          TIMESTAMPTZ GENERATED ALWAYS AS (immutable_text_to_timestamptz("Json"->>'DateTime')) STORED,
    "Json"              JSONB NOT NULL,
    "CreatedBy"         VARCHAR(50) NOT NULL DEFAULT 'system',
    "ChangedBy"         VARCHAR(50) NOT NULL DEFAULT 'system',
    "CreatedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    "ChangedTimestamp"  TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE INDEX IX_LogEntry_AccountNo_DateTime ON "LogEntry"("AccountNo", "DateTime");
CREATE INDEX IX_LogEntry_Severity ON "LogEntry"("Severity");
