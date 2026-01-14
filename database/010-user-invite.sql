-- UserInvite table for tracking pending email invitations
-- When a user logs in via OAuth with a matching email, they are automatically
-- added to the organization with the specified roles.

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserInvite' AND schema_id = SCHEMA_ID('Core'))
BEGIN
    CREATE TABLE [Core].[UserInvite]
    (
        [UserInviteId] INT NOT NULL PRIMARY KEY IDENTITY(1,1),
        [Email] AS CAST(JSON_VALUE([Json], '$.Email') AS VARCHAR(200)) PERSISTED,
        [AccountNo] AS CAST(JSON_VALUE([Json], '$.AccountNo') AS VARCHAR(50)),
        [Status] AS CAST(JSON_VALUE([Json], '$.Status') AS VARCHAR(20)),
        [ExpiresAt] AS CAST(JSON_VALUE([Json], '$.ExpiresAt') AS DATETIME2),
        [InvitedBy] AS CAST(JSON_VALUE([Json], '$.InvitedBy') AS VARCHAR(200)),
        [Json] NVARCHAR(MAX) NOT NULL,
        [CreatedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
        [ChangedBy] VARCHAR(50) NOT NULL DEFAULT 'system',
        [CreatedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET(),
        [ChangedTimestamp] DATETIMEOFFSET NOT NULL DEFAULT SYSDATETIMEOFFSET()
    );

    -- Index for efficient lookup by email and status during login
    CREATE INDEX IX_UserInvite_Email_Status
        ON [Core].[UserInvite]([Email], [Status]);

    -- Index for listing invites by organization
    CREATE INDEX IX_UserInvite_AccountNo_Status
        ON [Core].[UserInvite]([AccountNo], [Status]);

    PRINT 'Created [Core].[UserInvite] table';
END
ELSE
BEGIN
    PRINT '[Core].[UserInvite] table already exists';
END
GO
