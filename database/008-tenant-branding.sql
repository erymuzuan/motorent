-- =============================================
-- MotoRent Tenant Branding Migration
-- Adds CustomDomain computed column to Organization
-- for efficient lookup of tenants by custom domain
-- =============================================
-- Version: 008
-- Date: 2026-01-12
-- Description: Support for custom domains and tenant branding
--
-- Note: TenantBranding is stored in the JSON column as part of
-- the Organization entity. No schema change needed for branding
-- properties - only CustomDomain needs a computed column for indexing.
-- =============================================

SET QUOTED_IDENTIFIER ON
GO

-- Check if CustomDomain column already exists
IF NOT EXISTS (
    SELECT 1 FROM sys.columns
    WHERE object_id = OBJECT_ID('[Core].[Organization]')
    AND name = 'CustomDomain'
)
BEGIN
    -- Add CustomDomain computed column
    ALTER TABLE [Core].[Organization]
    ADD [CustomDomain] AS CAST(JSON_VALUE([Json], '$.CustomDomain') AS VARCHAR(100));

    PRINT 'Added CustomDomain computed column to [Core].[Organization]';
END
GO

-- Create index for custom domain lookup (critical for middleware performance)
-- Only index non-null values since most organizations won't have custom domains
IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE object_id = OBJECT_ID('[Core].[Organization]')
    AND name = 'IX_Organization_CustomDomain'
)
BEGIN
    CREATE INDEX [IX_Organization_CustomDomain]
    ON [Core].[Organization]([CustomDomain])
    WHERE [CustomDomain] IS NOT NULL;

    PRINT 'Created filtered index IX_Organization_CustomDomain';
END
GO

-- =============================================
-- Example: Update an organization with branding
-- =============================================
-- DECLARE @json NVARCHAR(MAX) = (
--     SELECT [Json] FROM [Core].[Organization] WHERE AccountNo = 'TestTenant'
-- );
--
-- SET @json = JSON_MODIFY(@json, '$.CustomDomain', 'adam.co.th');
-- SET @json = JSON_MODIFY(@json, '$.Branding.PrimaryColor', '#E91E63');
-- SET @json = JSON_MODIFY(@json, '$.Branding.SecondaryColor', '#C2185B');
-- SET @json = JSON_MODIFY(@json, '$.Branding.AccentColor', '#F48FB1');
-- SET @json = JSON_MODIFY(@json, '$.Branding.LayoutTemplate', 'Modern');
-- SET @json = JSON_MODIFY(@json, '$.Branding.Tagline', 'Best Motorbike Rentals in Thailand');
--
-- UPDATE [Core].[Organization]
-- SET [Json] = @json, [ChangedBy] = 'system', [ChangedTimestamp] = SYSDATETIMEOFFSET()
-- WHERE AccountNo = 'TestTenant';
-- =============================================
