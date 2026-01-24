-- =============================================
-- SaaS Subscription Schema Extensions
-- Adds computed columns for SaaS-related fields in Organization and User tables
-- =============================================

SET QUOTED_IDENTIFIER ON
GO

-- =============================================
-- Organization Extensions
-- =============================================
ALTER TABLE [Core].[Organization]
ADD [SubscriptionPlan] AS CAST(JSON_VALUE([Json], '$.SubscriptionPlan') AS INT),
    [TrialEndDate]     AS CAST(JSON_VALUE([Json], '$.TrialEndDate') AS DATETIMEOFFSET),
    [PreferredLanguage] AS CAST(JSON_VALUE([Json], '$.PreferredLanguage') AS VARCHAR(10));
GO

-- =============================================
-- User Extensions
-- =============================================
ALTER TABLE [Core].[User]
ADD [GoogleId] AS CAST(JSON_VALUE([Json], '$.GoogleId') AS VARCHAR(100)),
    [LineId]   AS CAST(JSON_VALUE([Json], '$.LineId') AS VARCHAR(100));
GO

-- Create index for subscription lookups
CREATE INDEX [IX_Organization_SubscriptionPlan] ON [Core].[Organization]([SubscriptionPlan]);
CREATE INDEX [IX_User_GoogleId] ON [Core].[User]([GoogleId]) WHERE [GoogleId] IS NOT NULL;
CREATE INDEX [IX_User_LineId] ON [Core].[User]([LineId]) WHERE [LineId] IS NOT NULL;
GO
