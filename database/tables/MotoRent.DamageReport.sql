-- DamageReport table
CREATE TABLE [<schema>].[DamageReport]
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

CREATE INDEX IX_DamageReport_RentalId ON [<schema>].[DamageReport]([RentalId])
CREATE INDEX IX_DamageReport_MotorbikeId ON [<schema>].[DamageReport]([MotorbikeId])
