-- Follow Table (Tenant-specific)
-- Track users following entities for notifications within a tenant
CREATE TABLE [MotoRent].[Follow]
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
)
--

CREATE INDEX [IX_Follow_EntityId_User_Type] ON [MotoRent].[Follow]([EntityId], [User], [Type])
--
